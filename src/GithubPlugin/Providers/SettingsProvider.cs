// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Xml.Linq;
using GitHubPlugin.Client;
using GitHubPlugin.DeveloperId;
using GitHubPlugin.Helpers;
using Microsoft.Windows.DevHome.SDK;
using Octokit;
using Windows.Foundation;

namespace GitHubPlugin.Providers;

public class SettingsProvider : ISettingsProvider
{
    public SettingsProvider()
    {
    }

    public string GetName() => Resources.GetResource(@"SettingsProviderDisplayName");

    public IPluginAdaptiveCardController GetAdaptiveCardController(string[] args)
    {
        Log.Logger()?.ReportInfo($"GetAdaptiveCardController");
        return new SettingsUIController();
    }
}
