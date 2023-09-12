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

    string ISettingsProvider.DisplayName => Resources.GetResource(@"SettingsProviderDisplayName");

    public AdaptiveCardSessionResult GetSettingsAdaptiveCardSession()
    {
        Log.Logger()?.ReportInfo($"GetSettingsAdaptiveCardSession");
        return new AdaptiveCardSessionResult(new SettingsUIController());
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
