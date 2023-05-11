// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubPlugin.DataManager;
using GitHubPlugin.DataModel;

namespace GitHubPlugin;

public interface IGitHubSearchManager : IDisposable
{
    Task SearchForGithubIssuesOrPRs(Octokit.SearchIssuesRequest request, string initiator, SearchCategory category, RequestOptions? options = null);
}
