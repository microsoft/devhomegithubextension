// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubPlugin.DataModel;

namespace GitHubPlugin.Helpers;
public class IconLoader
{
    private static readonly Dictionary<string, string> Base64ImageRegistry = new ();

    protected static readonly string Name = nameof(IconLoader);

    public static string GetIconAsBase64(string filename)
    {
        Log.Logger()?.ReportDebug(Name, $"Asking for icon: {filename}");
        if (!Base64ImageRegistry.ContainsKey(filename))
        {
            Base64ImageRegistry.Add(filename, ConvertIconToDataString(filename));
            Log.Logger()?.ReportDebug(Name, $"The icon {filename} was converted and is now stored.");
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
