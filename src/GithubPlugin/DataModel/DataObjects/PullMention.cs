// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Dapper;
using Dapper.Contrib.Extensions;

namespace GitHubPlugin.DataModel;

[Table("PullMention")]
public class PullMention
{
    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    // Pull table
    public long Pull { get; set; } = DataStore.NoForeignKey;

    // User table
    public long User { get; set; } = DataStore.NoForeignKey;

    private static PullMention GetByPullIdAndUserId(DataStore dataStore, long pullId, long userId)
    {
        var sql = @"SELECT * FROM PullMention WHERE Pull = @PullId AND User = @UserId;";
        var param = new
        {
            PullId = pullId,
            UserId = userId,
        };
        return dataStore.Connection!.QueryFirstOrDefault<PullMention>(sql, param, null);
    }

    public static PullMention AddUserToPull(DataStore dataStore, PullRequest pull, User user)
    {
        var exists = GetByPullIdAndUserId(dataStore, pull.Id, user.Id);
        if (exists is not null)
        {
            // Already an association between this label and this Pull.
            return exists;
        }

        var newPullMention = new PullMention
        {
            Pull = pull.Id,
            User = user.Id,
        };
        newPullMention.Id = dataStore.Connection!.Insert(newPullMention);
        return newPullMention;
    }

    public static IEnumerable<User> GetUsersForPull(DataStore dataStore, PullRequest pull)
    {
        var sql = @"SELECT * FROM User AS U WHERE U.Id IN (SELECT User FROM PullMention WHERE PullMention.Pull = @PullId)";
        var param = new
        {
            PullId = pull.Id,
        };

        Log.Logger()?.ReportDebug(DataStore.GetSqlLogMessage(sql, param));
        return dataStore.Connection!.Query<User>(sql, param, null) ?? Enumerable.Empty<User>();
    }

    public static void DeletePullMentionForPull(DataStore dataStore, PullRequest pull)
    {
        // Delete all PullMention entries that match this Pull Id.
        var sql = @"DELETE FROM PullMention WHERE Pull = $PullId;";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$PullId", pull.Id);
        command.ExecuteNonQuery();
    }
}
