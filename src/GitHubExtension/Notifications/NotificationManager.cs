// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.AppNotifications;
using Serilog;

namespace GitHubExtension.Notifications;

public class NotificationManager
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(NotificationManager)));

    private static readonly ILogger _log = _logger.Value;

    private bool _isRegistered;

    public NotificationManager(Windows.Foundation.TypedEventHandler<AppNotificationManager, AppNotificationActivatedEventArgs> handler)
    {
        AppNotificationManager.Default.NotificationInvoked += handler;
        AppNotificationManager.Default.Register();
        _isRegistered = true;
        _log.Information($"NotificationManager created and registered.");
    }

    ~NotificationManager()
    {
        Unregister();
    }

    public void Unregister()
    {
        if (_isRegistered)
        {
            AppNotificationManager.Default.Unregister();
            _isRegistered = false;
            _log.Information($"NotificationManager unregistered.");
        }
    }
}
