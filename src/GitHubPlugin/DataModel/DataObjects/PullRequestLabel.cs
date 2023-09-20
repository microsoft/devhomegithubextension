// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Dapper;
using Dapper.Contrib.Extensions;

namespace GitHubPlugin.DataModel;

[Table("PullRequestLabel")]
public class PullRequestLabel
{
    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    // PullRequest table
    public long PullRequest { get; set; } = DataStore.NoForeignKey;

    // Label table
    public long Label { get; set; } = DataStore.NoForeignKey;

    private static PullRequestLabel GetByPullRequestIdAndLabelId(DataStore dataStore, long pullId, long labelId)
    {
        var sql = @"SELECT * FROM PullRequestLabel WHERE PullRequest = @PullId AND Label = @LabelId;";
        var param = new
        {
            PullId = pullId,
            LabelId = labelId,
        };
        return dataStore.Connection!.QueryFirstOrDefault<PullRequestLabel>(sql, param, null);
    }

    public static PullRequestLabel AddLabelToPullRequest(DataStore dataStore, PullRequest pull, Label label)
    {
        var exists = GetByPullRequestIdAndLabelId(dataStore, pull.Id, label.Id);
        if (exists is not null)
        {
            // Already an association between this label and this pull request.
            return exists;
        }

        var newPullLabel = new PullRequestLabel
        {
            PullRequest = pull.Id,
            Label = label.Id,
        };
        newPullLabel.Id = dataStore.Connection!.Insert(newPullLabel);
        return newPullLabel;
    }

    public static IEnumerable<Label> GetLabelsForPullRequest(DataStore dataStore, PullRequest pull)
    {
        var sql = @"SELECT * FROM Label AS L WHERE L.Id IN (SELECT Label FROM PullRequestLabel WHERE PullRequestLabel.PullRequest = @PullId)";
        var param = new
        {
            PullId = pull.Id,
        };

        Log.Logger()?.ReportDebug(DataStore.GetSqlLogMessage(sql, param));
        return dataStore.Connection!.Query<Label>(sql, param, null) ?? Enumerable.Empty<Label>();
    }

    public static void DeletePullRequestLabelsForPullRequest(DataStore dataStore, PullRequest pullRequest)
    {
        // Delete all PullRequestLabel entries that match this PullRequest Id.
        var sql = @"DELETE FROM PullRequestLabel WHERE PullRequest = $PullRequestId;";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$PullRequestId", pullRequest.Id);
        command.ExecuteNonQuery();
    }
}
