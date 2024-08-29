// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using GitHubExtension.Client;
using GitHubExtension.DataManager;
using GitHubExtension.Widgets.Enums;
using Microsoft.Windows.Widgets.Providers;

namespace GitHubExtension.Widgets;

public abstract class GitHubRepositoryWidget : GitHubWidget
{
    protected string RepositoryUrl { get; set; } = string.Empty;

    private string? _message;

    public GitHubRepositoryWidget()
        : base()
    {
        GitHubDataManager.OnUpdate += DataManagerUpdateHandler;
    }

    ~GitHubRepositoryWidget()
    {
        GitHubDataManager.OnUpdate -= DataManagerUpdateHandler;
    }

    public string GetOwner()
    {
        return Validation.ParseOwnerFromGitHubURL(RepositoryUrl);
    }

    public string GetRepo()
    {
        return Validation.ParseRepositoryFromGitHubURL(RepositoryUrl);
    }

    public string GetIssueQuery()
    {
        return Validation.ParseIssueQueryFromGitHubURL(RepositoryUrl);
    }

    public string GetUnescapedIssueQuery()
    {
        return Uri.UnescapeDataString(GetIssueQuery()).Replace('+', ' ');
    }

    // If the user changed the URL after clicking submit and clicked
    // saved just after, we change it back to what was before to not corrupt our saved data.
    private void CorrectUrl()
    {
        var configurationData = JsonNode.Parse(ConfigurationData);
        if (configurationData != null)
        {
            configurationData["url"] = RepositoryUrl;
            ConfigurationData = configurationData.ToJsonString();
            UpdateWidget();
        }
    }

    public override void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        var verb = GetWidgetActionForVerb(actionInvokedArgs.Verb);
        Log.Debug($"ActionInvoked: {verb}");

        switch (verb)
        {
            case WidgetAction.Save:
                if (HandleCheckUrl(actionInvokedArgs))
                {
                    UpdateTitle(JsonNode.Parse(actionInvokedArgs.Data));
                    base.OnActionInvoked(actionInvokedArgs);
                    CorrectUrl();
                }

                break;

            default:
                base.OnActionInvoked(actionInvokedArgs);
                break;
        }
    }

    protected Octokit.Repository GetRepositoryFromUrl(string url)
    {
        var ownerName = Validation.ParseOwnerFromGitHubURL(url);
        var repositoryName = Validation.ParseRepositoryFromGitHubURL(url);
        var devIds = DeveloperId.DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIdsInternal();
        var found = false;
        Octokit.Repository? repository = null;

        // We only need to get the information from one account which has access.
        foreach (var devId in devIds)
        {
            try
            {
                repository = devId.GitHubClient.Repository.Get(ownerName, repositoryName).Result;
                found = true;
                break;
            }
            catch (Exception ex) when (ex.InnerException is not null && ex.InnerException is Octokit.ApiException)
            {
                switch (ex.InnerException)
                {
                    case Octokit.NotFoundException:
                        // A private repository will come back as "not found" by the GitHub API when an unauthorized account cannot even view it.
                        Log.Debug($"DeveloperId {devId.LoginId} did not find {ownerName}/{repositoryName}");
                        continue;

                    case Octokit.RateLimitExceededException:
                        Log.Debug($"DeveloperId {devId.LoginId} rate limit exceeded.");
                        throw ex.InnerException;

                    case Octokit.ForbiddenException:
                        // This can happen most commonly with SAML-enabled organizations.
                        // The user may have access but the org blocked the application.
                        Log.Debug($"DeveloperId {devId.LoginId} was forbidden access to {ownerName}/{repositoryName}");
                        throw ex.InnerException;

                    default:
                        // If it's some other error like abuse detection, abort and do not continue.
                        Log.Debug($"Unhandled Octokit API error for {devId.LoginId} and {ownerName}/{repositoryName}");
                        throw ex.InnerException;
                }
            }
        }

        if (!found || repository is null)
        {
            throw new RepositoryNotFoundException($"The repository {ownerName}/{repositoryName} could not be accessed by any available developer accounts.");
        }

        return repository;
    }

    private void UpdateTitle(JsonNode? dataObj)
    {
        if (dataObj == null)
        {
            return;
        }

        GetTitleFromDataObject(dataObj);
    }

    protected string GetActualTitle()
    {
        return string.IsNullOrEmpty(WidgetTitle) ? GetRepositoryFromUrl(RepositoryUrl).FullName : WidgetTitle;
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
                // and we get a failure, check if it is the old version.
                if (!string.IsNullOrEmpty(ConfigurationData))
                {
                    Log.Information($"Found string data format, migrating to JSON format. Data: {ConfigurationData}");
                    var migratedState = new JsonObject
                    {
                        { "url", ConfigurationData },
                    };
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
            RepositoryUrl = dataObject!["url"]?.GetValue<string>() ?? string.Empty;
            UpdateTitle(dataObject);
        }
        catch (Exception e)
        {
            // If we fail to parse configuration data, do nothing, report the failure, and don't
            // crash the entire extension.
            RepositoryUrl = string.Empty;
            Log.Error(e, $"Unexpected error while resetting state: {e.Message}");
        }
    }

    public override void OnCustomizationRequested(WidgetCustomizationRequestedArgs customizationRequestedArgs)
    {
        Saved = false;
        base.OnCustomizationRequested(customizationRequestedArgs);
    }

    private bool HandleCheckUrl(WidgetActionInvokedArgs args)
    {
        // Set loading page while we fetch data from GitHub.
        Page = WidgetPageState.Loading;
        UpdateWidget();

        // This is the action when the user clicks the submit button after entering a URL while in
        // the Configure state.
        Page = WidgetPageState.Configure;
        var data = args.Data;
        var dataObject = JsonObject.Parse(data);
        if (dataObject != null && dataObject["url"] != null)
        {
            RepositoryUrl = dataObject["url"]?.GetValue<string>() ?? string.Empty;
            UpdateTitle(dataObject);

            var isGoodToSave = true;

            try
            {
                GetRepositoryFromUrl(RepositoryUrl);
                ConfigurationData = data;
                _message = null;
            }
            catch (Exception ex)
            {
                _message = ex.Message;
                isGoodToSave = false;
            }

            var updateRequestOptions = new WidgetUpdateRequestOptions(Id)
            {
                Data = GetConfiguration(RepositoryUrl),
                CustomState = ConfigurationData,
                Template = GetTemplateForPage(Page),
            };

            WidgetManager.GetDefault().UpdateWidget(updateRequestOptions);

            // Already shown error message while updating above,
            // can reset it to null here.
            _message = null;
            return isGoodToSave;
        }

        UpdateWidget();

        return false;
    }

    public string GetConfiguration(string dataUrl)
    {
        var configurationData = new JsonObject
        {
            { "widgetTitle", WidgetTitle },
            { "url", dataUrl },
            { "savedRepositoryUrl", SavedConfigurationData },
            { "errorMessage", _message },
        };

        return configurationData.ToJsonString();
    }

    public override string GetData(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.SignIn => GetSignIn(),
            WidgetPageState.Configure => GetConfiguration(RepositoryUrl),
            WidgetPageState.Content => ContentData,
            WidgetPageState.Loading => EmptyJson,
            _ => throw new NotImplementedException(Page.GetType().Name),
        };
    }

    protected new string GetCurrentState()
    {
        return $"State: {ActivityState}  Page: {Page}  Data: {DataState}  Repository: {RepositoryUrl}";
    }

    protected abstract void DataManagerUpdateHandler(object? source, DataManagerUpdateEventArgs e);
}
