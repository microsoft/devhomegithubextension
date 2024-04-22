// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Dapper;
using Dapper.Contrib.Extensions;
using Serilog;

namespace GitHubExtension.DataModel;

[Table("IssueAssign")]
public class IssueAssign
{
    private static readonly Lazy<ILogger> _log = new(() => Serilog.Log.ForContext("SourceContext", $"DataModel/{nameof(IssueAssign)}"));

    private static readonly ILogger Log = _log.Value;

    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    // Issue table
    public long Issue { get; set; } = DataStore.NoForeignKey;

    // User table
    public long User { get; set; } = DataStore.NoForeignKey;

    private static IssueAssign GetByIssueIdAndUserId(DataStore dataStore, long issueId, long userId)
    {
        var sql = @"SELECT * FROM IssueAssign WHERE Issue = @IssueId AND User = @UserId;";
        var param = new
        {
            IssueId = issueId,
            UserId = userId,
        };
        return dataStore.Connection!.QueryFirstOrDefault<IssueAssign>(sql, param, null);
    }

    public static IssueAssign AddUserToIssue(DataStore dataStore, Issue issue, User user)
    {
        var exists = GetByIssueIdAndUserId(dataStore, issue.Id, user.Id);
        if (exists is not null)
        {
            // Already an association between this label and this issue.
            return exists;
        }

        var newIssueAssign = new IssueAssign
        {
            Issue = issue.Id,
            User = user.Id,
        };
        newIssueAssign.Id = dataStore.Connection!.Insert(newIssueAssign);
        return newIssueAssign;
    }

    public static IEnumerable<User> GetUsersForIssue(DataStore dataStore, Issue issue)
    {
        var sql = @"SELECT * FROM User AS U WHERE U.Id IN (SELECT User FROM IssueAssign WHERE IssueAssign.Issue = @IssueId)";
        var param = new
        {
            IssueId = issue.Id,
        };

        Log.Verbose(DataStore.GetSqlLogMessage(sql, param));
        return dataStore.Connection!.Query<User>(sql, param, null) ?? Enumerable.Empty<User>();
    }

    public static void DeleteIssueAssignForIssue(DataStore dataStore, Issue issue)
    {
        // Delete all IssueAssign entries that match this Issue Id.
        var sql = @"DELETE FROM IssueAssign WHERE Issue = $IssueId;";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$IssueId", issue.Id);
        command.ExecuteNonQuery();
    }
}
