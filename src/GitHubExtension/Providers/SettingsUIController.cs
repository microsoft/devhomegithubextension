// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Helpers;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;

namespace GitHubExtension.Providers;

internal sealed class SettingsUIController : IExtensionAdaptiveCardSession
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(RepositoryProvider)));

    private static readonly ILogger _log = _logger.Value;

    private static readonly string _notificationsEnabledString = "NotificationsEnabled";

    private IExtensionAdaptiveCard? _settingsUI;
    private static readonly SettingsUITemplate _settingsUITemplate = new();

    public void Dispose()
    {
        _log.Debug($"Dispose");
        _settingsUI?.Update(null, null, null);
    }

    public ProviderOperationResult Initialize(IExtensionAdaptiveCard extensionUI)
    {
        _log.Debug($"Initialize");
        _settingsUI = extensionUI;
        return _settingsUI.Update(_settingsUITemplate.GetSettingsUITemplate(), null, "SettingsPage");
    }

    public IAsyncOperation<ProviderOperationResult> OnAction(string action, string inputs)
    {
        return Task.Run(async () =>
        {
            ProviderOperationResult operationResult;
            _log.Information($"OnAction() called with state:{_settingsUI?.State}");
            _log.Debug($"action: {action}");

            switch (_settingsUI?.State)
            {
                case "SettingsPage":
                    {
                        _log.Debug($"inputs: {inputs}");

                        var currentNotificationsEnabled = LocalSettings.ReadSettingAsync<string>(_notificationsEnabledString).Result ?? "true";
                        await LocalSettings.SaveSettingAsync(_notificationsEnabledString, currentNotificationsEnabled == "true" ? "false" : "true");

                        operationResult = _settingsUI.Update(_settingsUITemplate.GetSettingsUITemplate(), null, "SettingsPage");

                        break;
                    }

                default:
                    {
                        _log.Error($"Unexpected state:{_settingsUI?.State}");
                        operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Something went wrong", $"Unexpected state:{_settingsUI?.State}");
                        break;
                    }
            }

            return operationResult;
        }).AsAsyncOperation();
    }

    // Adaptive Card Templates for SettingsUI.
    private sealed class SettingsUITemplate
    {
        internal string GetSettingsUITemplate()
        {
            var loader = new ResourceLoader(ResourceLoader.GetDefaultResourceFilePath(), "GitHubExtension/Resources");

            var notificationsEnabled = LocalSettings.ReadSettingAsync<string>(_notificationsEnabledString).Result ?? "true";
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
