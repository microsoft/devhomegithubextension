// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using GitHubExtension.Client;
using Microsoft.Windows.AppNotifications;

namespace GitHubExtension.Notifications;

public class NotificationHandler
{
#pragma warning disable IDE0060 // Remove unused parameter
    public static void OnNotificationInvoked(object sender, AppNotificationActivatedEventArgs args) => NotificationActivation(args);
#pragma warning restore IDE0060 // Remove unused parameter

    public static void NotificationActivation(AppNotificationActivatedEventArgs args)
    {
        Log.Logger()?.ReportInfo($"Notification Activated with args: {NotificationArgsToString(args)}");

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

                Log.Logger()?.ReportInfo($"Launching Uri: {urlString}");
                var processStartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = urlString,
                };

                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError($"Failed launching Uri for {htmlUrl}", ex);
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
