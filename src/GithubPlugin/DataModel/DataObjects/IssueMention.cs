// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Dapper;
using Dapper.Contrib.Extensions;

namespace GitHubPlugin.DataModel;

[Table("IssueMention")]
public class IssueMention
{
    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    // Issue table
    public long Issue { get; set; } = DataStore.NoForeignKey;

    // User table
    public long User { get; set; } = DataStore.NoForeignKey;

    private static IssueMention GetByIssueIdAndUserId(DataStore dataStore, long issueId, long userId)
    {
        var sql = @"SELECT * FROM IssueMention WHERE Issue = @IssueId AND User = @UserId;";
        var param = new
        {
            IssueId = issueId,
            UserId = userId,
        };
        return dataStore.Connection!.QueryFirstOrDefault<IssueMention>(sql, param, null);
    }

    public static IssueMention AddUserToIssue(DataStore dataStore, Issue issue, User user)
    {
        var exists = GetByIssueIdAndUserId(dataStore, issue.Id, user.Id);
        if (exists is not null)
        {
            // Already an association between this label and this issue.
            return exists;
        }

        var newIssueMention = new IssueMention
        {
            Issue = issue.Id,
            User = user.Id,
        };
        newIssueMention.Id = dataStore.Connection!.Insert(newIssueMention);
        return newIssueMention;
    }

    public static IEnumerable<User> GetUsersForIssue(DataStore dataStore, Issue issue)
    {
        var sql = @"SELECT * FROM User AS U WHERE U.Id IN (SELECT User FROM IssueMention WHERE IssueMention.Issue = @IssueId)";
        var param = new
        {
            IssueId = issue.Id,
        };

        Log.Logger()?.ReportDebug(DataStore.GetSqlLogMessage(sql, param));
        return dataStore.Connection!.Query<User>(sql, param, null) ?? Enumerable.Empty<User>();
    }

    public static void DeleteIssueMentionForIssue(DataStore dataStore, Issue issue)
    {
        // Delete all IssueMention entries that match this Issue Id.
        var sql = @"DELETE FROM IssueMention WHERE Issue = $IssueId;";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$IssueId", issue.Id);
        command.ExecuteNonQuery();
    }
}
