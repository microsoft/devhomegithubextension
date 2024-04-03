// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Windows.Widgets.Providers;
using Serilog;

namespace GitHubExtension.Widgets;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[Guid("F23870B0-B391-4466-84E2-42A991078613")]
public sealed class WidgetProvider : IWidgetProvider, IWidgetProvider2
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetProvider));

    public WidgetProvider()
    {
        _log.Verbose("Provider Constructed");
        widgetDefinitionRegistry.Add("GitHub_Issues", new WidgetImplFactory<GitHubIssuesWidget>());
        widgetDefinitionRegistry.Add("GitHub_PullRequests", new WidgetImplFactory<GitHubPullsWidget>());
        widgetDefinitionRegistry.Add("GitHub_MentionedIns", new WidgetImplFactory<GitHubMentionedInWidget>());
        widgetDefinitionRegistry.Add("GitHub_Assigneds", new WidgetImplFactory<GitHubAssignedWidget>());
        widgetDefinitionRegistry.Add("GitHub_Reviews", new WidgetImplFactory<GitHubReviewWidget>());
        widgetDefinitionRegistry.Add("GitHub_Releases", new WidgetImplFactory<GitHubReleasesWidget>());
        RecoverRunningWidgets();
    }

    private readonly Dictionary<string, IWidgetImplFactory> widgetDefinitionRegistry = new();
    private readonly Dictionary<string, WidgetImpl> runningWidgets = new();

    private void InitializeWidget(WidgetContext widgetContext, string state)
    {
        var widgetId = widgetContext.Id;
        var widgetDefinitionId = widgetContext.DefinitionId;
        _log.Verbose($"Calling Initialize for Widget Id: {widgetId} - {widgetDefinitionId}");
        if (widgetDefinitionRegistry.ContainsKey(widgetDefinitionId))
        {
            if (!runningWidgets.ContainsKey(widgetId))
            {
                var factory = widgetDefinitionRegistry[widgetDefinitionId];
                var widgetImpl = factory.Create(widgetContext, state);
                runningWidgets.Add(widgetId, widgetImpl);
            }
            else
            {
                _log.Warning($"Attempted to initialize a widget twice: {widgetDefinitionId} - {widgetId}");
            }
        }
        else
        {
            _log.Error($"Unknown widget DefinitionId: {widgetDefinitionId}");
        }
    }

    private void RecoverRunningWidgets()
    {
        WidgetInfo[] runningWidgets;
        try
        {
            runningWidgets = WidgetManager.GetDefault().GetWidgetInfos();
        }
        catch (Exception e)
        {
            _log.Error("Failed retrieving list of running widgets.", e);
            return;
        }

        if (runningWidgets is null)
        {
            _log.Debug("No running widgets to recover.");
            return;
        }

        foreach (var widgetInfo in runningWidgets)
        {
            if (!this.runningWidgets.ContainsKey(widgetInfo.WidgetContext.Id))
            {
                InitializeWidget(widgetInfo.WidgetContext, widgetInfo.CustomState);
            }
        }

        _log.Debug("Finished recovering widgets.");
    }

    public void CreateWidget(WidgetContext widgetContext)
    {
        _log.Information($"CreateWidget id: {widgetContext.Id} definitionId: {widgetContext.DefinitionId}");
        InitializeWidget(widgetContext, string.Empty);
    }

    public void Activate(WidgetContext widgetContext)
    {
        _log.Verbose($"Activate id: {widgetContext.Id} definitionId: {widgetContext.DefinitionId}");
        var widgetId = widgetContext.Id;
        if (runningWidgets.ContainsKey(widgetId))
        {
            runningWidgets[widgetId].Activate(widgetContext);
        }
        else
        {
            // Called to activate a widget that we don't know about, which is unexpected. Try to recover by creating it.
            _log.Warning($"Found WidgetId that was not known: {widgetContext.Id}, attempting to recover by creating it.");
            CreateWidget(widgetContext);
            if (runningWidgets.ContainsKey(widgetId))
            {
                runningWidgets[widgetId].Activate(widgetContext);
            }
        }
    }

    public void Deactivate(string widgetId)
    {
        _log.Verbose($"Deactivate id: {widgetId}");
        if (runningWidgets.ContainsKey(widgetId))
        {
            runningWidgets[widgetId].Deactivate(widgetId);
        }
    }

    public void DeleteWidget(string widgetId, string customState)
    {
        _log.Information($"DeleteWidget id: {widgetId}");
        if (runningWidgets.ContainsKey(widgetId))
        {
            runningWidgets[widgetId].DeleteWidget(widgetId, customState);
            runningWidgets.Remove(widgetId);
        }
    }

    public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        _log.Debug($"OnActionInvoked id: {actionInvokedArgs.WidgetContext.Id} definitionId: {actionInvokedArgs.WidgetContext.DefinitionId}");
        var widgetContext = actionInvokedArgs.WidgetContext;
        var widgetId = widgetContext.Id;
        if (runningWidgets.ContainsKey(widgetId))
        {
            runningWidgets[widgetId].OnActionInvoked(actionInvokedArgs);
        }
    }

    public void OnCustomizationRequested(WidgetCustomizationRequestedArgs customizationRequestedArgs)
    {
        _log.Debug($"OnCustomizationRequested id: {customizationRequestedArgs.WidgetContext.Id} definitionId: {customizationRequestedArgs.WidgetContext.DefinitionId}");
        var widgetContext = customizationRequestedArgs.WidgetContext;
        var widgetId = widgetContext.Id;
        if (runningWidgets.ContainsKey(widgetId))
        {
            runningWidgets[widgetId].OnCustomizationRequested(customizationRequestedArgs);
        }
    }

    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        _log.Debug($"OnWidgetContextChanged id: {contextChangedArgs.WidgetContext.Id} definitionId: {contextChangedArgs.WidgetContext.DefinitionId}");
        var widgetContext = contextChangedArgs.WidgetContext;
        var widgetId = widgetContext.Id;
        if (runningWidgets.ContainsKey(widgetId))
        {
            runningWidgets[widgetId].OnWidgetContextChanged(contextChangedArgs);
        }
    }
}
