﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.Widgets.Providers;
using Serilog;

namespace GitHubExtension.Widgets;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Templated class")]
internal sealed class WidgetImplFactory<T> : IWidgetImplFactory
    where T : WidgetImpl, new()
{
    public WidgetImpl Create(WidgetContext widgetContext, string state)
    {
        var log = Log.ForContext("SourceContext", nameof(WidgetImpl));
        log.Debug($"In WidgetImpl Create for Id {widgetContext.Id} Definition: {widgetContext.DefinitionId} and state: '{state}'");
        WidgetImpl widgetImpl = new T();
        widgetImpl.CreateWidget(widgetContext, state);
        return widgetImpl;
    }
}
