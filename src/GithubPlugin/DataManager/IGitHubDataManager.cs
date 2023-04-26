// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubPlugin.DataModel;

namespace GitHubPlugin;

public interface IGitHubDataManager : IDisposable
{
    DataStoreOptions DataStoreOptions { get; }

    DateTime LastUpdated { get; }

    Task UpdateAllDataForRepositoryAsync(string owner, string name, RequestOptions? options = null);

    Task UpdateAllDataForRepositoryAsync(string fullName, RequestOptions? options = null);

    Task UpdatePullRequestsForRepositoryAsync(string owner, string name, RequestOptions? options = null);

    Task UpdateMentionedInAsync(string owner, string name, RequestOptions? options = null);

    Task UpdateAssignedToAsync(string assignedToUser, RequestOptions? options = null);

    Task UpdatePullRequestsReviewRequestedForRepositoryAsync(string referredUser, RequestOptions? options = null);

    Task UpdatePullRequestsForRepositoryAsync(string fullName, RequestOptions? options = null);

    Task UpdateIssuesForRepositoryAsync(string owner, string name, RequestOptions? options = null);

    Task UpdateIssuesForRepositoryAsync(string fullName, RequestOptions? options = null);

    Task UpdatePullRequestsForLoggedInDeveloperIdsAsync();

    IEnumerable<Repository> GetRepositories();

    IEnumerable<User> GetDeveloperUsers();

    IEnumerable<Notification> GetNotifications(DateTime? since = null, bool includeToasted = false);

    Repository? GetRepository(string owner, string name);

    Repository? GetRepository(string fullName);

    IEnumerable<Issue> GetIssuesMentionedIn();

    IEnumerable<Issue> GetIssuesAssignedTo();
}
