// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Runtime.InteropServices;
using GitHubExtension.DeveloperId;
using GitHubExtension.Providers;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace GitHubExtension;

[ComVisible(true)]
#if CANARY_BUILD
[Guid("7AB70F8F-3644-495C-B473-A6750AE1D547")]
#elif STABLE_BUILD
[Guid("6B5F1179-B2AE-4D5E-94FC-E5E119D1B8F0")]
#else
[Guid("190B5CB2-BBAC-424E-92F8-98C7C41C1039")]
#endif
[ComDefaultInterface(typeof(IExtension))]
public sealed class GitHubExtension : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(GitHubExtension));

    private readonly WebServer.WebServer _webServer;

    private readonly string _url = string.Empty;

    public GitHubExtension(ManualResetEvent extensionDisposedEvent)
    {
        _extensionDisposedEvent = extensionDisposedEvent;

        var webcontentPath = Path.Combine(AppContext.BaseDirectory, "WebContent");
        Console.WriteLine($"Web content path: {webcontentPath}");
        _webServer = new WebServer.WebServer(webcontentPath);
        _webServer.RegisterRouteHandler("/api/test", HandleRequest);
        string extensionSettingsWebPage = "testjquery.html";

        Console.WriteLine($"GitHubExtension is running on port {_webServer.Port}");
        _url = $"http://localhost:{_webServer.Port}/{extensionSettingsWebPage}";
        Console.WriteLine($"Navigate to: {_url}");
    }

    public object? GetProvider(ProviderType providerType)
    {
        Console.WriteLine($"GetProvider called. URL = {_url}");
        switch (providerType)
        {
            case ProviderType.DeveloperId:
                return DeveloperIdProvider.GetInstance();
            case ProviderType.Repository:
                return new RepositoryProvider();
            case ProviderType.Settings:
                return new SettingsProvider2(_url);
            case ProviderType.FeaturedApplications:
                return new object();
            default:
                _log.Warning($"Invalid provider: {providerType}");
                return null;
        }
    }

    public void Dispose()
    {
        _extensionDisposedEvent.Set();
        _webServer.Dispose();
    }

    public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        Console.WriteLine("Received request for /api/test");
        return true;
    }
}
