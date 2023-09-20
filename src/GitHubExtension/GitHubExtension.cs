﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Runtime.InteropServices;
using GitHubExtension.DeveloperId;
using GitHubExtension.Providers;
using Microsoft.Windows.DevHome.SDK;

namespace GitHubExtension;

[ComVisible(true)]
[Guid("6B5F1179-B2AE-4D5E-94FC-E5E119D1B8F0")]
[ComDefaultInterface(typeof(IExtension))]
public sealed class GitHubExtension : IExtension
{
    private readonly ManualResetEvent _extensionDisposedEvent;

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
    }
}
