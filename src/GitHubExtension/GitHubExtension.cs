// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Runtime.InteropServices;
using GitHubExtension.DeveloperId;
using GitHubExtension.Providers;
using Microsoft.Windows.DevHome.SDK;

namespace GitHubExtension;

[ComVisible(true)]
[Guid("6B5F1179-B2AE-4D5E-94FC-E5E119D1B8F0")]
[ComDefaultInterface(typeof(IExtension))]
public sealed class GitHubExtension : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;

    private readonly WebServer.WebServer _webServer;

    public GitHubExtension(ManualResetEvent extensionDisposedEvent)
    {
        _extensionDisposedEvent = extensionDisposedEvent;

        var webcontentPath = Path.Combine(AppContext.BaseDirectory, "WebContent");
        _webServer = new WebServer.WebServer(webcontentPath);

        _webServer.RegisterRouteHandler("/api/test", HandleRequest);

        Console.WriteLine($"GitHubExtension is running on port {_webServer.Port}");
        var url = $"http://localhost:{_webServer.Port}/HelloWorld.html";
        Console.WriteLine($"Navigate to: {url}");
    }

    public object? GetProvider(ProviderType providerType)
    {
        switch (providerType)
        {
            case ProviderType.DeveloperId:
                return DeveloperIdProvider.GetInstance();
            case ProviderType.Repository:
                return new RepositoryProvider();
            case ProviderType.Settings:
                return new SettingsProvider();
            case ProviderType.FeaturedApplications:
                return new object();
            default:
                Providers.Log.Logger()?.ReportInfo("Invalid provider");
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
