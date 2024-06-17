// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using GitHubExtension.Client;
using Microsoft.Windows.AppNotifications;
using Serilog;

namespace GitHubExtension.Notifications;

public class NotificationHandler
{
    private static readonly Lazy<ILogger> _log = new(() => Serilog.Log.ForContext("SourceContext", nameof(NotificationHandler)));

    private static readonly ILogger Log = _log.Value;

#pragma warning disable IDE0060 // Remove unused parameter
    public static void OnNotificationInvoked(object sender, AppNotificationActivatedEventArgs args) => NotificationActivation(args);
#pragma warning restore IDE0060 // Remove unused parameter

    public static void NotificationActivation(AppNotificationActivatedEventArgs args)
    {
        Log.Information($"Notification Activated with args: {NotificationArgsToString(args)}");

        if (args.Arguments.TryGetValue("htmlurl", out var htmlUrl))
        {
            try
            {
                // Do not assume this string is a safe URL and blindly execute it; verify that it is
                // in fact a valid GitHub URL.
                var urlString = htmlUrl;
                if (!Validation.IsValidGitHubURL(urlString))
                {
                    throw new InvalidGitHubUrlException($"{urlString} is invalid.");
                }

                Log.Information($"Launching Uri: {urlString}");
                var processStartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = urlString,
                };

                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed launching Uri for {htmlUrl}");
            }

            return;
        }
    }

    public static string NotificationArgsToString(AppNotificationActivatedEventArgs args)
    {
        var sb = new StringBuilder();
        foreach (var arg in args.Arguments)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{arg.Key}={arg.Value} ");
        }

        return sb.ToString();
    }
}
