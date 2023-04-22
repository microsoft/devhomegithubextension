// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json.Nodes;
using GitHubPlugin.Client;
using GitHubPlugin.DataManager;
using GitHubPlugin.Helpers;
using Octokit;

namespace GitHubPlugin.Widgets;
internal class GithubIssuesWidget : GithubWidget
{
    protected static readonly new string Name = nameof(GithubIssuesWidget);

    public GithubIssuesWidget()
    : base()
    {
        GitHubDataManager.OnUpdate += DataManagerUpdateHandler;
    }

    ~GithubIssuesWidget()
    {
        GitHubDataManager.OnUpdate -= DataManagerUpdateHandler;
    }

    public override void DeleteWidget(string widgetId, string customState)
    {
        // Remove event handler
        GitHubDataManager.OnUpdate -= DataManagerUpdateHandler;
        base.DeleteWidget(widgetId, customState);
    }

    public override void RequestContentData()
    {
        if (RepositoryUrl == string.Empty)
        {
            // Nothing to request.
            return;
        }

        // Throttle protection against a widget trigging rapid data updates.
        if (DateTime.Now - LastUpdated < WidgetDataRequestMinTime)
        {
            Log.Logger()?.ReportDebug(Name, ShortId, "Data request too soon, skipping.");
        }

        try
        {
            Log.Logger()?.ReportDebug(Name, ShortId, $"Requesting data update for {GetOwner()}/{GetRepo()}");
            var requestOptions = new RequestOptions
            {
                // We are only interested in getting the first 10 issues. Repositories can have
                // hundreds and thousands of issues open, and the widget can only display a small
                // number of them. We also don't need all of the issues possible, just the most
                // recent which are likely of interest to the developer to watch for new issues.
                SearchIssuesRequest = new SearchIssuesRequest
                {
                    State = ItemState.Open,
                    Type = IssueTypeQualifier.Issue,
                    SortField = IssueSearchSort.Created,
                    Order = SortDirection.Descending,
                    PerPage = 10,
                    Page = 1,
                },
                UsePublicClientAsFallback = true,
            };

            var dataManager = GitHubDataManager.CreateInstance();
            _ = dataManager?.UpdateIssuesForRepositoryAsync(GetOwner(), GetRepo(), requestOptions);
            Log.Logger()?.ReportInfo(Name, ShortId, $"Requested data update for {GetOwner()}/{GetRepo()}");
            DataState = WidgetDataState.Requested;
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Failed requesting data update.", ex);
        }
    }

    public override void LoadContentData()
    {
        if (RepositoryUrl == string.Empty)
        {
            ContentData = string.Empty;
            DataState = WidgetDataState.Okay;
            return;
        }

        Log.Logger()?.ReportDebug(Name, ShortId, "Getting Data for Issues");

        try
        {
            using var dataManager = GitHubDataManager.CreateInstance();
            var repository = dataManager!.GetRepository(GetOwner(), GetRepo());
            var issues = repository is null ? Enumerable.Empty<DataModel.Issue>() : repository.Issues;

            var issuesData = new JsonObject();
            var issuesArray = new JsonArray();
            foreach (var issueItem in issues)
            {
                var issue = new JsonObject
                {
                    { "title", issueItem.Title },
                    { "url", issueItem.HtmlUrl },
                    { "number", issueItem.Number },
                    { "date", issueItem.CreatedAt.ToLocalTime().ToStringInvariant() },
                    { "user", issueItem.Author.Login },
                    { "avatar", issueItem.Author.AvatarUrl },
                };

                var labels = issueItem.Labels.ToList();
                var issueLabels = new JsonArray();
                foreach (var label in labels)
                {
                    var issueLabel = new JsonObject
                    {
                        { "name", label.Name },
                        { "color", label.Color },
                    };

                    ((IList<JsonNode?>)issueLabels).Add(issueLabel);
                }

                issue.Add("labels", issueLabels);

                ((IList<JsonNode?>)issuesArray).Add(issue);
            }

            issuesData.Add("issues", issuesArray);
            issuesData.Add("selected_repo", repository?.FullName ?? string.Empty);
            issuesData.Add("is_loading_data", DataState == WidgetDataState.Unknown);

            LastUpdated = DateTime.Now;
            DataState = WidgetDataState.Okay;
            ContentData = issuesData.ToJsonString();
        }
        catch (Exception e)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Error retrieving data.", e);
            DataState = WidgetDataState.Failed;
            return;
        }
    }

    public override string GetTemplatePath(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.SignIn => @"Widgets\Templates\GitHubSignInTemplate.json",
            WidgetPageState.Configure => @"Widgets\Templates\GitHubIssuesConfigurationTemplate.json",
            WidgetPageState.Content => @"Widgets\Templates\GitHubIssuesTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\GithubLoadingTemplate.json",
            _ => throw new NotImplementedException(),
        };
    }

    public override string GetData(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.SignIn => new JsonObject { { "message", Resources.GetResource(@"Widget_Template/SignInRequired", Log.Logger()) } }.ToJsonString(),
            WidgetPageState.Configure => GetConfiguration(RepositoryUrl),
            WidgetPageState.Content => ContentData,
            WidgetPageState.Loading => EmptyJson,
            _ => throw new NotImplementedException(Page.GetType().Name),
        };
    }

    private void DataManagerUpdateHandler(object? source, DataManagerUpdateEventArgs e)
    {
        Log.Logger()?.ReportDebug(Name, ShortId, $"Data Update Event: Kind={e.Kind} Info={e.Repository} Context={string.Join(",", e.Context)}");
        if (e.Kind == DataManagerUpdateKind.Repository && !string.IsNullOrEmpty(RepositoryUrl))
        {
            var fullName = Validation.ParseFullNameFromGitHubURL(RepositoryUrl);
            if (fullName == e.Repository && e.Context.Contains("Issues"))
            {
                Log.Logger()?.ReportInfo(Name, ShortId, $"Received matching repository update event.");
                LoadContentData();
                UpdateActivityState();
            }
        }
    }
}
