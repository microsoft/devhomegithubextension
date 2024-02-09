// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.AppNotifications;

namespace GitHubExtension.Notifications;

public class NotificationManager
{
    private bool isRegistered;

    public NotificationManager(Windows.Foundation.TypedEventHandler<AppNotificationManager, AppNotificationActivatedEventArgs> handler)
    {
        AppNotificationManager.Default.NotificationInvoked += handler;
        AppNotificationManager.Default.Register();
        isRegistered = true;
        Log.Logger()?.ReportInfo($"NotificationManager created and registered.");
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
            Log.Logger()?.ReportInfo($"NotificationManager unregistered.");
        }
    }
}
