﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using GitHubExtension.Client;
using GitHubExtension.DataManager;
using GitHubExtension.Helpers;
using GitHubExtension.Widgets.Enums;
using Octokit;

namespace GitHubExtension.Widgets;

internal sealed class GitHubPullsWidget : GitHubRepositoryWidget
{
    private readonly string _pullsIconData = IconLoader.GetIconAsBase64("pulls.png");

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
            Log.Debug("Data request too soon, skipping.");
        }

        if (ActivityState == WidgetActivityState.Configure)
        {
            return;
        }

        try
        {
            Log.Debug($"Requesting data update for {GetOwner()}/{GetRepo()}");
            var requestOptions = new RequestOptions
            {
                PullRequestRequest = new PullRequestRequest
                {
                    State = ItemStateFilter.Open,
                    SortProperty = PullRequestSort.Updated,
                    SortDirection = SortDirection.Descending,
                },
                ApiOptions = new ApiOptions
                {
                    PageSize = 10,
                    PageCount = 1,
                    StartPage = 1,
                },
                UsePublicClientAsFallback = true,
            };

            var dataManager = GitHubDataManager.CreateInstance();
            _ = dataManager?.UpdatePullRequestsForRepositoryAsync(GetOwner(), GetRepo(), requestOptions);
            Log.Information($"Requested data update for {GetOwner()}/{GetRepo()}");
            DataState = WidgetDataState.Requested;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed requesting data update.");
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

        Log.Debug("Getting Data for Pull Requests");

        try
        {
            using var dataManager = GitHubDataManager.CreateInstance();
            var repository = dataManager!.GetRepository(GetOwner(), GetRepo());
            var pulls = repository is null ? Enumerable.Empty<DataModel.PullRequest>() : repository.PullRequests;

            var pullsData = new JsonObject();
            var pullsArray = new JsonArray();
            foreach (var pullItem in pulls)
            {
                var pull = new JsonObject
                {
                    { "title", pullItem.Title },
                    { "url", pullItem.HtmlUrl },
                    { "number", pullItem.Number },
                    { "date", TimeSpanHelper.DateTimeOffsetToDisplayString(pullItem.UpdatedAt, Log) },
                    { "user", pullItem.Author.Login },
                    { "avatar", pullItem.Author.AvatarUrl },
                    { "icon", _pullsIconData },
                };

                var labels = pullItem.Labels.ToList();
                var pullLabels = new JsonArray();
                foreach (var label in labels)
                {
                    var pullLabel = new JsonObject
                    {
                        { "name", label.Name },
                        { "color", label.Color },
                    };

                    ((IList<JsonNode?>)pullLabels).Add(pullLabel);
                }

                pull.Add("labels", pullLabels);

                ((IList<JsonNode?>)pullsArray).Add(pull);
            }

            pullsData.Add("pulls", pullsArray);
            pullsData.Add("selected_repo", repository?.FullName ?? string.Empty);
            pullsData.Add("widgetTitle", GetActualTitle());
            pullsData.Add("is_loading_data", DataState == WidgetDataState.Unknown);
            pullsData.Add("pulls_icon_data", _pullsIconData);

            LastUpdated = DateTime.Now;
            DataState = WidgetDataState.Okay;
            ContentData = pullsData.ToJsonString();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error retrieving data.");
            DataState = WidgetDataState.Failed;
            return;
        }
    }

    public override string GetTemplatePath(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.SignIn => @"Widgets\Templates\GitHubSignInTemplate.json",
            WidgetPageState.Configure => @"Widgets\Templates\GitHubPullsConfigurationTemplate.json",
            WidgetPageState.Content => @"Widgets\Templates\GitHubPullsTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\GitHubLoadingTemplate.json",
            _ => throw new NotImplementedException(),
        };
    }

    protected override void DataManagerUpdateHandler(object? source, DataManagerUpdateEventArgs e)
    {
        Log.Debug($"Data Update Event: Kind={e.Kind} Info={e.Description} Context={string.Join(",", e.Context)}");

        // Don't update if we're in configuration mode.
        if (ActivityState == WidgetActivityState.Configure)
        {
            return;
        }

        if (e.Kind == DataManagerUpdateKind.Repository && !string.IsNullOrEmpty(RepositoryUrl))
        {
            var fullName = Validation.ParseFullNameFromGitHubURL(RepositoryUrl);
            if (fullName == e.Description && e.Context.Contains("PullRequests"))
            {
                Log.Information($"Received matching repository update event.");
                LoadContentData();
                UpdateActivityState();
            }
        }
    }
}
