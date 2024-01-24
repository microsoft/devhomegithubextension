// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GitHubExtension.Client;
using GitHubExtension.DataManager;
using GitHubExtension.DeveloperId;
using GitHubExtension.Helpers;
using GitHubExtension.Widgets.Enums;
using Microsoft.Windows.DevHome.SDK;
using Microsoft.Windows.Widgets.Providers;

namespace GitHubExtension.Widgets;

public abstract class GitHubRepositoryWidget : GitHubWidget
{
    protected static readonly new string Name = nameof(GitHubRepositoryWidget);

    protected string RepositoryUrl { get; set; } = string.Empty;

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

    public override void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        var verb = GetWidgetActionForVerb(actionInvokedArgs.Verb);
        Log.Logger()?.ReportDebug(Name, ShortId, $"ActionInvoked: {verb}");

        switch (verb)
        {
            case WidgetAction.CheckUrl:
                HandleCheckUrl(actionInvokedArgs);
                break;

            default:
                base.OnActionInvoked(actionInvokedArgs);
                break;
        }
    }

    protected override void ResetWidgetInfoFromState()
    {
        JsonNode? dataObject = null;
        try
        {
            dataObject = JsonNode.Parse(ConfigurationData);
        }
        catch (JsonException)
        {
            // Old data versioning was not a Json string. If we attempt to parse
            // and we get a failure, check if it is the old version.
            if (Validation.IsValidGitHubURL(ConfigurationData))
            {
                Log.Logger()?.ReportInfo(Name, ShortId, $"Found string data format, migrating to JSON format. Data: {ConfigurationData}");
                var migratedState = new JsonObject
                {
                    { "url", ConfigurationData },
                };
                ConfigurationData = migratedState.ToJsonString();
            }
            else
            {
                Log.Logger()?.ReportWarn(Name, ShortId, $"Unrecognized configuration data, setting to default. Data: {ConfigurationData}");
                ConfigurationData = EmptyJson;
            }
        }

        try
        {
            dataObject ??= JsonNode.Parse(ConfigurationData);
            RepositoryUrl = dataObject!["url"]?.GetValue<string>() ?? string.Empty;
        }
        catch (Exception e)
        {
            // If we fail to parse configuration data, do nothing, report the failure, and don't
            // crash the entire extension.
            RepositoryUrl = string.Empty;
            Log.Logger()?.ReportError(Name, ShortId, $"Unexpected error while resetting state: {e.Message}", e);
        }
    }

    public override void OnCustomizationRequested(WidgetCustomizationRequestedArgs customizationRequestedArgs)
    {
        Saved = false;
        base.OnCustomizationRequested(customizationRequestedArgs);
    }

    private void HandleCheckUrl(WidgetActionInvokedArgs args)
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

            ConfigurationData = data;

            var updateRequestOptions = new WidgetUpdateRequestOptions(Id)
            {
                Data = GetConfiguration(RepositoryUrl),
                CustomState = ConfigurationData,
                Template = GetTemplateForPage(Page),
            };

            WidgetManager.GetDefault().UpdateWidget(updateRequestOptions);
        }
    }

    public string GetConfiguration(string dataUrl)
    {
        var configurationData = new JsonObject
        {
            { "submitIcon", IconLoader.GetIconAsBase64("arrow.png") },
        };

        if (dataUrl == string.Empty)
        {
            configurationData.Add("hasConfiguration", false);
            var repositoryData = new JsonObject
            {
                { "url", string.Empty },
            };

            configurationData.Add("configuration", repositoryData);
            configurationData.Add("savedRepositoryUrl", SavedConfigurationData);
            configurationData.Add("saveEnabled", false);

            return configurationData.ToString();
        }
        else
        {
            try
            {
                // Get client for logged in user.
                var client = GitHubClientProvider.Instance.GetClientForLoggedInDeveloper(true).Result ?? throw new InvalidOperationException("Failed getting GitHubClient.");

                // Get repository for the URL, which is "data" in this case.
                var ownerName = Validation.ParseOwnerFromGitHubURL(dataUrl);
                var repositoryName = Validation.ParseRepositoryFromGitHubURL(dataUrl);
                var repository = client.Repository.Get(ownerName, repositoryName).Result;

                var repositoryData = new JsonObject
                {
                    { "name", repository.FullName },
                    { "label", repository.Name },
                    { "owner", repository.Owner.Login },
                    { "milestone", string.Empty },
                    { "project", repository.Description },
                    { "url", repository.HtmlUrl },
                    { "query", GetUnescapedIssueQuery() },
                };

                configurationData.Add("hasConfiguration", true);
                configurationData.Add("configuration", repositoryData);
                configurationData.Add("savedRepositoryUrl", SavedConfigurationData);
                configurationData.Add("saveEnabled", true);
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError(Name, ShortId, $"Failed getting configuration information for input url: {dataUrl}", ex);
                configurationData.Add("hasConfiguration", false);

                var repositoryData = new JsonObject
                {
                    { "url", RepositoryUrl },
                };

                configurationData.Add("errorMessage", ex.Message);
                configurationData.Add("configuration", repositoryData);
                configurationData.Add("saveEnabled", false);

                return configurationData.ToString();
            }

            return configurationData.ToJsonString();
        }
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
