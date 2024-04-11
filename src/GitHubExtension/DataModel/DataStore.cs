// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Reflection;
using System.Text;
using GitHubExtension.Helpers;
using Microsoft.Data.Sqlite;
using Serilog;

namespace GitHubExtension.DataModel;

public class DataStore : IDisposable
{
    private static readonly Lazy<ILogger> _log = new(() => Serilog.Log.ForContext("SourceContext", nameof(DataStore)));

    private static readonly ILogger Log = _log.Value;

    public string Name { get; private set; }

    public const long NoForeignKey = 0;

    public DataStore(string name, string dataStoreFilePath, IDataStoreSchema schema)
    {
        Name = name;
        DataStoreFilePath = dataStoreFilePath;
        this.schema = schema;
    }

    public SqliteConnection? Connection { get; private set; }

    public bool IsConnected => Connection != null;

    public string DataStoreFilePath
    {
        get;
        private set;
    }

    private readonly IDataStoreSchema schema;

    public bool Create(bool deleteExistingDatabase = false)
    {
        if (File.Exists(DataStoreFilePath))
        {
            // If not deleting, check for schema version mismatch.
            // If we encounter problems or mismatch, we will delete existing db.
            if (!deleteExistingDatabase)
            {
                try
                {
                    Open();
                    var currentSchemaVersion = GetPragma<long>("user_version");
                    if (currentSchemaVersion != schema.SchemaVersion)
                    {
                        // Any mismatch of schema is considered invalid.
                        // Since the data stored is functionally a cache, the simplest and most reliable.
                        // migration method is to delete the existing database and create anew.
                        deleteExistingDatabase = true;
                        Close();
                        Log.Information($"Schema mismatch. Expected: {schema.SchemaVersion}  Actual: {currentSchemaVersion}");
                    }
                }
                catch (SqliteException e)
                {
                    // if we had a problem opening the DB and fetching the pragma, then
                    // we surely cannot reuse it.
                    deleteExistingDatabase = true;
                    Log.Error(e, $"Unable to open existing database to verify schema. Deleting database.");
                }
            }

            if (!deleteExistingDatabase)
            {
                return false;
            }
            else
            {
                Log.Warning($"Deleting database: {DataStoreFilePath}");

                if (IsConnected)
                {
                    // Must close the connection or we will get a sharing violation error.
                    Close();
                }

                try
                {
                    File.Delete(DataStoreFilePath);
                }
                catch (IOException e)
                {
                    if ((uint)e.HResult == 0x80070020)
                    {
                        Log.Fatal(e, $"Sharing Violation Error; datastore exists and cannot be deleted ({DataStoreFilePath})");
                    }
                    else
                    {
                        Log.Fatal(e, $"I/O Error ({DataStoreFilePath})");
                    }
                }
                catch (UnauthorizedAccessException e)
                {
                    Log.Fatal(e, $"Access Denied ({e})");
                }
            }
        }

        // Report creating new if it didn't exist or it was successfully deleted.
        if (!File.Exists(DataStoreFilePath))
        {
            Log.Information($"Creating new DataStore at {DataStoreFilePath}");
        }

        // Ensure Directory exists. Sqlite open database will create a file
        // that does not exist, but it will fail if the directory does not exist.
        var directory = Path.GetDirectoryName(DataStoreFilePath);
        if (!Directory.Exists(directory))
        {
            // Create the directory.
            Log.Information($"Creating root directory: {directory}");
            try
            {
                Directory.CreateDirectory(directory!);
            }
            catch (Exception e)
            {
                Log.Fatal(e, $"Failed creating directory: ({directory})");
            }
        }

        // Open will create the datastore.
        Open();

        // Create schema from all services.
        CreateSchema();
        return true;
    }

    public void Open()
    {
        if (Connection is not null)
        {
            Log.Verbose($"Connection is already open.");
            return;
        }

        Log.Verbose($"Opening datastore {DataStoreFilePath}");
        disposed = false;
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = DataStoreFilePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        };
        Connection = new SqliteConnection(builder.ToString());
        Log.Verbose($"SL: new SqliteConnection: {Connection.ConnectionString}");

        try
        {
            Connection.Open();
            SetPragma("temp_store", "MEMORY");
        }
        catch (SqliteException e)
        {
            Log.Fatal(e, $"Failed to open connection: {Connection.ConnectionString}");
        }

        Log.Debug($"Opened DataStore at {DataStoreFilePath}");
    }

    private void CreateSchema()
    {
        Log.Debug("Creating Schema");
        SetPragma("encoding", "\"UTF-8\"");

        using var tx = BeginTransaction();
        var sqls = schema.SchemaSqls;
        foreach (var sql in sqls)
        {
            Execute(sql);
        }

        Log.Debug($"Created schema ({sqls.Count} entities)");
        SetPragma("user_version", schema.SchemaVersion);
        tx.Commit();
    }

    // Gets the string representing a log message for sql string + anonymous parameter object.
    public static string GetSqlLogMessage(string sql, object? param = null)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"Execute SQL: {sql.Trim()}");

#if DEBUG
        // To prevent potentially sensitive information from getting into the log, and for
        // performance considerations, we will only log parameter values on a Debug build.
        if (param is not null)
        {
            sb.Append("    Parameters:");
            foreach (var p in param.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                sb.Append(CultureInfo.InvariantCulture, $" {p.Name}={p.GetValue(param, null)}");
            }
        }
#endif

        return sb.ToString();
    }

    // Gets the string representing a log message for a command.
    public static string GetCommandLogMessage(string sql, SqliteCommand? command = null)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"Execute SQL: {sql}");

#if DEBUG
        // To prevent potentially sensitive information from getting into the log, and for
        // performance considerations, we will only log parameter values on a Debug build.
        if (command is not null)
        {
            sb.Append("    Parameters:");
            foreach (var param in command.Parameters)
            {
                // These are objects due to SQLite's dynamic type system.
                // There is an object with name "ParameterName" and an object with name "Value"
                // To construct a Name=Value pairing we need both properties.
                foreach (var p in param.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (p.Name == "ParameterName")
                    {
                        sb.Append(CultureInfo.InvariantCulture, $" {p.GetValue(param, null)}");
                    }

                    if (p.Name == "Value")
                    {
                        sb.Append(CultureInfo.InvariantCulture, $"={p.GetValue(param, null)}");
                    }
                }
            }
        }
#endif

        return sb.ToString();
    }

    public static string GetDeletedLogMessage(long rowsDeleted)
    {
        return $"Deleted {rowsDeleted} rows.";
    }

    private void Execute(string sql)
    {
        using var command = Connection!.CreateCommand();
        command!.CommandText = sql;
        try
        {
            command!.ExecuteNonQuery();
        }
        catch (SqliteException e)
        {
            Log.Error(e, $"Failure executing SQL Command: {command.CommandText}");
        }
    }

    public T GetPragma<T>(string name)
    {
        var cmd = Connection!.CreateCommand();
        cmd.CommandText = "PRAGMA {0};".FormatInvariant(name);
        var value = cmd.ExecuteScalar();
        return (T)value!;
    }

    private void SetPragma(string name, string value) => Execute("PRAGMA {0}={1};".FormatInvariant(name, value));

    private void SetPragma(string name, long value) => SetPragma(name, value.ToStringInvariant());

    public IDataStoreTransaction BeginTransaction()
    {
        return DataStoreTransaction.BeginTransaction(this);
    }

    public void Close()
    {
        if (Connection != null)
        {
            Connection.Close();
            Log.Debug("DataStore closed.");
            Connection = null;
            SqliteConnection.ClearAllPools();
        }
    }

    private bool disposed; // To detect redundant calls.

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                Close();
            }

            disposed = true;
        }
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
