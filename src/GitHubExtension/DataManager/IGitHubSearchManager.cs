// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubExtension.DataManager;
using GitHubExtension.DataModel;

namespace GitHubExtension;

public interface IGitHubSearchManager : IDisposable
{
    Task SearchForGitHubIssuesOrPRs(Octokit.SearchIssuesRequest request, string initiator, SearchCategory category, RequestOptions? options = null);
}
