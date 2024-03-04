// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.DataModel;

namespace GitHubExtension;

public interface IGitHubDataManager : IDisposable
{
    DataStoreOptions DataStoreOptions { get; }

    DateTime LastUpdated { get; }

    Task UpdateAllDataForRepositoryAsync(string owner, string name, RequestOptions? options = null);

    Task UpdateAllDataForRepositoryAsync(string fullName, RequestOptions? options = null);

    Task UpdatePullRequestsForRepositoryAsync(string owner, string name, RequestOptions? options = null);

    Task UpdatePullRequestsForRepositoryAsync(string fullName, RequestOptions? options = null);

    Task UpdateIssuesForRepositoryAsync(string owner, string name, RequestOptions? options = null);

    Task UpdateIssuesForRepositoryAsync(string fullName, RequestOptions? options = null);

    Task UpdatePullRequestsForLoggedInDeveloperIdsAsync();

    Task UpdateReleasesForRepositoryAsync(string owner, string name, RequestOptions? options = null);

    IEnumerable<Repository> GetRepositories();

    IEnumerable<User> GetDeveloperUsers();

    IEnumerable<Notification> GetNotifications(DateTime? since = null, bool includeToasted = false);

    Repository? GetRepository(string owner, string name);

    Repository? GetRepository(string fullName);
}
