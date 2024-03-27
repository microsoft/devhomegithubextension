// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Serilog;

namespace GitHubExtension.Helpers;

public class IconLoader
{
    private static readonly Dictionary<string, string> Base64ImageRegistry = new();

    public static string GetIconAsBase64(string filename)
    {
        var log = Log.ForContext("SourceContext", nameof(IconLoader));
        log.Verbose($"Asking for icon: {filename}");
        if (!Base64ImageRegistry.ContainsKey(filename))
        {
            Base64ImageRegistry.Add(filename, ConvertIconToDataString(filename));
            log.Verbose($"The icon {filename} was converted and is now stored.");
        }

        return Base64ImageRegistry[filename];
    }

    private static string ConvertIconToDataString(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, @"Widgets/Assets/", fileName);
        var imageData = Convert.ToBase64String(File.ReadAllBytes(path.ToString()));
        return imageData;
    }
}
