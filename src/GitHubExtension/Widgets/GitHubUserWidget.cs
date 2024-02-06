// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using GitHubExtension.DataManager;
using GitHubExtension.DeveloperId;
using GitHubExtension.Helpers;
using GitHubExtension.Widgets.Enums;
using Microsoft.Windows.DevHome.SDK;
using Microsoft.Windows.Widgets.Providers;
using Octokit;

namespace GitHubExtension.Widgets;

internal abstract class GitHubUserWidget : GitHubWidget
{
    protected static readonly new string Name = nameof(GitHubUserWidget);

    protected string DeveloperLoginId { get; set; } = string.Empty;

    protected SearchCategory ShowCategory { get; set; } = SearchCategory.Unknown;

    private string userName = string.Empty;

    protected string UserName
    {
        get => userName = DeveloperLoginId;
        set => userName = value;
    }

    public GitHubUserWidget()
        : base()
    {
        GitHubSearchManager.OnResultsAvailable += SearchManagerResultsAvailableHandler;
        ShowCategory = SearchCategory.Unknown;
        UserName = string.Empty;
    }

    ~GitHubUserWidget()
    {
        GitHubSearchManager.OnResultsAvailable -= SearchManagerResultsAvailableHandler;
    }

    protected abstract string GetTitleIconData();

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

    protected override void ResetWidgetInfoFromState()
    {
        JsonNode? dataObject = null;

        try
        {
            dataObject = JsonNode.Parse(ConfigurationData);
        }
        catch (JsonException e)
        {
            Log.Logger()?.ReportWarn(Name, ShortId, $"Failed to parse ConfigurationData; attempting migration. {e.Message}");
            Log.Logger()?.ReportDebug(Name, ShortId, $"Json parse failure.", e);

            try
            {
                // Old data versioning was not a Json string. If we attempt to parse
                // and we get a failure, assume it is the old version.
                if (!string.IsNullOrEmpty(ConfigurationData))
                {
                    Log.Logger()?.ReportInfo(Name, ShortId, $"Found string data format, migrating to JSON format. Data: {ConfigurationData}");
                    var migratedState = new JsonObject
                    {
                        { "showCategory", ConfigurationData },
                    };

                    // Prior to this configuration change, multi-account was not supported. Assume that
                    // if there is exactly one Developer Id that is the one the user had configured.
                    // There is no else case here because if we leave this value absent, the widget will
                    // change state to configuring, where the user can select the developer ID they want.
                    if (DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIds().DeveloperIds.Count() == 1)
                    {
                        migratedState.Add("account", DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIds().DeveloperIds.First().LoginId);
                    }

                    ConfigurationData = migratedState.ToJsonString();
                }
                else
                {
                    ConfigurationData = EmptyJson;
                }
            }
            catch (Exception ex)
            {
                // Adding for abundance of caution because we have seen crashes in this space.
                Log.Logger()?.ReportError(Name, ShortId, $"Unexpected failure during migration.", ex);
            }
        }

        try
        {
            dataObject ??= JsonNode.Parse(ConfigurationData);
            ShowCategory = EnumHelper.StringToSearchCategory(dataObject!["showCategory"]?.GetValue<string>() ?? string.Empty);
            DeveloperLoginId = dataObject!["account"]?.GetValue<string>() ?? string.Empty;
        }
        catch (Exception e)
        {
            // If we fail to parse configuration data, do nothing, report the failure, and don't
            // crash the entire extension.
            DeveloperLoginId = string.Empty;
            ShowCategory = SearchCategory.Unknown;
            Log.Logger()?.ReportError(Name, ShortId, $"Unexpected error while resetting state: {e.Message}", e);
        }
    }

    public override void OnCustomizationRequested(WidgetCustomizationRequestedArgs customizationRequestedArgs)
    {
        ShowCategory = SearchCategory.Unknown;
        base.OnCustomizationRequested(customizationRequestedArgs);
    }

    public override void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        if (actionInvokedArgs.Verb == "Submit")
        {
            var data = actionInvokedArgs.Data;
            var dataObject = JsonNode.Parse(data);

            if (dataObject == null)
            {
                return;
            }

            ShowCategory = EnumHelper.StringToSearchCategory(dataObject["showCategory"]?.GetValue<string>() ?? string.Empty);
            DeveloperLoginId = dataObject["account"]?.GetValue<string>() ?? string.Empty;

            ConfigurationData = data;

            // If we got here during the customization flow, we need to LoadContentData again
            // so we can show the loading page rather than stale data.
            LoadContentData();
            UpdateActivityState();
        }
        else
        {
            base.OnActionInvoked(actionInvokedArgs);
        }
    }

    public override void UpdateActivityState()
    {
        // State logic for the Widget:
        // Signed in -> Configure -> Active / Inactive per widget host.
        if (!IsUserLoggedIn())
        {
            SetSignIn();
            return;
        }

        if (ShowCategory == SearchCategory.Unknown || string.IsNullOrEmpty(DeveloperLoginId))
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

    protected void RequestContentData(SearchIssuesRequest request)
    {
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
            Log.Logger()?.ReportInfo(Name, ShortId, $"Requesting data update for {UserName}");
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

            var searchManager = GitHubSearchManager.CreateInstance();
            var developerId = GetWidgetDeveloperId();

            if (developerId == null)
            {
                throw new InvalidOperationException($"DevID does not exist for login id: {DeveloperLoginId}");
            }

            searchManager?.SearchForGitHubIssuesOrPRs(request, Id, ShowCategory, developerId, requestOptions);
            Log.Logger()?.ReportInfo(Name, ShortId, $"Requested data update for {UserName}");
            DataState = WidgetDataState.Requested;
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Failed requesting data update.", ex);
        }
    }

    public override void LoadContentData()
    {
        var issuesData = new JsonObject
        {
            { "openCount", 0 },
            { "items", new JsonArray() },
            { "userName", UserName },
            { "titleIconUrl", GetTitleIconData() },
            { "is_loading_data", true },
        };
        ContentData = issuesData.ToJsonString();
    }

    public void LoadContentData(IEnumerable<Octokit.Issue> items)
    {
        Log.Logger()?.ReportDebug(Name, ShortId, "Getting Data for Category in Widget");

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
            issuesData.Add("userName", UserName);
            issuesData.Add("titleIconUrl", GetTitleIconData());

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

    protected IDeveloperId? GetWidgetDeveloperId()
    {
        foreach (var devid in DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIds().DeveloperIds)
        {
            if (devid.LoginId == DeveloperLoginId)
            {
                return devid;
            }
        }

        return null;
    }

    protected void AddDevIds(ref JsonObject configurationData)
    {
        var developerIdsData = new JsonArray();

        foreach (var developerId in DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIds().DeveloperIds)
        {
            Log.Logger()?.ReportInfo(developerId.LoginId);
            developerIdsData.Add(new JsonObject
            {
                { "devId", developerId.LoginId },
            });
        }

        configurationData.Add("accounts", developerIdsData);
    }

    public string GetConfigurationData()
    {
        var configurationData = new JsonObject
        {
            { "showCategory", EnumHelper.SearchCategoryToString(ShowCategory == SearchCategory.Unknown ? SearchCategory.IssuesAndPullRequests : ShowCategory) },
            { "savedShowCategory", SavedConfigurationData },
            { "configuring", true },
        };

        if (!string.IsNullOrEmpty(DeveloperLoginId))
        {
            configurationData.Add("selectedDevId", DeveloperLoginId);
        }
        else if (DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIds().DeveloperIds.Count() == 1)
        {
            configurationData.Add("selectedDevId", DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIds().DeveloperIds.First().LoginId);
        }

        AddDevIds(ref configurationData);

        return configurationData.ToJsonString();
    }

    private void SearchManagerResultsAvailableHandler(IEnumerable<Octokit.Issue> results, string widgetId)
    {
        Log.Logger()?.ReportDebug(Name, ShortId, $"Results Available Event: ID={widgetId}");
        if (widgetId == Id)
        {
            Log.Logger()?.ReportInfo(Name, ShortId, $"Received matching repository update event.");
            LoadContentData(results);
            UpdateActivityState();
        }
    }
}
