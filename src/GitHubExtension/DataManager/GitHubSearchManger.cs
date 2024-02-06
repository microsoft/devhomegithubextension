// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Client;
using GitHubExtension.DataManager;
using GitHubExtension.DataModel;
using Microsoft.Windows.DevHome.SDK;
using Octokit;

namespace GitHubExtension;

public delegate void SearchManagerResultsAvailableEventHandler(IEnumerable<Octokit.Issue> results, string resultType);

public partial class GitHubSearchManager : IGitHubSearchManager, IDisposable
{
    private static readonly string Name = nameof(GitHubSearchManager);

    public static event SearchManagerResultsAvailableEventHandler? OnResultsAvailable;

    public GitHubSearchManager()
    {
    }

    public static IGitHubSearchManager? CreateInstance()
    {
        try
        {
            return new GitHubSearchManager();
        }
        catch (Exception e)
        {
            Log.Logger()?.ReportError(Name, "Failed creating GitHubSearchManager", e);
            Environment.FailFast(e.Message, e);
            return null;
        }
    }

    public async Task SearchForGitHubIssuesOrPRs(Octokit.SearchIssuesRequest request, string initiator, SearchCategory category, IDeveloperId developerId, RequestOptions? options = null)
    {
        var client = GitHubClientProvider.Instance.GetClient(developerId.Url) ?? throw new InvalidOperationException($"Client does not exist for {developerId.Url}");

        await SearchForGitHubIssuesOrPRs(request, initiator, category, client, options);
    }

    public async Task SearchForGitHubIssuesOrPRs(Octokit.SearchIssuesRequest request, string initiator, SearchCategory category, RequestOptions? options = null)
    {
        var client = await GitHubClientProvider.Instance.GetClientForLoggedInDeveloper(true);

        await SearchForGitHubIssuesOrPRs(request, initiator, category, client, options);
    }

    private async Task SearchForGitHubIssuesOrPRs(Octokit.SearchIssuesRequest request, string initiator, SearchCategory category, GitHubClient client, RequestOptions? options = null)
    {
        Log.Logger()?.ReportInfo(Name, $"Searching for issues or pull requests for widget {initiator}");
        request.State = Octokit.ItemState.Open;
        request.Archived = false;
        request.PerPage = 10;
        request.SortField = Octokit.IssueSearchSort.Updated;
        request.Order = Octokit.SortDirection.Descending;

        // Set is: parameter according to the search category.
        // For the case we are searching for both we don't have to set the parameter
        if (category.Equals(SearchCategory.Issues))
        {
            request.Is = new List<Octokit.IssueIsQualifier>() { Octokit.IssueIsQualifier.Issue };
        }
        else if (category.Equals(SearchCategory.PullRequests))
        {
            request.Is = new List<Octokit.IssueIsQualifier>() { Octokit.IssueIsQualifier.PullRequest };
        }

        var octokitResult = await client.Search.SearchIssues(request);
        if (octokitResult == null)
        {
            Log.Logger()?.ReportDebug($"No issues or PRs found.");
            SendResultsAvailable(new List<Octokit.Issue>(), initiator);
        }
        else
        {
            Log.Logger()?.ReportDebug(Name, $"Results contain {octokitResult.Items.Count} items.");
            SendResultsAvailable(octokitResult.Items, initiator);
        }
    }

    private void SendResultsAvailable(IEnumerable<Octokit.Issue> results, string initiator)
    {
        if (OnResultsAvailable != null)
        {
            Log.Logger()?.ReportInfo(Name, $"Sending search results available Event, of type: {initiator}");
            OnResultsAvailable.Invoke(results, initiator);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
