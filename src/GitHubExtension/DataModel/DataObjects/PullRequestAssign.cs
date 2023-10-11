// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Dapper;
using Dapper.Contrib.Extensions;

namespace GitHubExtension.DataModel;

[Table("PullRequestAssign")]
public class PullRequestAssign
{
    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    // PullRequest table
    public long PullRequest { get; set; } = DataStore.NoForeignKey;

    // User table
    public long User { get; set; } = DataStore.NoForeignKey;

    private static PullRequestAssign GetByPullRequestIdAndUserId(DataStore dataStore, long pullId, long userId)
    {
        var sql = @"SELECT * FROM PullRequestAssign WHERE PullRequest = @PullId AND User = @UserId;";
        var param = new
        {
            PullId = pullId,
            UserId = userId,
        };

        return dataStore.Connection!.QueryFirstOrDefault<PullRequestAssign>(sql, param, null);
    }

    public static PullRequestAssign AddUserToPullRequest(DataStore dataStore, PullRequest pull, User user)
    {
        var exists = GetByPullRequestIdAndUserId(dataStore, pull.Id, user.Id);
        if (exists is not null)
        {
            // Already an association between this label and this issue.
            return exists;
        }

        var newPullRequestAssign = new PullRequestAssign
        {
            PullRequest = pull.Id,
            User = user.Id,
        };

        newPullRequestAssign.Id = dataStore.Connection!.Insert(newPullRequestAssign);
        return newPullRequestAssign;
    }

    public static IEnumerable<User> GetUsersForPullRequest(DataStore dataStore, PullRequest pull)
    {
        var sql = @"SELECT * FROM User AS U WHERE U.Id IN (SELECT User FROM PullRequestAssign WHERE PullRequestAssign.PullRequest = @PullId)";
        var param = new
        {
            PullId = pull.Id,
        };

        Log.Logger()?.ReportDebug(DataStore.GetSqlLogMessage(sql, param));
        return dataStore.Connection!.Query<User>(sql, param, null) ?? Enumerable.Empty<User>();
    }

    public static void DeletePullRequestAssignForPullRequest(DataStore dataStore, PullRequest pullRequest)
    {
        // Delete all PullRequestAssign entries that match this PullRequest Id.
        var sql = @"DELETE FROM PullRequestAssign WHERE PullRequest = $PullRequestId;";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$PullRequestId", pullRequest.Id);
        command.ExecuteNonQuery();
    }
}
