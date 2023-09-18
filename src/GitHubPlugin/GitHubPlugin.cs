﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Runtime.InteropServices;
using GitHubPlugin.DeveloperId;
using GitHubPlugin.Providers;
using Microsoft.Windows.DevHome.SDK;

namespace GitHubPlugin;

[ComVisible(true)]
[Guid("6B5F1179-B2AE-4D5E-94FC-E5E119D1B8F0")]
[ComDefaultInterface(typeof(IExtension))]
public sealed class GitHubPlugin : IExtension
{
    private readonly ManualResetEvent _pluginDisposedEvent;

    public GitHubPlugin(ManualResetEvent pluginDisposedEvent)
    {
        _pluginDisposedEvent = pluginDisposedEvent;
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
        _pluginDisposedEvent.Set();
    }
}
