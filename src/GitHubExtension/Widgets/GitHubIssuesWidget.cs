// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json.Nodes;
using GitHubExtension.Client;
using GitHubExtension.DataManager;
using GitHubExtension.Helpers;
using GitHubExtension.Widgets.Enums;
using Microsoft.Windows.Widgets.Providers;
using Octokit;

namespace GitHubExtension.Widgets;
internal class GitHubIssuesWidget : GitHubWidget
{
    private readonly string issuesIconData = IconLoader.GetIconAsBase64("issues.png");

    protected static readonly new string Name = nameof(GitHubIssuesWidget);

    public GitHubIssuesWidget()
    : base()
    {
        GitHubDataManager.OnUpdate += DataManagerUpdateHandler;
    }

    ~GitHubIssuesWidget()
    {
        GitHubDataManager.OnUpdate -= DataManagerUpdateHandler;
    }

    public override void DeleteWidget(string widgetId, string customState)
    {
        // Remove event handler.
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

        if (ActivityState == WidgetActivityState.Configure)
        {
            return;
        }

        try
        {
            Log.Logger()?.ReportDebug(Name, ShortId, $"Requesting data update for {GetOwner()}/{GetRepo()}");
            var requestOptions = new RequestOptions
            {
                UsePublicClientAsFallback = true,
            };

            var issueQuery = GetIssueQuery();
            if (!string.IsNullOrEmpty(issueQuery))
            {
                // If a query was provided, use that query for parameters.
                requestOptions.SearchIssuesRequest = new SearchIssuesRequest(issueQuery);
            }
            else
            {
                // Default query parameters.
                // We are only interested in getting the first 10 issues. Repositories can have
                // hundreds and thousands of issues open, and the widget can only display a small
                // number of them. We also don't need all of the issues possible, just the most
                // recent which are likely of interest to the developer to watch for new issues.
                requestOptions.SearchIssuesRequest = new SearchIssuesRequest
                {
                    State = ItemState.Open,
                    Type = IssueTypeQualifier.Issue,
                    SortField = IssueSearchSort.Created,
                    Order = SortDirection.Descending,
                    PerPage = 10,
                    Page = 1,
                };
            }

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

            IEnumerable<DataModel.Issue> issues;
            if (repository is null)
            {
                issues = Enumerable.Empty<DataModel.Issue>();
            }
            else
            {
                var issueQuery = GetIssueQuery();
                if (!string.IsNullOrEmpty(issueQuery))
                {
                    issues = repository.GetIssuesForQuery(issueQuery);
                }
                else
                {
                    issues = repository.Issues;
                }
            }

            var issuesData = new JsonObject();
            var issuesArray = new JsonArray();
            foreach (var issueItem in issues)
            {
                var issue = new JsonObject
                {
                    { "title", issueItem.Title },
                    { "url", issueItem.HtmlUrl },
                    { "number", issueItem.Number },
                    { "date", TimeSpanHelper.DateTimeOffsetToDisplayString(issueItem.CreatedAt, Log.Logger()) },
                    { "user", issueItem.Author.Login },
                    { "avatar", issueItem.Author.AvatarUrl },
                    { "icon", issuesIconData },
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
            issuesData.Add("issues_icon_data", issuesIconData);

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
            WidgetPageState.Loading => @"Widgets\Templates\GitHubLoadingTemplate.json",
            _ => throw new NotImplementedException(),
        };
    }

    public override string GetData(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.SignIn => GetSignIn(),
            WidgetPageState.Configure => GetConfiguration(RepositoryUrl),
            WidgetPageState.Content => ContentData,
            WidgetPageState.Loading => new JsonObject { { "configuring", true } }.ToJsonString(),
            _ => throw new NotImplementedException(Page.GetType().Name),
        };
    }

    private void DataManagerUpdateHandler(object? source, DataManagerUpdateEventArgs e)
    {
        Log.Logger()?.ReportDebug(Name, ShortId, $"Data Update Event: Kind={e.Kind} Info={e.Description} Context={string.Join(",", e.Context)}");
        if (e.Kind == DataManagerUpdateKind.Repository && !string.IsNullOrEmpty(RepositoryUrl))
        {
            var fullName = Validation.ParseFullNameFromGitHubURL(RepositoryUrl);
            if (fullName == e.Description && e.Context.Contains("Issues"))
            {
                Log.Logger()?.ReportInfo(Name, ShortId, $"Received matching repository update event.");
                LoadContentData();
                UpdateActivityState();
            }
        }
    }

    public override void OnCustomizationRequested(WidgetCustomizationRequestedArgs customizationRequestedArgs)
    {
        SavedRepositoryUrl = RepositoryUrl;
        SetConfigure();
    }
}
