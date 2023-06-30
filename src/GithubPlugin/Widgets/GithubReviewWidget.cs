// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GitHubPlugin.DataManager;
using GitHubPlugin.Helpers;
using Microsoft.Windows.Widgets.Providers;
using Octokit;

namespace GitHubPlugin.Widgets;
internal class GithubReviewWidget : GithubWidget
{
    private static Dictionary<string, string> Templates { get; set; } = new ();

    protected static readonly new string Name = nameof(GithubReviewWidget);

    private string referredName = string.Empty;

    private string ReferredName
    {
        get
        {
            if (string.IsNullOrEmpty(referredName))
            {
                GetReferredName();
            }

            return referredName;
        }
        set => referredName = value;
    }

    private string ConfigurationState
    {
        get => State();

        set => SetState(value);
    }

    public GithubReviewWidget()
        : base()
    {
        GitHubSearchManager.OnResultsAvailable += SearchManagerResultsAvailableHandler;
    }

    ~GithubReviewWidget()
    {
        GitHubSearchManager.OnResultsAvailable -= SearchManagerResultsAvailableHandler;
    }

    private void GetReferredName()
    {
        var devIds = DeveloperId.DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIdsInternal();
        if ((devIds != null) && devIds.Any())
        {
            referredName = devIds.First().LoginId;
        }
    }

    public override void DeleteWidget(string widgetId, string customState)
    {
        // Remove event handler.
        GitHubSearchManager.OnResultsAvailable -= SearchManagerResultsAvailableHandler;
        base.DeleteWidget(widgetId, customState);
    }

    public override void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        Enabled = contextChangedArgs.WidgetContext.IsActive;
        UpdateActivityState();
    }

    public new void UpdateActivityState()
    {
        // State logic for the Widget: (no configuration is needed)
        // Signed in -> Active / Inactive per widget host.
        if (!IsUserLoggedIn())
        {
            SetSignIn();
            return;
        }

        ConfigurationState = "Activated";

        if (Enabled)
        {
            if (ContentData == EmptyJson)
            {
                SetLoading();
            }
            else
            {
                SetActive();
            }

            return;
        }

        SetInactive();
    }

    public override void RequestContentData()
    {
        // Throttle protection against a widget trigging rapid data updates.
        if (DateTime.Now - LastUpdated < WidgetDataRequestMinTime)
        {
            Log.Logger()?.ReportDebug(Name, ShortId, "Data request too soon, skipping.");
        }

        try
        {
            Log.Logger()?.ReportInfo(Name, ShortId, $"Requesting search for mentioned user {referredName}");
            var requestOptions = new RequestOptions
            {
                ApiOptions = new ApiOptions
                {
                    PageSize = 10,
                    PageCount = 1,
                    StartPage = 1,
                },
                UsePublicClientAsFallback = true,
            };

            SearchIssuesRequest request = new SearchIssuesRequest($"review-requested:{ReferredName}");
            var searchManager = GitHubSearchManager.CreateInstance();
            searchManager?.SearchForGithubIssuesOrPRs(request, Name, SearchCategory.PullRequests, requestOptions);
            Log.Logger()?.ReportInfo(Name, ShortId, $"Requested search for {referredName}");
            DataState = WidgetDataState.Requested;
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Failed requesting search.", ex);
        }
    }

    public override void LoadContentData()
    {
        var issuesData = new JsonObject
        {
            { "openCount", 0 },
            { "items", new JsonArray() },
            { "referredName", ReferredName },
            { "titleIconUrl", IconLoader.GetIconAsBase64("pulls.png") },
            { "is_loading_data", true },
        };
        ContentData = issuesData.ToJsonString();
    }

    public void LoadContentData(IEnumerable<Octokit.Issue> items)
    {
        Log.Logger()?.ReportDebug(Name, ShortId, "Getting Data for Mentioned in Widget");

        try
        {
            using var dataManager = GitHubDataManager.CreateInstance();

            var issuesData = new JsonObject();
            var issuesArray = new JsonArray();
            issuesData.Add("openCount", items.Count());
            foreach (var item in items)
            {
                var issue = new JsonObject
                {
                    { "title", item.Title },
                    { "url", item.HtmlUrl },
                    { "number", item.Number },
                    { "date", TimeSpanHelper.DateTimeOffsetToDisplayString(item.UpdatedAt, Log.Logger()) },
                    { "user", item.User.Login },
                    { "avatar", item.User.AvatarUrl },
                    { "iconUrl", IconLoader.GetIconAsBase64("pulls.png") },
                };

                var issueLabels = new JsonArray();
                foreach (var label in item.Labels)
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

                var parsedUrl = item.HtmlUrl.Split('/');
                var repo = parsedUrl[3] + '/' + parsedUrl[4];
                issue.Add("repo", repo);
            }

            issuesData.Add("items", issuesArray);
            issuesData.Add("referredName", ReferredName);
            issuesData.Add("titleIconUrl", IconLoader.GetIconAsBase64("pulls.png"));

            LastUpdated = DateTime.Now;
            ContentData = issuesData.ToJsonString();
            DataState = WidgetDataState.Okay;
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
            WidgetPageState.Configure => @"Widgets\Templates\GithubReviewConfigurationTemplate.json",
            WidgetPageState.Content => @"Widgets\Templates\GitHubReviewTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\GithubLoadingTemplate.json",
            _ => throw new NotImplementedException(),
        };
    }

    public override string GetData(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.SignIn => new JsonObject { { "message", Resources.GetResource(@"Widget_Template/SignInRequired", Log.Logger()) } }.ToJsonString(),
            WidgetPageState.Configure => EmptyJson,
            WidgetPageState.Content => ContentData,
            WidgetPageState.Loading => EmptyJson,
            _ => throw new NotImplementedException(Page.GetType().Name),
        };
    }

    private void SearchManagerResultsAvailableHandler(IEnumerable<Octokit.Issue> results, string resultType)
    {
        Log.Logger()?.ReportDebug(Name, ShortId, $"Results Available Event: Type={resultType}");
        if (resultType == Name)
        {
            Log.Logger()?.ReportInfo(Name, ShortId, $"Received matching repository update event.");
            LoadContentData(results);
            UpdateActivityState();
        }
    }
}
