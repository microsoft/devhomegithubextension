// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Helpers;
using Microsoft.Windows.DevHome.SDK;

namespace GitHubExtension.Providers;

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
