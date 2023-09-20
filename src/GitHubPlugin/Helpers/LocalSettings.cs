// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Runtime.CompilerServices;
using DevHome.Common.Services;
using DevHome.Logging;
using Microsoft.Windows.ApplicationModel.Resources;
using Newtonsoft.Json.Linq;
using Windows.Storage;

namespace GitHubPlugin.Helpers;
public static class LocalSettings
{
    private static readonly string _applicationDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevHome/ApplicationData");
    private static readonly string _localSettingsFile = "LocalSettings.json";

    private static IDictionary<string, object>? _settings;

    private static async Task InitializeAsync()
    {
        if (_settings == null)
        {
            if (RuntimeHelper.IsMSIX)
            {
                _settings = new Dictionary<string, object>();
            }
            else
            {
                _settings = await Task.Run(() => FileHelper.Read<IDictionary<string, object>>(_applicationDataFolder, _localSettingsFile)) ?? new Dictionary<string, object>();
            }
        }
    }

    public static async Task<T?> ReadSettingAsync<T>(string key)
    {
        await InitializeAsync();

        if (_settings != null)
        {
            if (_settings.TryGetValue(key, out var obj))
            {
                return await Json.ToObjectAsync<T>((string)obj);
            }
            else
            {
                if (RuntimeHelper.IsMSIX)
                {
                    if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj2))
                    {
                        _settings![key] = obj2;
                        return await Json.ToObjectAsync<T>((string)obj2);
                    }
                }
            }
        }

        return default;
    }

    public static async Task SaveSettingAsync<T>(string key, T value)
    {
        await InitializeAsync();

        if (_settings != null)
        {
            _settings![key] = await Json.StringifyAsync(value!);

            if (RuntimeHelper.IsMSIX)
            {
                ApplicationData.Current.LocalSettings.Values[key] = _settings![key];
            }
            else
            {
                await Task.Run(() => FileHelper.Save(_applicationDataFolder, _localSettingsFile, _settings));
            }
        }
    }
}
