﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace GitHubExtension.Helpers;

public static class Json
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        IncludeFields = true,
    };

    public static async Task<T> ToObjectAsync<T>(string value)
    {
        if (typeof(T) == typeof(bool))
        {
            return (T)(object)bool.Parse(value);
        }

        return await Task.Run<T>(() =>
        {
            return JsonConvert.DeserializeObject<T>(value)!;
        });
    }

    public static async Task<string> StringifyAsync<T>(T value)
    {
        if (typeof(T) == typeof(bool))
        {
            return value!.ToString()!.ToLowerInvariant();
        }

        return await Task.Run<string>(() =>
        {
            return JsonConvert.SerializeObject(value);
        });
    }

    public static string Stringify<T>(T value)
    {
        if (typeof(T) == typeof(bool))
        {
            return value!.ToString()!.ToLowerInvariant();
        }

        return System.Text.Json.JsonSerializer.Serialize(value, _options);
    }

    public static T? ToObject<T>(string json)
    {
        if (typeof(T) == typeof(bool))
        {
            return (T)(object)bool.Parse(json);
        }

        return System.Text.Json.JsonSerializer.Deserialize<T>(json, _options);
    }
}
