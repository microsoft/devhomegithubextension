// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.
using System.Globalization;

namespace GitHubPlugin.Helpers;
internal class ColorHelper
{
    public static string GithubColorToWidgetColor(string githubColor)
    {
        switch (githubColor.ToLower(CultureInfo.CurrentCulture))
        {
            case "e60000":
                return "attention";
            case "ffc1cb":
                return "warning";
            case "d5aed5":
            case "885988":
                return "accent";
            case "bfdadc":
            case "e99695":
                return "light";
            default:
                return "default";
        }
    }
}
