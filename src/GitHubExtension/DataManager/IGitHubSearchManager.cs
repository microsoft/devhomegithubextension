// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.DataManager;
using Microsoft.Windows.DevHome.SDK;

namespace GitHubExtension;

public interface IGitHubSearchManager : IDisposable
{
    Task SearchForGitHubIssuesOrPRs(Octokit.SearchIssuesRequest request, string initiator, SearchCategory category, RequestOptions? options = null);

    Task SearchForGitHubIssuesOrPRs(Octokit.SearchIssuesRequest request, string initiator, SearchCategory category, IDeveloperId developerId, RequestOptions? options = null);
}
