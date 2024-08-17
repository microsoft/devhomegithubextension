// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
public sealed class GitHubExtension : IExtension
{
    private readonly ManualResetEvent _extensionDisposedEvent;
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(GitHubExtension));

    public GitHubExtension(ManualResetEvent extensionDisposedEvent)
    {
        _extensionDisposedEvent = extensionDisposedEvent;
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
            case ProviderType.ComputeSystem:
                return new ComputeSystemProvider();
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
    }
}
