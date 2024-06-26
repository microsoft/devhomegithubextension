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
        _widgetDefinitionRegistry.Add("GitHub_Issues", new WidgetImplFactory<GitHubIssuesWidget>());
        _widgetDefinitionRegistry.Add("GitHub_PullRequests", new WidgetImplFactory<GitHubPullsWidget>());
        _widgetDefinitionRegistry.Add("GitHub_MentionedIns", new WidgetImplFactory<GitHubMentionedInWidget>());
        _widgetDefinitionRegistry.Add("GitHub_Assigneds", new WidgetImplFactory<GitHubAssignedWidget>());
        _widgetDefinitionRegistry.Add("GitHub_Reviews", new WidgetImplFactory<GitHubReviewWidget>());
        _widgetDefinitionRegistry.Add("GitHub_Releases", new WidgetImplFactory<GitHubReleasesWidget>());
        RecoverRunningWidgets();
    }

    private readonly Dictionary<string, IWidgetImplFactory> _widgetDefinitionRegistry = new();
    private readonly Dictionary<string, WidgetImpl> _runningWidgets = new();

    private void InitializeWidget(WidgetContext widgetContext, string state)
    {
        var widgetId = widgetContext.Id;
        var widgetDefinitionId = widgetContext.DefinitionId;
        _log.Verbose($"Calling Initialize for Widget Id: {widgetId} - {widgetDefinitionId}");
        if (_widgetDefinitionRegistry.TryGetValue(widgetDefinitionId, out var widgetDefinition))
        {
            if (!_runningWidgets.ContainsKey(widgetId))
            {
                var factory = widgetDefinition;
                var widgetImpl = factory.Create(widgetContext, state);
                _runningWidgets.Add(widgetId, widgetImpl);
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
            _log.Error(e, "Failed retrieving list of running widgets.");
            return;
        }

        if (runningWidgets is null)
        {
            _log.Debug("No running widgets to recover.");
            return;
        }

        foreach (var widgetInfo in runningWidgets)
        {
            if (!_runningWidgets.ContainsKey(widgetInfo.WidgetContext.Id))
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
        if (_runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.Activate(widgetContext);
        }
        else
        {
            // Called to activate a widget that we don't know about, which is unexpected. Try to recover by creating it.
            _log.Warning($"Found WidgetId that was not known: {widgetContext.Id}, attempting to recover by creating it.");
            CreateWidget(widgetContext);
            if (_runningWidgets.TryGetValue(widgetId, out var newWidget))
            {
                newWidget.Activate(widgetContext);
            }
        }
    }

    public void Deactivate(string widgetId)
    {
        _log.Verbose($"Deactivate id: {widgetId}");
        if (_runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.Deactivate(widgetId);
        }
    }

    public void DeleteWidget(string widgetId, string customState)
    {
        _log.Information($"DeleteWidget id: {widgetId}");
        if (_runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.DeleteWidget(widgetId, customState);
            _runningWidgets.Remove(widgetId);
        }
    }

    public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        _log.Debug($"OnActionInvoked id: {actionInvokedArgs.WidgetContext.Id} definitionId: {actionInvokedArgs.WidgetContext.DefinitionId}");
        var widgetContext = actionInvokedArgs.WidgetContext;
        var widgetId = widgetContext.Id;
        if (_runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.OnActionInvoked(actionInvokedArgs);
        }
    }

    public void OnCustomizationRequested(WidgetCustomizationRequestedArgs customizationRequestedArgs)
    {
        _log.Debug($"OnCustomizationRequested id: {customizationRequestedArgs.WidgetContext.Id} definitionId: {customizationRequestedArgs.WidgetContext.DefinitionId}");
        var widgetContext = customizationRequestedArgs.WidgetContext;
        var widgetId = widgetContext.Id;
        if (_runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.OnCustomizationRequested(customizationRequestedArgs);
        }
    }

    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        _log.Debug($"OnWidgetContextChanged id: {contextChangedArgs.WidgetContext.Id} definitionId: {contextChangedArgs.WidgetContext.DefinitionId}");
        var widgetContext = contextChangedArgs.WidgetContext;
        var widgetId = widgetContext.Id;
        if (_runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.OnWidgetContextChanged(contextChangedArgs);
        }
    }
}
