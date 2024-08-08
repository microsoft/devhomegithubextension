// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Helpers;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace GitHubExtension.Providers;

public class SettingsProvider2 : ISettingsProvider2
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(SettingsProvider)));

    private static readonly ILogger _log = _logger.Value;

    string ISettingsProvider.DisplayName => Resources.GetResource(@"SettingsProviderDisplayName");

    private readonly string _url;

    public SettingsProvider2()
    {
        _url = string.Empty;
    }

    public SettingsProvider2(string url)
    {
        _log.Information($"SettingsProvider2 URL: {url}");
        _url = url;
    }

    public AdaptiveCardSessionResult GetSettingsAdaptiveCardSession()
    {
        _log.Information($"GetSettingsAdaptiveCardSession");
        return new AdaptiveCardSessionResult(new SettingsUIController());
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public WebViewResult GetSettingsWebView()
    {
        _log.Information($"GetSettingsWebView");
        return new WebViewResult(_url);
    }
}
