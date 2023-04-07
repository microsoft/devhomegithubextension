// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Dapper;
using Dapper.Contrib.Extensions;

namespace GitHubPlugin.DataModel;

[Table("IssueLabel")]
public class IssueLabel
{
    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    // Issue table
    public long Issue { get; set; } = DataStore.NoForeignKey;

    // Label table
    public long Label { get; set; } = DataStore.NoForeignKey;

    private static IssueLabel? GetByIssueIdAndLabelId(DataStore dataStore, long issueId, long labelId)
    {
        var sql = @"SELECT * FROM IssueLabel WHERE Issue = @IssueId AND Label = @LabelId;";
        var param = new
        {
            IssueId = issueId,
            LabelId = labelId,
        };
        return dataStore.Connection!.QueryFirstOrDefault<IssueLabel>(sql, param, null);
    }

    public static IssueLabel AddLabelToIssue(DataStore dataStore, Issue issue, Label label)
    {
        var exists = GetByIssueIdAndLabelId(dataStore, issue.Id, label.Id);
        if (exists is not null)
        {
            // Already an association between this label and this issue.
            return exists;
        }

        var newIssueLabel = new IssueLabel
        {
            Issue = issue.Id,
            Label = label.Id,
        };
        newIssueLabel.Id = dataStore.Connection!.Insert(newIssueLabel);
        return newIssueLabel;
    }

    public static IEnumerable<Label> GetLabelsForIssue(DataStore dataStore, Issue issue)
    {
        var sql = @"SELECT * FROM Label AS L WHERE L.Id IN (SELECT Label FROM IssueLabel WHERE IssueLabel.Issue = @IssueId)";
        var param = new
        {
            IssueId = issue.Id,
        };

        Log.Logger()?.ReportDebug(DataStore.GetSqlLogMessage(sql, param));
        return dataStore.Connection!.Query<Label>(sql, param, null) ?? Enumerable.Empty<Label>();
    }

    public static void DeleteIssueLabelsForIssue(DataStore dataStore, Issue issue)
    {
        // Delete all IssueLabel entries that match this Issue Id.
        var sql = @"DELETE FROM IssueLabel WHERE Issue = $IssueId;";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$IssueId", issue.Id);
        command.ExecuteNonQuery();
    }
}
