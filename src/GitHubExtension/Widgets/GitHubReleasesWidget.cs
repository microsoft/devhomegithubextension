// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using GitHubExtension.Client;
using GitHubExtension.DataManager;
using GitHubExtension.Helpers;
using GitHubExtension.Widgets.Enums;
using Octokit;

namespace GitHubExtension.Widgets;

internal class GitHubReleasesWidget : GitHubRepositoryWidget
{
    private readonly string _releasesIconData = IconLoader.GetIconAsBase64("releases.png");

    protected static readonly new string Name = nameof(GitHubReleasesWidget);

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

            var dataManager = GitHubDataManager.CreateInstance();
            _ = dataManager?.UpdateReleasesForRepositoryAsync(GetOwner(), GetRepo(), requestOptions);
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

        Log.Logger()?.ReportDebug(Name, ShortId, "Getting Data for Releases");

        try
        {
            using var dataManager = GitHubDataManager.CreateInstance();
            var repository = dataManager!.GetRepository(GetOwner(), GetRepo());

            DataModel.Release[] releases;
            if (repository is null)
            {
                releases = Enumerable.Empty<DataModel.Release>().ToArray();
            }
            else
            {
                releases = repository.Releases.ToArray();
            }

            var releasesData = new JsonObject();
            var releasesArray = new JsonArray();
            var latestFound = false;

            foreach (var releaseItem in releases)
            {
                var infoText = string.Empty;
                var infoColor = string.Empty;

                if (releaseItem.Prerelease == 1)
                {
                    infoText = Resources.GetResource("Widget_Releases_Prerelease");
                    infoColor = "warning";
                }
                else if (!latestFound)
                {
                    infoText = Resources.GetResource(@"Widget_Releases_Latest");
                    infoColor = "good";
                    latestFound = true;
                }

                var release = new JsonObject
                {
                    { "name", releaseItem.Name },
                    { "tag", releaseItem.TagName },
                    { "infoText", infoText },
                    { "infoColor", infoColor },
                    { "published", TimeSpanHelper.DateTimeOffsetToDisplayString(releaseItem.PublishedAt, Log.Logger()) },
                    { "url", releaseItem.HtmlUrl },
                    { "icon", _releasesIconData },
                };

                ((IList<JsonNode?>)releasesArray).Add(release);
            }

            releasesData.Add("releases", releasesArray);
            releasesData.Add("selected_repo", repository?.FullName ?? string.Empty);
            releasesData.Add("is_loading_data", DataState == WidgetDataState.Unknown);
            releasesData.Add("releases_icon_data", _releasesIconData);

            LastUpdated = DateTime.Now;
            DataState = WidgetDataState.Okay;
            ContentData = releasesData.ToJsonString();
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
            WidgetPageState.Configure => @"Widgets\Templates\GitHubReleasesConfigurationTemplate.json",
            WidgetPageState.Content => @"Widgets\Templates\GitHubReleasesTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\GitHubLoadingTemplate.json",
            _ => throw new NotImplementedException(),
        };
    }

    protected override void DataManagerUpdateHandler(object? source, DataManagerUpdateEventArgs e)
    {
        Log.Logger()?.ReportDebug(Name, ShortId, $"Data Update Event: Kind={e.Kind} Info={e.Description} Context={string.Join(",", e.Context)}");

        // Don't update if we're in configuration mode.
        if (ActivityState == WidgetActivityState.Configure)
        {
            return;
        }

        if (e.Kind == DataManagerUpdateKind.Repository && !string.IsNullOrEmpty(RepositoryUrl))
        {
            var fullName = Validation.ParseFullNameFromGitHubURL(RepositoryUrl);
            if (fullName == e.Description && e.Context.Contains("Releases"))
            {
                Log.Logger()?.ReportInfo(Name, ShortId, $"Received matching repository update event.");
                LoadContentData();
                UpdateActivityState();
            }
        }
    }
}
