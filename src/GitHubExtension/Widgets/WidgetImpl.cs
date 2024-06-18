﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.Widgets.Providers;
using Serilog;

namespace GitHubExtension.Widgets;

public abstract class WidgetImpl
{
    private string _state = string.Empty;

    public WidgetImpl()
    {
        _logger = new(() => Serilog.Log.ForContext("SourceContext", SourceName));
    }

    private readonly Lazy<ILogger> _logger;

    protected ILogger Log => _logger.Value;

    protected string Name => GetType().Name;

    protected string Id { get; set; } = string.Empty;

    // This is not a unique identifier, but is easier to read in a log and highly unlikely to
    // match another running widget.
    protected string ShortId => Id.Length > 6 ? Id[..6] : Id;

    protected string SourceName => string.IsNullOrEmpty(ShortId) ? Name : $"{Name}/{ShortId}";

    public string State()
    {
        return _state;
    }

    public void SetState(string state)
    {
        _state = state;
    }

    public abstract void CreateWidget(WidgetContext widgetContext, string state);

    public abstract void Activate(WidgetContext widgetContext);

    public abstract void Deactivate(string widgetId);

    public abstract void DeleteWidget(string widgetId, string customState);

    public abstract void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs);

    public abstract void OnCustomizationRequested(WidgetCustomizationRequestedArgs customizationRequestedArgs);

    public abstract void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs);
}
