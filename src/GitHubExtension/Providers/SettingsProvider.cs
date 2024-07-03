// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Helpers;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace GitHubExtension.Providers;

public class SettingsProvider : ISettingsProvider
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(SettingsProvider)));

    private static readonly ILogger _log = _logger.Value;

    string ISettingsProvider.DisplayName => Resources.GetResource(@"SettingsProviderDisplayName");

    public AdaptiveCardSessionResult GetSettingsAdaptiveCardSession()
    {
        _log.Information($"GetSettingsAdaptiveCardSession");
        return new AdaptiveCardSessionResult(new SettingsUIController());
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
