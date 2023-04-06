﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubPlugin.PluginServer;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Windows.ApplicationModel.Activation;
using Windows.Management.Deployment;

namespace GitHubPlugin;
public sealed class Program
{
    [MTAThread]
    public static void Main([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] string[] args)
    {
        Log.Logger()?.ReportInfo($"Launched with args: {string.Join(' ', args.ToArray())}");
        LogPackageInformation();

        // TODO: Do we need a lock here to prevent race condition around MainInstance registered?

        // Set up notification handling. This must happen before GetActivatedEventArgs().
        var notificationManager = new Notifications.NotificationManager(Notifications.NotificationHandler.OnNotificationInvoked);

        // Force the app to be single instanced
        // Get or register the main instance
        var mainInstance = AppInstance.FindOrRegisterForKey("mainInstance");
        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

        // If the main instance isn't this current instance
        if (!mainInstance.IsCurrent)
        {
            Log.Logger()?.ReportInfo($"Not main instance, redirecting.");
            mainInstance.RedirectActivationToAsync(activationArgs).AsTask().Wait();
            notificationManager.Unregister();
            return;
        }

        // Otherwise, we're in the main instance
        // Register for activation redirection
        AppInstance.GetCurrent().Activated += AppActivationRedirected;

        if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
        {
            HandleCOMServerActivation();
        }
        else if (activationArgs.Kind == ExtendedActivationKind.AppNotification)
        {
            HandleNotificationActivation(activationArgs);
        }
        else
        {
            Log.Logger()?.ReportWarn("Not being launched as a ComServer... exiting.");
        }

        notificationManager.Unregister();
        Log.Logger()?.Dispose();
    }

    private static void AppActivationRedirected(object? sender, Microsoft.Windows.AppLifecycle.AppActivationArguments activationArgs)
    {
        Log.Logger()?.ReportInfo($"Redirected with kind: {activationArgs.Kind}");

        // Handle COM server
        if (activationArgs.Kind == ExtendedActivationKind.Launch)
        {
            var d = activationArgs.Data as ILaunchActivatedEventArgs;
            var args = d?.Arguments.Split();

            if (args?.Length > 0 && args[1] == "-RegisterProcessAsComServer")
            {
                Log.Logger()?.ReportInfo($"Activation COM Registration Redirect: {string.Join(' ', args.ToList())}");
                HandleCOMServerActivation();
            }
        }

        // Handle Notification
        if (activationArgs.Kind == ExtendedActivationKind.AppNotification)
        {
            HandleNotificationActivation(activationArgs);
        }

        // Handle Protocol
        if (activationArgs.Kind == ExtendedActivationKind.Protocol)
        {
            var d = activationArgs.Data as IProtocolActivatedEventArgs;
            if (d is not null)
            {
                Log.Logger()?.ReportInfo($"Protocol Activation redirected from: {d.Uri}");
                HandleProtocolActivation(d.Uri);
            }
        }

        // TODO: Handle other activation types
        // TODO: Handle the case when protocol activation happens first - This shouldn't happen since we'd have 0 active oauth requests.
    }

    private static void HandleNotificationActivation(AppActivationArguments activationArgs)
    {
        var notificationArgs = activationArgs.Data as AppNotificationActivatedEventArgs;
        if (notificationArgs != null)
        {
            Log.Logger()?.ReportInfo($"Notification Activation.");
            Notifications.NotificationHandler.NotificationActivation(notificationArgs);
        }
    }

    private static void HandleProtocolActivation(Uri oauthRedirectUri)
    {
        DeveloperId.DeveloperIdProvider.GetInstance().HandleOauthRedirection(oauthRedirectUri);
    }

    private static void HandleCOMServerActivation()
    {
        Log.Logger()?.ReportInfo($"Activating COM Server");

        // Register and run COM server
        // This could be called by either of the COM registrations, we will do them all to avoid deadlock and bind all on the plugin's lifetime.
        using var pluginServer = new Microsoft.Windows.DevHome.SDK.PluginServer();
        var pluginDisposedEvent = new ManualResetEvent(false);
        var pluginInstance = new GitHubPlugin(pluginDisposedEvent);

        // We are instantiating plugin instance once above, and returning it every time the callback in RegisterPlugin below is called.
        // This makes sure that only one instance of SamplePlugin is alive, which is returned every time the host asks for the IPlugin object.
        // If you want to instantiate a new instance each time the host asks, create the new instance inside the delegate.
        pluginServer.RegisterPlugin(() => pluginInstance, true);

        // Do Widget COM server registration
        // We are not using a disposed event for this, as we want the widgets to be disposed when the plugin is disposed.
        using var widgetServer = new Widgets.WidgetServer();
        var widgetProviderInstance = new Widgets.WidgetProvider();
        widgetServer.RegisterWidget(() => widgetProviderInstance);

        // Set up the data updater. This will schedule updating the DataStore.
        using var dataUpdater = new DataManager.DataUpdater(GitHubDataManager.Update);
        _ = dataUpdater.Start();

        // This will make the main thread wait until the event is signalled by the plugin class.
        // Since we have single instance of the plugin object, we exit as sooon as it is disposed.
        pluginDisposedEvent.WaitOne();
        Log.Logger()?.ReportInfo($"Plugin is disposed.");
    }

    private static void LogPackageInformation()
    {
        var relatedPackageFamilyNames = new string[]
        {
              "MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy",
              "Microsoft.Windows.DevHome_8wekyb3d8bbwe",
              "Microsoft.Windows.DevHomeGitHubExtension_8wekyb3d8bbwe",
        };

        try
        {
            var packageManager = new PackageManager();
            foreach (var pfn in relatedPackageFamilyNames)
            {
                foreach (var package in packageManager.FindPackagesForUser(string.Empty, pfn))
                {
                    Log.Logger()?.ReportInfo($"{package.Id.FullName}  Devmode: {package.IsDevelopmentMode}  Signature: {package.SignatureKind}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError("Failed getting package information.", ex);
        }
    }
}
