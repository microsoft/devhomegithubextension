// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Dapper;
using Dapper.Contrib.Extensions;

namespace GitHubPlugin.DataModel;

[Table("PullRequestMention")]
public class PullRequestMention
{
    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    // Pull table
    public long Pull { get; set; } = DataStore.NoForeignKey;

    // User table
    public long User { get; set; } = DataStore.NoForeignKey;

    private static PullRequestMention GetByPullIdAndUserId(DataStore dataStore, long pullId, long userId)
    {
        var sql = @"SELECT * FROM PullRequestMention WHERE Pull = @PullId AND User = @UserId;";
        var param = new
        {
            PullId = pullId,
            UserId = userId,
        };
        return dataStore.Connection!.QueryFirstOrDefault<PullRequestMention>(sql, param, null);
    }

    public static PullRequestMention AddUserToPull(DataStore dataStore, PullRequest pull, User user)
    {
        var exists = GetByPullIdAndUserId(dataStore, pull.Id, user.Id);
        if (exists is not null)
        {
            // Already an association between this label and this Pull.
            return exists;
        }

        var newPullRequestMention = new PullRequestMention
        {
            Pull = pull.Id,
            User = user.Id,
        };
        newPullRequestMention.Id = dataStore.Connection!.Insert(newPullRequestMention);
        return newPullRequestMention;
    }

    public static IEnumerable<User> GetUsersForPull(DataStore dataStore, PullRequest pull)
    {
        var sql = @"SELECT * FROM User AS U WHERE U.Id IN (SELECT User FROM PullRequestMention WHERE PullRequestMention.Pull = @PullId)";
        var param = new
        {
            PullId = pull.Id,
        };

        Log.Logger()?.ReportDebug(DataStore.GetSqlLogMessage(sql, param));
        return dataStore.Connection!.Query<User>(sql, param, null) ?? Enumerable.Empty<User>();
    }

    public static void DeletePullRequestMentionForPull(DataStore dataStore, PullRequest pull)
    {
        // Delete all PullRequestMention entries that match this Pull Id.
        var sql = @"DELETE FROM PullRequestMention WHERE Pull = $PullId;";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$PullId", pull.Id);
        command.ExecuteNonQuery();
    }
}
