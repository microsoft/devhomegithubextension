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
    protected string DeveloperLoginId { get; set; } = string.Empty;

    protected SearchCategory ShowCategory { get; set; } = SearchCategory.Unknown;

    protected virtual string DefaultShowCategory => string.Empty;

    private string _userName = string.Empty;

    protected string UserName
    {
        get => _userName = DeveloperLoginId;
        set => _userName = value;
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

    protected void UpdateTitle(JsonNode dataObj)
    {
        if (dataObj == null)
        {
            return;
        }

        GetTitleFromDataObject(dataObj);
    }

    protected string GetActualTitle()
    {
        return string.IsNullOrEmpty(WidgetTitle) ? UserName : WidgetTitle;
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
            Log.Warning($"Failed to parse ConfigurationData; attempting migration. {e.Message}");
            Log.Debug($"Json parse failure.", e);

            try
            {
                // Old data versioning was not a Json string. If we attempt to parse
                // and we get a failure, assume it is the old version.
                if (!string.IsNullOrEmpty(ConfigurationData))
                {
                    Log.Information($"Found string data format, migrating to JSON format. Data: {ConfigurationData}");
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
                Log.Error(ex, $"Unexpected failure during migration.");
            }
        }

        try
        {
            dataObject ??= JsonNode.Parse(ConfigurationData);
            ShowCategory = EnumHelper.StringToSearchCategory(dataObject!["showCategory"]?.GetValue<string>() ?? DefaultShowCategory);
            DeveloperLoginId = dataObject!["account"]?.GetValue<string>() ?? string.Empty;
            UpdateTitle(dataObject);
        }
        catch (Exception e)
        {
            // If we fail to parse configuration data, do nothing, report the failure, and don't
            // crash the entire extension.
            DeveloperLoginId = string.Empty;
            ShowCategory = SearchCategory.Unknown;
            Log.Error(e, $"Unexpected error while resetting state: {e.Message}");
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

            ShowCategory = EnumHelper.StringToSearchCategory(dataObject["showCategory"]?.GetValue<string>() ?? DefaultShowCategory);
            DeveloperLoginId = dataObject["account"]?.GetValue<string>() ?? string.Empty;
            UpdateTitle(dataObject);

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
            Log.Debug("Data request too soon, skipping.");
        }

        if (ActivityState == WidgetActivityState.Configure)
        {
            return;
        }

        try
        {
            Log.Information($"Requesting data update for {UserName}");
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
            Log.Information($"Requested data update for {UserName}");
            DataState = WidgetDataState.Requested;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed requesting data update.");
        }
    }

    public override void LoadContentData()
    {
        var issuesData = new JsonObject
        {
            { "openCount", 0 },
            { "items", new JsonArray() },
            { "userName", UserName },
            { "widgetTitle", GetActualTitle() },
            { "titleIconUrl", GetTitleIconData() },
            { "is_loading_data", true },
        };
        ContentData = issuesData.ToJsonString();
    }

    public void LoadContentData(IEnumerable<Octokit.Issue> items)
    {
        Log.Debug("Getting Data for Category in Widget");

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
                    { "date", TimeSpanHelper.DateTimeOffsetToDisplayString(item.UpdatedAt, Log) },
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
            issuesData.Add("widgetTitle", GetActualTitle());

            LastUpdated = DateTime.Now;
            ContentData = issuesData.ToJsonString();
            DataState = WidgetDataState.Okay;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error retrieving data.");
            DataState = WidgetDataState.Failed;
            return;
        }
    }

    public override string GetData(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.SignIn => new JsonObject { { "message", Resources.GetResource(@"Widget_Template/SignInRequired", Log) } }.ToJsonString(),
            WidgetPageState.Configure => GetConfigurationData(),
            WidgetPageState.Content => ContentData,
            WidgetPageState.Loading => EmptyJson,
            _ => throw new NotImplementedException(Page.GetType().Name),
        };
    }

    protected IDeveloperId? GetWidgetDeveloperId()
    {
        foreach (var devId in DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIds().DeveloperIds)
        {
            if (devId.LoginId == DeveloperLoginId)
            {
                return devId;
            }
        }

        return null;
    }

    protected void AddDevIds(ref JsonObject configurationData)
    {
        var developerIdsData = new JsonArray();

        foreach (var developerId in DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIds().DeveloperIds)
        {
            Log.Information(developerId.LoginId);
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
            { "widgetTitle", WidgetTitle },
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
        Log.Debug($"Results Available Event: ID={widgetId}");
        if (widgetId == Id)
        {
            Log.Information($"Received matching repository update event.");
            LoadContentData(results);
            UpdateActivityState();
        }
    }
}
