// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Data.Sqlite;

namespace GitHubExtension.DataModel;

public class DataStoreTransaction : IDataStoreTransaction
{
    private SqliteTransaction? transaction;

    public static IDataStoreTransaction BeginTransaction(DataStore dataStore)
    {
        if (dataStore != null)
        {
            if (dataStore.Connection != null)
            {
                return new DataStoreTransaction(dataStore.Connection.BeginTransaction());
            }
        }

        return new DataStoreTransaction(null);
    }

    private DataStoreTransaction(SqliteTransaction? tx)
    {
        transaction = tx;
    }

    public void Commit()
    {
        transaction?.Commit();
    }

    public void Rollback()
    {
        transaction?.Rollback();
    }

    private bool disposed; // To detect redundant calls.

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                transaction?.Dispose();
                transaction = null;
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
