// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Dapper;
using Dapper.Contrib.Extensions;
using GitHubExtension.Helpers;

namespace GitHubExtension.DataModel;

[Table("SearchIssue")]
public class SearchIssue
{
    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    public long TimeUpdated { get; set; } = DataStore.NoForeignKey;

    // Issue table
    public long Issue { get; set; } = DataStore.NoForeignKey;

    // Search table
    public long Search { get; set; } = DataStore.NoForeignKey;

    [Write(false)]
    [Computed]
    public DateTime UpdatedAt => TimeUpdated.ToDateTime();

    private static SearchIssue GetByIssueIdAndSearchId(DataStore dataStore, long issueId, long searchId)
    {
        var sql = @"SELECT * FROM SearchIssue WHERE Issue = @IssueId AND Search = @SearchId;";
        var param = new
        {
            IssueId = issueId,
            SearchId = searchId,
        };
        return dataStore.Connection!.QueryFirstOrDefault<SearchIssue>(sql, param, null);
    }

    public static SearchIssue AddIssueToSearch(DataStore dataStore, Issue issue, Search search)
    {
        var exists = GetByIssueIdAndSearchId(dataStore, issue.Id, search.Id);
        if (exists is not null)
        {
            // Update the timestamp for this record so we know it is fresh.
            exists.TimeUpdated = DateTime.Now.ToDataStoreInteger();
            dataStore.Connection!.Update(exists);
            return exists;
        }

        var newSearchIssue = new SearchIssue
        {
            Issue = issue.Id,
            Search = search.Id,
            TimeUpdated = DateTime.Now.ToDataStoreInteger(),
        };
        newSearchIssue.Id = dataStore.Connection!.Insert(newSearchIssue);
        return newSearchIssue;
    }

    public static void DeleteUnreferenced(DataStore dataStore)
    {
        // Delete any where the Search Id or Issue Id does not exist.
        var sql = @"DELETE FROM SearchIssue WHERE (Search NOT IN (SELECT Id FROM Search)) OR (Issue NOT IN (SELECT Id FROM Issue))";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        Log.Logger()?.ReportDebug(DataStore.GetCommandLogMessage(sql, command));
        var rowsDeleted = command.ExecuteNonQuery();
        Log.Logger()?.ReportDebug(DataStore.GetDeletedLogMessage(rowsDeleted));
    }

    public static void DeleteBefore(DataStore dataStore, Search search, DateTime date)
    {
        // Delete delete out of date entires for a given search.
        var sql = @"DELETE FROM SearchIssue WHERE TimeUpdated < $Time AND Search = $SearchId;";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$Time", date.ToDataStoreInteger());
        command.Parameters.AddWithValue("$SearchId", search.Id);
        Log.Logger()?.ReportDebug(DataStore.GetCommandLogMessage(sql, command));
        var rowsDeleted = command.ExecuteNonQuery();
        Log.Logger()?.ReportDebug(DataStore.GetDeletedLogMessage(rowsDeleted));
    }
}
