// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Dapper;
using Dapper.Contrib.Extensions;

namespace GitHubExtension.DataModel;

[Table("Metadata")]
public class MetaData
{
    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public static void AddOrUpdate(DataStore dataStore, string key, string value)
    {
        // Do UPSERT
        var sql = @"INSERT INTO Metadata(Key, Value) VALUES ($Key, $Value) ON CONFLICT (Key) DO UPDATE SET Value=excluded.Value";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$Key", key);
        command.Parameters.AddWithValue("$Value", value);
        command.ExecuteNonQuery();
    }

    public static MetaData? GetByKey(DataStore dataStore, string key)
    {
        var sql = @"SELECT * FROM MetaData WHERE Key = @Key;";
        var param = new
        {
            Key = key,
        };

        return dataStore.Connection!.QueryFirstOrDefault<MetaData>(sql, param, null);
    }

    public static string? Get(DataStore dataStore, string key)
    {
        var metaData = GetByKey(dataStore, key);
        return metaData?.Value;
    }
}
