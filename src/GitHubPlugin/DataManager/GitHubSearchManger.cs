// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubPlugin.Client;
using GitHubPlugin.DataManager;
using GitHubPlugin.DataModel;

namespace GitHubPlugin;

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

    public async Task SearchForGitHubIssuesOrPRs(Octokit.SearchIssuesRequest request, string initiator, SearchCategory category, RequestOptions? options = null)
    {
        Log.Logger()?.ReportInfo(Name, $"Searching for issues or pull requests for widget {initiator}");
        request.State = Octokit.ItemState.Open;
        request.Archived = false;
        request.PerPage = 10;
        request.SortField = Octokit.IssueSearchSort.Updated;
        request.Order = Octokit.SortDirection.Descending;

        var client = await GitHubClientProvider.Instance.GetClientForLoggedInDeveloper(true);

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
