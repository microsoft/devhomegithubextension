// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using GitHubPlugin.Client;
using GitHubPlugin.DataManager;
using GitHubPlugin.Helpers;
using Octokit;

namespace GitHubPlugin.Widgets;
internal class SystemMemoryWidget : SystemWidget
{
    private static Dictionary<string, string> Templates { get; set; } = new ();

    protected static readonly new string Name = nameof(SystemMemoryWidget);

    public SystemMemoryWidget()
        : base()
    {
        GitHubDataManager.OnUpdate += DataManagerUpdateHandler;
    }

    ~SystemMemoryWidget()
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
        if (WidgetState == string.Empty)
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
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Failed requesting data update.", ex);
        }
    }

    public override void LoadContentData()
    {
        if (WidgetState == string.Empty)
        {
            ContentData = EmptyJson;
            DataState = WidgetDataState.Okay;
            return;
        }

        Log.Logger()?.ReportDebug(Name, ShortId, "Getting Data for Pull Requests");

        try
        {
            var memoryData = new JsonObject();

            /*
            ulong availableMem = 0;
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                var allMem = memStatus.ullTotalPhys;
                availableMem = memStatus.ullAvailPhys;
                var usedMem = allMem - availableMem;

                // AddNextChartValue(systemData.memUsage * 100, ref systemData.memChartValues);
            }

            systemData.memCached = (ulong)memCached.NextValue();
            systemData.memCommited = (ulong)memCommitted.NextValue();
            systemData.memCommitLimit = (ulong)memCommittedLimit.NextValue();
            systemData.memPagedPool = (ulong)memPoolPaged.NextValue();
            systemData.memNonPagedPool = (ulong)memPoolNonPaged.NextValue();
            memoryData.Add("allMem", allMem);
            memoryData.Add("usedMem", usedMem);
            memoryData.Add("memUsage", (float)usedMem / allMem);
            */
            memoryData.Add("allMem", "13.25%");
            memoryData.Add("usedMem", "13.25%");
            memoryData.Add("memUsage", "13.25%");
            memoryData.Add("commitedMem", "13.25%");
            memoryData.Add("commitedLimitMem", "13.25%");
            memoryData.Add("cachedMem", "13.25%");
            memoryData.Add("pagedPoolMem", "13.25%");
            memoryData.Add("nonPagedPoolMem", "13.25%");
            memoryData.Add("memGraphUrl", "a");

            ContentData = memoryData.ToJsonString();
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
            WidgetPageState.Content => @"Widgets\Templates\SystemMemoryTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\GithubLoadingTemplate.json",
            _ => throw new NotImplementedException(),
        };
    }

    public override string GetData(WidgetPageState page)
    {
        if (string.IsNullOrEmpty(ContentData))
        {
            LoadContentData();
        }

        return page switch
        {
            WidgetPageState.Content => ContentData,
            WidgetPageState.Loading => EmptyJson,
            _ => throw new NotImplementedException(Page.GetType().Name),
        };
    }

    private void DataManagerUpdateHandler(object? source, DataManagerUpdateEventArgs e)
    {
        Log.Logger()?.ReportDebug(Name, ShortId, $"Data Update Event: Kind={e.Kind} Info={e.Description} Context={string.Join(",", e.Context)}");
        if (e.Kind == DataManagerUpdateKind.Repository && !string.IsNullOrEmpty(WidgetState))
        {
            var fullName = Validation.ParseFullNameFromGitHubURL(WidgetState);
            if (fullName == e.Description && e.Context.Contains("PullRequests"))
            {
                Log.Logger()?.ReportInfo(Name, ShortId, $"Received matching repository update event.");
                LoadContentData();
                UpdateActivityState();
            }
        }
    }
}
