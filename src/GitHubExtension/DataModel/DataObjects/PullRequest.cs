﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Dapper;
using Dapper.Contrib.Extensions;
using GitHubExtension.Helpers;

namespace GitHubExtension.DataModel;

[Table("PullRequest")]
public class PullRequest
{
    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    public long InternalId { get; set; } = DataStore.NoForeignKey;

    public long Number { get; set; } = DataStore.NoForeignKey;

    // Repository table
    public long RepositoryId { get; set; } = DataStore.NoForeignKey;

    // User table
    public long AuthorId { get; set; } = DataStore.NoForeignKey;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string HeadSha { get; set; } = string.Empty;

    public long Merged { get; set; } = DataStore.NoForeignKey;

    public long Mergeable { get; set; } = DataStore.NoForeignKey;

    public string MergeableState { get; set; } = string.Empty;

    public long CommitCount { get; set; } = DataStore.NoForeignKey;

    public string HtmlUrl { get; set; } = string.Empty;

    public long Locked { get; set; } = DataStore.NoForeignKey;

    public long Draft { get; set; } = DataStore.NoForeignKey;

    public long TimeCreated { get; set; } = DataStore.NoForeignKey;

    public long TimeUpdated { get; set; } = DataStore.NoForeignKey;

    public long TimeMerged { get; set; } = DataStore.NoForeignKey;

    public long TimeClosed { get; set; } = DataStore.NoForeignKey;

    // Label IDs are a string concatenation of Label internalIds.
    // We need to duplicate this data in order to properly do inserts and
    // to compare two objects for changes in order to add/remove associations.
    public string LabelIds { get; set; } = string.Empty;

    // Same use as Label IDs.
    public string AssigneeIds { get; set; } = string.Empty;

    [Write(false)]
    private DataStore? DataStore { get; set; }

    [Write(false)]
    [Computed]
    public DateTime CreatedAt => TimeCreated.ToDateTime();

    [Write(false)]
    [Computed]
    public DateTime UpdatedAt => TimeUpdated.ToDateTime();

    [Write(false)]
    [Computed]
    public DateTime MergedAt => TimeMerged.ToDateTime();

    [Write(false)]
    [Computed]
    public DateTime ClosedAt => TimeClosed.ToDateTime();

    // Derived Properties so consumers of these objects do not
    // need to do further queries of the datastore.
    [Write(false)]
    [Computed]
    public IEnumerable<Label> Labels
    {
        get
        {
            if (DataStore == null)
            {
                return Enumerable.Empty<Label>();
            }
            else
            {
                return PullRequestLabel.GetLabelsForPullRequest(DataStore, this) ?? Enumerable.Empty<Label>();
            }
        }
    }

    [Write(false)]
    [Computed]
    public IEnumerable<User> Assignees
    {
        get
        {
            if (DataStore == null)
            {
                return Enumerable.Empty<User>();
            }
            else
            {
                return PullRequestAssign.GetUsersForPullRequest(DataStore, this) ?? Enumerable.Empty<User>();
            }
        }
    }

    [Write(false)]
    [Computed]
    public Repository Repository
    {
        get
        {
            if (DataStore == null)
            {
                return new Repository();
            }
            else
            {
                return Repository.GetById(DataStore, RepositoryId) ?? new Repository();
            }
        }
    }

    [Write(false)]
    [Computed]
    public User Author
    {
        get
        {
            if (DataStore == null)
            {
                return new User();
            }
            else
            {
                return User.GetById(DataStore, AuthorId) ?? new User();
            }
        }
    }

    /// <summary>
    /// Gets all CheckRuns associated with this pull request.
    /// </summary>
    [Write(false)]
    [Computed]
    public IEnumerable<CheckRun> Checks
    {
        get
        {
            if (DataStore == null)
            {
                return Enumerable.Empty<CheckRun>();
            }
            else
            {
                return CheckRun.GetCheckRunsForPullRequest(DataStore, this) ?? Enumerable.Empty<CheckRun>();
            }
        }
    }

    /// <summary>
    /// Gets all failed CheckRuns associated with this pull request.
    /// </summary>
    [Write(false)]
    [Computed]
    public IEnumerable<CheckRun> FailedChecks
    {
        get
        {
            if (DataStore == null)
            {
                return Enumerable.Empty<CheckRun>();
            }
            else
            {
                return CheckRun.GetFailedCheckRunsForPullRequest(DataStore, this) ?? Enumerable.Empty<CheckRun>();
            }
        }
    }

    /// <summary>
    /// Gets the least-completed run status of all check runs associated with this pull request.
    /// </summary>
    /// <remarks>
    /// If status is "Completed" then it means all runs have completed. If one or more runs is not
    /// completed it will be in one of the other states that indicates in progress or queued.
    /// A status of "None" means there are no checks associated with this PR or we have not added them
    /// to the datastore. When checking the for the ChecksStatus or result of a pull request, always
    /// check this property. It must be "Completed", meaning all checks have run, before success can
    /// be determined. If the Status is not yet completed, check for failed checks during runs by
    /// examining the FailedChecks.Count() value.
    /// </remarks>
    [Write(false)]
    [Computed]
    public CheckStatus ChecksStatus
    {
        get
        {
            if (DataStore == null)
            {
                return CheckStatus.Unknown;
            }
            else
            {
                return CheckRun.GetCheckRunStatusForPullRequest(DataStore, this);
            }
        }
    }

    [Write(false)]
    [Computed]
    public CheckStatus CheckSuiteStatus
    {
        get
        {
            if (DataStore == null)
            {
                return CheckStatus.Unknown;
            }
            else
            {
                return CheckSuite.GetCheckStatusForPullRequest(DataStore, this);
            }
        }
    }

    /// <summary>
    /// Gets the least-successful conclusion of all completed CheckRuns associated with this pull request.
    /// </summary>
    /// <remarks>
    /// A "Success" conclusion is not accurate unless ChecksStatus property is also "Completed". Any
    /// failures that occur mid-run will be accurately returned as the CheckConclusion.
    /// </remarks>
    [Write(false)]
    [Computed]
    public CheckConclusion ChecksConclusion
    {
        get
        {
            if (DataStore == null)
            {
                return CheckConclusion.Unknown;
            }
            else
            {
                return CheckRun.GetCheckRunConclusionForPullRequest(DataStore, this);
            }
        }
    }

    [Write(false)]
    [Computed]
    public CheckConclusion CheckSuiteConclusion
    {
        get
        {
            if (DataStore == null)
            {
                return CheckConclusion.Unknown;
            }
            else
            {
                return CheckSuite.GetCheckConclusionForPullRequest(DataStore, this);
            }
        }
    }

    [Write(false)]
    [Computed]
    public CommitState CommitState
    {
        get
        {
            if (DataStore == null)
            {
                return CommitState.Unknown;
            }
            else
            {
                return CommitCombinedStatus.GetCommitState(DataStore, this);
            }
        }
    }

    [Write(false)]
    [Computed]
    public PullRequestStatus? PullRequestStatus
    {
        get
        {
            if (DataStore == null)
            {
                return null;
            }
            else
            {
                return PullRequestStatus.Get(DataStore, this);
            }
        }
    }

    public override string ToString() => $"{Number}: {Title}";

    // Create pull request from OctoKit pull request data
    private static PullRequest CreateFromOctokitPullRequest(DataStore dataStore, Octokit.PullRequest okitPull, long repositoryId)
    {
        var pull = new PullRequest
        {
            DataStore = dataStore,
            InternalId = okitPull.Id,
            Number = okitPull.Number,
            Title = okitPull.Title ?? string.Empty,
            Body = okitPull.Body ?? string.Empty,
            State = okitPull.State.Value.ToString(),
            HeadSha = okitPull.Head.Sha ?? string.Empty,
            Merged = okitPull.Merged ? 1 : 0,
            Mergeable = (okitPull.Mergeable is not null && okitPull.Mergeable == true) ? 1 : 0,
            MergeableState = okitPull.MergeableState.HasValue ? okitPull.MergeableState.Value.ToString() : string.Empty,
            CommitCount = okitPull.Commits,
            HtmlUrl = okitPull.HtmlUrl ?? string.Empty,
            Locked = okitPull.Locked ? 1 : 0,
            Draft = okitPull.Draft ? 1 : 0,
            TimeCreated = okitPull.CreatedAt.DateTime.ToDataStoreInteger(),
            TimeUpdated = okitPull.UpdatedAt.DateTime.ToDataStoreInteger(),
            TimeMerged = okitPull.MergedAt.HasValue ? okitPull.MergedAt.Value.DateTime.ToDataStoreInteger() : 0,
            TimeClosed = okitPull.ClosedAt.HasValue ? okitPull.ClosedAt.Value.DateTime.ToDataStoreInteger() : 0,
        };

        // Labels are a string concat of label internal ids.
        var labels = new List<string>();
        foreach (var label in okitPull.Labels)
        {
            labels.Add(label.Id.ToStringInvariant());
            Label.GetOrCreateByOctokitLabel(dataStore, label);
        }

        pull.LabelIds = string.Join(",", labels);

        // Assignees are a string concat of User internal ids.
        var assignees = new List<string>();
        foreach (var user in okitPull.Assignees)
        {
            assignees.Add(user.Id.ToStringInvariant());
            User.GetOrCreateByOctokitUser(dataStore, user);
        }

        pull.AssigneeIds = string.Join(",", assignees);

        // Owner is a rowid in the User table
        var author = User.GetOrCreateByOctokitUser(dataStore, okitPull.User);
        pull.AuthorId = author.Id;

        // Repo is a row id in the Repository table.
        // It is likely the case that we already know the repository id (such as when querying pulls for a repository).
        if (repositoryId != DataStore.NoForeignKey)
        {
            pull.RepositoryId = repositoryId;
        }
        else if (okitPull.Base.Repository is not null)
        {
            // Use the base repository for the pull request.
            // This PR may be a private fork and Head and Base may be different.
            var repo = Repository.GetOrCreateByOctokitRepository(dataStore, okitPull.Base.Repository);
            pull.RepositoryId = repo.Id;
        }

        return pull;
    }

    private static PullRequest AddOrUpdatePullRequest(DataStore dataStore, PullRequest pull)
    {
        // Check for existing pull request data.
        var existingPull = GetByInternalId(dataStore, pull.InternalId);
        if (existingPull is not null)
        {
            if (pull.TimeUpdated > existingPull.TimeUpdated)
            {
                pull.Id = existingPull.Id;
                dataStore.Connection!.Update(pull);
                pull.DataStore = dataStore;

                if (pull.LabelIds != existingPull.LabelIds)
                {
                    UpdateLabelsForPullRequest(dataStore, pull);
                }

                if (pull.AssigneeIds != existingPull.AssigneeIds)
                {
                    UpdateAssigneesForPullRequest(dataStore, pull);
                }

                return pull;
            }
            else
            {
                return existingPull;
            }
        }

        // No existing pull request, add it.
        pull.Id = dataStore.Connection!.Insert(pull);

        // Now that we have an inserted Id, we can associate labels and assignees.
        UpdateLabelsForPullRequest(dataStore, pull);
        UpdateAssigneesForPullRequest(dataStore, pull);

        pull.DataStore = dataStore;

        return pull;
    }

    public static PullRequest? GetById(DataStore dataStore, long id)
    {
        var pull = dataStore.Connection!.Get<PullRequest>(id);
        if (pull is not null)
        {
            // Add Datastore so this object can make internal queries.
            pull.DataStore = dataStore;
        }

        return pull;
    }

    public static PullRequest? GetByInternalId(DataStore dataStore, long internalId)
    {
        var sql = @"SELECT * FROM PullRequest WHERE InternalId = @InternalId;";
        var param = new
        {
            InternalId = internalId,
        };

        var pull = dataStore.Connection!.QueryFirstOrDefault<PullRequest>(sql, param, null);
        if (pull is not null)
        {
            // Add Datastore so this object can make internal queries.
            pull.DataStore = dataStore;
        }

        return pull;
    }

    public static PullRequest GetOrCreateByOctokitPullRequest(DataStore dataStore, Octokit.PullRequest octokitPullRequest, long repositoryId = DataStore.NoForeignKey)
    {
        var newPull = CreateFromOctokitPullRequest(dataStore, octokitPullRequest, repositoryId);
        return AddOrUpdatePullRequest(dataStore, newPull);
    }

    public static IEnumerable<PullRequest> GetAllForRepository(DataStore dataStore, Repository repository)
    {
        var sql = @"SELECT * FROM PullRequest WHERE RepositoryId = @RepositoryId ORDER BY TimeUpdated DESC;";
        var param = new
        {
            RepositoryId = repository.Id,
        };

        Log.Logger()?.ReportDebug(DataStore.GetSqlLogMessage(sql, param));
        var pulls = dataStore.Connection!.Query<PullRequest>(sql, param, null) ?? Enumerable.Empty<PullRequest>();
        foreach (var pull in pulls)
        {
            pull.DataStore = dataStore;
        }

        return pulls;
    }

    public static IEnumerable<PullRequest> GetAllForUser(DataStore dataStore, User user)
    {
        var sql = @"SELECT * FROM PullRequest WHERE AuthorId = @AuthorId;";
        var param = new
        {
            AuthorId = user.Id,
        };

        Log.Logger()?.ReportDebug(DataStore.GetSqlLogMessage(sql, param));
        var pulls = dataStore.Connection!.Query<PullRequest>(sql, param, null) ?? Enumerable.Empty<PullRequest>();
        foreach (var pull in pulls)
        {
            pull.DataStore = dataStore;
        }

        return pulls;
    }

    private static void UpdateLabelsForPullRequest(DataStore dataStore, PullRequest pullRequest)
    {
        // Delete existing labels for this Pull Request and add new ones.
        PullRequestLabel.DeletePullRequestLabelsForPullRequest(dataStore, pullRequest);
        foreach (var label in pullRequest.LabelIds.Split(','))
        {
            if (long.TryParse(label, out var internalId))
            {
                var labelObj = Label.GetByInternalId(dataStore, internalId);
                if (labelObj is not null)
                {
                    PullRequestLabel.AddLabelToPullRequest(dataStore, pullRequest, labelObj);
                }
            }
        }
    }

    private static void UpdateAssigneesForPullRequest(DataStore dataStore, PullRequest pullRequest)
    {
        // Delete existing assignees for this Pull Request and add new ones.
        PullRequestAssign.DeletePullRequestAssignForPullRequest(dataStore, pullRequest);
        foreach (var user in pullRequest.AssigneeIds.Split(','))
        {
            if (long.TryParse(user, out var internalId))
            {
                var userObj = User.GetByInternalId(dataStore, internalId);
                if (userObj is not null)
                {
                    PullRequestAssign.AddUserToPullRequest(dataStore, pullRequest, userObj);
                }
            }
        }
    }
}
