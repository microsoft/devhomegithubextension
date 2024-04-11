// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.AppNotifications;
using Serilog;

namespace GitHubExtension.Notifications;

public class NotificationManager
{
    private static readonly Lazy<ILogger> _log = new(() => Serilog.Log.ForContext("SourceContext", nameof(NotificationManager)));

    private static readonly ILogger Log = _log.Value;

    private bool isRegistered;

    public NotificationManager(Windows.Foundation.TypedEventHandler<AppNotificationManager, AppNotificationActivatedEventArgs> handler)
    {
        AppNotificationManager.Default.NotificationInvoked += handler;
        AppNotificationManager.Default.Register();
        isRegistered = true;
        Log.Information($"NotificationManager created and registered.");
    }

    ~NotificationManager()
    {
        Unregister();
    }

    public void Unregister()
    {
        if (isRegistered)
        {
            AppNotificationManager.Default.Unregister();
            isRegistered = false;
            Log.Information($"NotificationManager unregistered.");
        }
    }
}
