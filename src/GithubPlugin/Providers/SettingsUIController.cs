// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubPlugin.Helpers;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.DevHome.SDK;
using Newtonsoft.Json.Linq;
using Windows.Foundation;

namespace GitHubPlugin.Providers;
internal class SettingsUIController : IExtensionAdaptiveCardSession
{
    private IExtensionAdaptiveCard? _settingsUI;
    private static readonly SettingsUITemplate _settingsUITemplate = new ();

    public SettingsUIController()
    {
    }

    public void Dispose()
    {
        Log.Logger()?.ReportDebug($"Dispose");
        _settingsUI?.Update(null, null, null);
    }

    public ProviderOperationResult Initialize(IExtensionAdaptiveCard extensionUI)
    {
        Log.Logger()?.ReportDebug($"Initialize");
        _settingsUI = extensionUI;
        return _settingsUI.Update(_settingsUITemplate.GetSettingsUITemplate(), null, "SettingsPage");
    }

    public IAsyncOperation<ProviderOperationResult> OnAction(string action, string inputs)
    {
        return Task.Run(async () =>
        {
            ProviderOperationResult operationResult;
            Log.Logger()?.ReportInfo($"OnAction() called with state:{_settingsUI?.State}");
            Log.Logger()?.ReportDebug($"action: {action}");

            switch (_settingsUI?.State)
            {
                case "SettingsPage":
                    {
                        Log.Logger()?.ReportDebug($"inputs: {inputs}");

                        var currentNotificationsEnabled = LocalSettings.ReadSettingAsync<string>("NotificationsEnabled").Result ?? "true";
                        await LocalSettings.SaveSettingAsync("NotificationsEnabled", currentNotificationsEnabled == "true" ? "false" : "true");

                        operationResult = _settingsUI.Update(_settingsUITemplate.GetSettingsUITemplate(), null, "SettingsPage");

                        break;
                    }

                default:
                    {
                        Log.Logger()?.ReportError($"Unexpected state:{_settingsUI?.State}");
                        operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Something went wrong", $"Unexpected state:{_settingsUI?.State}");
                        break;
                    }
            }

            return operationResult;
        }).AsAsyncOperation();
    }

    // Adaptive Card Templates for SettingsUI.
    private class SettingsUITemplate
    {
        internal string GetSettingsUITemplate()
        {
            var loader = new ResourceLoader(ResourceLoader.GetDefaultResourceFilePath(), "GitHubPlugin/Resources");

            var notificationsEnabled = LocalSettings.ReadSettingAsync<string>("NotificationsEnabled").Result ?? "true";
            var notificationsEnabledString = (notificationsEnabled == "true") ? loader.GetString("Settings_NotificationsEnabled") : loader.GetString("Settings_NotificationsDisabled");

            var settingsUI = @"
{
    ""type"": ""AdaptiveCard"",
    ""body"": [
        {
            ""type"": ""ActionSet"",
            ""actions"": [
                {
                    ""type"": ""Action.Submit"",
                    ""title"": """ + $"{notificationsEnabledString}" + @""",
                    ""associatedInputs"": ""auto""
                }
            ]
        }
    ],
    ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
    ""version"": ""1.5"",
    ""minHeight"": ""200px""
}
";

            return settingsUI;
        }
    }
}
