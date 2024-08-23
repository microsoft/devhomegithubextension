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
    private readonly WebViewResult _webViewResult;

    string ISettingsProvider.DisplayName => Resources.GetResource(@"SettingsProviderDisplayName");

    public SettingsProvider2()
    {
        _webViewResult = new WebViewResult(string.Empty);
    }

    public SettingsProvider2(WebViewResult webViewResult)
    {
        _webViewResult = webViewResult;
        _log.Information($"SettingsProvider2 URL: {webViewResult.Url}");
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
        return _webViewResult;
    }
}
