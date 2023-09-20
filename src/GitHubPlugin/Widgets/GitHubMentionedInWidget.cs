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
internal class GitHubMentionedInWidget : GitHubWidget
{
    private static readonly string TitleIconData =
        "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";

    private static Dictionary<string, string> Templates { get; set; } = new ();

    protected static readonly new string Name = nameof(GitHubMentionedInWidget);

    private SearchCategory ShowCategory
    {
        get => EnumHelper.StringToSearchCategory(State());

        set => SetState(EnumHelper.SearchCategoryToString(value));
    }

    private string mentionedName = string.Empty;

    private string MentionedName
    {
        get
        {
            if (string.IsNullOrEmpty(mentionedName))
            {
                GetMentionedName();
            }

            return mentionedName;
        }
        set => mentionedName = value;
    }

    public GitHubMentionedInWidget()
        : base()
    {
        GitHubSearchManager.OnResultsAvailable += SearchManagerResultsAvailableHandler;
        ShowCategory = SearchCategory.IssuesAndPullRequests;
    }

    ~GitHubMentionedInWidget()
    {
        GitHubSearchManager.OnResultsAvailable -= SearchManagerResultsAvailableHandler;
    }

    private void GetMentionedName()
    {
        var devIds = DeveloperId.DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIdsInternal();
        if ((devIds != null) && devIds.Any())
        {
            mentionedName = devIds.First().LoginId;
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
        // State logic for the Widget:
        // Signed in -> Configure -> Active / Inactive per widget host.
        if (!IsUserLoggedIn())
        {
            SetSignIn();
            return;
        }

        if (ShowCategory == SearchCategory.Unknown)
        {
            SetConfigure();
            return;
        }

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

    public override void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        if (actionInvokedArgs.Verb == "Submit")
        {
            var dataObject = JsonSerializer.Deserialize(actionInvokedArgs.Data, SourceGenerationContextMentionedWidget.Default.DataPayloadMentionedWidget);
            if (dataObject != null && dataObject.ShowCategory != null)
            {
                ShowCategory = EnumHelper.StringToSearchCategory(dataObject.ShowCategory);
                UpdateActivityState();
            }
        }
        else
        {
            base.OnActionInvoked(actionInvokedArgs);
        }
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
            Log.Logger()?.ReportInfo(Name, ShortId, $"Requesting search for mentioned user {mentionedName}");
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

            var request = new SearchIssuesRequest()
            {
                Mentions = MentionedName,
            };

            var searchManager = GitHubSearchManager.CreateInstance();
            searchManager?.SearchForGitHubIssuesOrPRs(request, Name, ShowCategory, requestOptions);
            Log.Logger()?.ReportInfo(Name, ShortId, $"Requested search for {mentionedName}");
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
            { "mentionedName", MentionedName },
            { "titleIconUrl", TitleIconData },
            { "is_loading_data", true },
        };
        ContentData = issuesData.ToJsonString();
    }

    public void LoadContentData(IEnumerable<Octokit.Issue> items)
    {
        Log.Logger()?.ReportDebug(Name, ShortId, "Getting Data for Mentioned in Widget");

        try
        {
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
                    { "iconUrl", IconLoader.GetIconAsBase64(item.PullRequest == null ? "issues.png" : "pulls.png") },
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
            issuesData.Add("mentionedName", MentionedName);
            issuesData.Add("titleIconUrl", TitleIconData);

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
            WidgetPageState.Configure => @"Widgets\Templates\GitHubMentionedInConfigurationTemplate.json",
            WidgetPageState.Content => @"Widgets\Templates\GitHubMentionedInTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\GitHubLoadingTemplate.json",
            _ => throw new NotImplementedException(),
        };
    }

    public override string GetData(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.SignIn => new JsonObject { { "message", Resources.GetResource(@"Widget_Template/SignInRequired", Log.Logger()) } }.ToJsonString(),
            WidgetPageState.Configure => GetConfigurationData(),
            WidgetPageState.Content => ContentData,
            WidgetPageState.Loading => EmptyJson,
            _ => throw new NotImplementedException(Page.GetType().Name),
        };
    }

    public string GetConfigurationData()
    {
        var configurationData = new JsonObject
        {
            { "showCategory", EnumHelper.SearchCategoryToString(ShowCategory == SearchCategory.Unknown ? SearchCategory.IssuesAndPullRequests : ShowCategory) },
            { "configuring", true },
        };
        return configurationData.ToJsonString();
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

internal class DataPayloadMentionedWidget
{
    public string? ShowCategory
    {
        get; set;
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DataPayloadMentionedWidget))]
internal partial class SourceGenerationContextMentionedWidget : JsonSerializerContext
{
}
