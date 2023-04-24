﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Dapper;
using Dapper.Contrib.Extensions;
using GitHubPlugin.Helpers;

namespace GitHubPlugin.DataModel;

[Table("Search")]
public class Search
{
    // This is the time between seeing a search and updating it's TimeUpdated.
    private static readonly long UpdateThreshold = TimeSpan.FromMinutes(2).Ticks;

    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    public string Query { get; set; } = string.Empty;

    public long RepositoryId { get; set; } = DataStore.NoForeignKey;

    public long TimeUpdated { get; set; } = DataStore.NoForeignKey;

    public override string ToString() => Query;

    [Write(false)]
    [Computed]
    public DateTime UpdatedAt => TimeUpdated.ToDateTime();

    private static Search Create(string query, long repositoryId)
    {
        return new Search
        {
            Query = query,
            RepositoryId = repositoryId,
            TimeUpdated = DateTime.Now.ToDataStoreInteger(),
        };
    }

    private static Search AddOrUpdate(DataStore dataStore, Search search)
    {
        var existing = Get(dataStore, search.Query, search.RepositoryId);
        if (existing is not null)
        {
            // The Search time updated is for identifying stale data for deletion later.
            // If it's been recently updated, don't repeatedly update it for every item in a search.
            if ((search.TimeUpdated - existing.TimeUpdated) > UpdateThreshold)
            {
                search.Id = existing.Id;
                dataStore.Connection!.Update(search);
                return search;
            }
            else
            {
                return existing;
            }
        }

        // No existing search, add it.
        search.Id = dataStore.Connection!.Insert(search);
        return search;
    }

    public static Search? Get(DataStore dataStore, long id)
    {
        return dataStore.Connection!.Get<Search>(id);
    }

    public static Search? Get(DataStore dataStore, string query, long repositoryId)
    {
        var sql = @"SELECT * FROM Search WHERE Query = @Query AND RepositoryId = @RepositoryId;";
        var param = new
        {
            Query = query,
            RepositoryId = repositoryId,
        };

        return dataStore.Connection!.QueryFirstOrDefault<Search>(sql, param, null);
    }

    public static Search GetOrCreate(DataStore dataStore, string query, long repositoryId)
    {
        var newSearch = Create(query, repositoryId);
        return AddOrUpdate(dataStore, newSearch);
    }

    public static void DeleteBefore(DataStore dataStore, DateTime date)
    {
        // Delete search queries older than the date listed.
        var sql = @"DELETE FROM Search WHERE TimeUpdated < $Time;";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$Time", date.ToDataStoreInteger());
        Log.Logger()?.ReportDebug(DataStore.GetCommandLogMessage(sql, command));
        var rowsDeleted = command.ExecuteNonQuery();
        Log.Logger()?.ReportDebug(DataStore.GetDeletedLogMessage(rowsDeleted));
    }
}
