// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubPlugin.Helpers;
using Microsoft.Windows.DevHome.SDK;

namespace GitHubPlugin.Providers;

public class SettingsProvider : ISettingsProvider
{
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
