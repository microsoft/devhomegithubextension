﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Serilog;
using Windows.ApplicationModel.Activation;
using Windows.Management.Deployment;
using Windows.Storage;

namespace GitHubExtension;

public sealed class Program
{
    [MTAThread]
    public static void Main([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] string[] args)
    {
        // Setup Logging
        Environment.SetEnvironmentVariable("DEVHOME_LOGS_ROOT", ApplicationData.Current.TemporaryFolder.Path);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        Log.Information($"Launched with args: {string.Join(' ', args.ToArray())}");
        LogPackageInformation();

        // Set up notification handling. This must happen before GetActivatedEventArgs().
        var notificationManager = new Notifications.NotificationManager(Notifications.NotificationHandler.OnNotificationInvoked);

        // Force the app to be single instanced.
        // Get or register the main instance.
        var mainInstance = AppInstance.FindOrRegisterForKey("mainInstance");
        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (!mainInstance.IsCurrent)
        {
            Log.Information($"Not main instance, redirecting.");
            mainInstance.RedirectActivationToAsync(activationArgs).AsTask().Wait();
            notificationManager.Unregister();
            Log.CloseAndFlush();
            return;
        }

        // Register for activation redirection.
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
            Log.Warning("Not being launched as a ComServer... exiting.");
        }

        notificationManager.Unregister();
        Log.CloseAndFlush();
    }

    private static void AppActivationRedirected(object? sender, Microsoft.Windows.AppLifecycle.AppActivationArguments activationArgs)
    {
        Log.Information($"Redirected with kind: {activationArgs.Kind}");

        // Handle COM server.
        if (activationArgs.Kind == ExtendedActivationKind.Launch)
        {
            var d = activationArgs.Data as ILaunchActivatedEventArgs;
            var args = d?.Arguments.Split();

            if (args?.Length > 0 && args[1] == "-RegisterProcessAsComServer")
            {
                Log.Information($"Activation COM Registration Redirect: {string.Join(' ', args.ToList())}");
                HandleCOMServerActivation();
            }
        }

        // Handle Notification.
        if (activationArgs.Kind == ExtendedActivationKind.AppNotification)
        {
            HandleNotificationActivation(activationArgs);
        }

        // Handle Protocol.
        if (activationArgs.Kind == ExtendedActivationKind.Protocol)
        {
            var d = activationArgs.Data as IProtocolActivatedEventArgs;
            if (d is not null)
            {
                Log.Information($"Protocol Activation redirected from: {d.Uri}");
                HandleProtocolActivation(d.Uri);
            }
        }
    }

    private static void HandleNotificationActivation(AppActivationArguments activationArgs)
    {
        var notificationArgs = activationArgs.Data as AppNotificationActivatedEventArgs;
        if (notificationArgs != null)
        {
            Log.Information($"Notification Activation.");
            Notifications.NotificationHandler.NotificationActivation(notificationArgs);
        }
    }

    private static void HandleProtocolActivation(Uri oauthRedirectUri)
    {
        DeveloperId.DeveloperIdProvider.GetInstance().HandleOauthRedirection(oauthRedirectUri);
    }

    private static void HandleCOMServerActivation()
    {
        Log.Information($"Activating COM Server");

        // Register and run COM server.
        // This could be called by either of the COM registrations, we will do them all to avoid deadlock and bind all on the extension's lifetime.
        using var extensionServer = new Microsoft.Windows.DevHome.SDK.ExtensionServer();
        var extensionDisposedEvent = new ManualResetEvent(false);
        var extensionInstance = new GitHubExtension(extensionDisposedEvent);

        // We are instantiating extension instance once above, and returning it every time the callback in RegisterExtension below is called.
        // This makes sure that only one instance of SampleExtension is alive, which is returned every time the host asks for the IExtension object.
        // If you want to instantiate a new instance each time the host asks, create the new instance inside the delegate.
        extensionServer.RegisterExtension(() => extensionInstance, true);

        // Do Widget COM server registration
        // We are not using a disposed event for this, as we want the widgets to be disposed when the extension is disposed.
        using var widgetServer = new Widgets.WidgetServer();
        var widgetProviderInstance = new Widgets.WidgetProvider();
        widgetServer.RegisterWidget(() => widgetProviderInstance);

        // Set up the data updater. This will schedule updating the DataStore.
        using var dataUpdater = new DataManager.DataUpdater(GitHubDataManager.Update);
        _ = dataUpdater.Start();

        // This will make the main thread wait until the event is signaled by the extension class.
        // Since we have single instance of the extension object, we exit as soon as it is disposed.
        extensionDisposedEvent.WaitOne();
        Log.Information($"Extension is disposed.");
    }

    private static void LogPackageInformation()
    {
        var relatedPackageFamilyNames = new string[]
        {
              "MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy",
              "Microsoft.Windows.DevHome_8wekyb3d8bbwe",
              "Microsoft.Windows.DevHomeGitHubExtension_8wekyb3d8bbwe",
              "Microsoft.Windows.DevHomeGitHubExtension.Dev_8wekyb3d8bbwe",
        };

        try
        {
            var packageManager = new PackageManager();
            foreach (var pfn in relatedPackageFamilyNames)
            {
                foreach (var package in packageManager.FindPackagesForUser(string.Empty, pfn))
                {
                    Log.Information($"{package.Id.FullName}  DevMode: {package.IsDevelopmentMode}  Signature: {package.SignatureKind}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed getting package information.");
        }
    }
}
