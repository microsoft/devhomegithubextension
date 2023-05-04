// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Logging;
using Microsoft.Windows.ApplicationModel.Resources;

namespace GitHubPlugin.Helpers;
public static class Resources
{
    private static ResourceLoader? _resourceLoader;

    public static string GetResource(string identifier, Logger? log = null)
    {
        try
        {
            if (_resourceLoader == null)
            {
                _resourceLoader = new ResourceLoader(ResourceLoader.GetDefaultResourceFilePath(), "GitHubPlugin/Resources");
            }

            return _resourceLoader.GetString(identifier);
        }
        catch (Exception ex)
        {
            log?.ReportError($"Failed loading resource: {identifier}", ex);

            // If we fail, load the original identifier so it is obvious which resource is missing.
            return identifier;
        }
    }

    // Replaces all identifiers in the provided list in the target string. Assumes all identifiers
    // are wrapped with '%' to prevent sub-string replacement errors. This is intended for strings
    // such as a JSON string with resource identifiers embedded.
    public static string ReplaceIdentifers(string str, string[] resourceIdentifiers, Logger? log = null)
    {
        var start = DateTime.Now;
        foreach (var identifier in resourceIdentifiers)
        {
            // What is faster, String.Replace, RegEx, or StringBuilder.Replace? It is String.Replace().
            // https://learn.microsoft.com/en-us/archive/blogs/debuggingtoolbox/comparing-regex-replace-string-replace-and-stringbuilder-replace-which-has-better-performance
            var resourceString = GetResource(identifier, log);
            str = str.Replace($"%{identifier}%", resourceString);
        }

        var elapsed = DateTime.Now - start;
        log?.ReportDebug($"Replaced identifiers in {elapsed.TotalMilliseconds}ms");
        return str;
    }

    // These are all the string identifiers that appear in widgets.
    public static string[] GetWidgetResourceIdentifiers()
    {
        return new string[]
        {
            "Widget_Template/ContentLoading",
            "Widget_Template/EmptyIssues",
            "Widget_Template/EmptyPulls",
            "Widget_Template/Pulls",
            "Widget_Template/Issues",
            "Widget_Template/Opened",
            "Widget_Template/By",
            "Extension_Name/GitHub",
            "Widget_Template/Repository",
            "Widget_Template/Label",
            "Widget_Template/Loading",
            "Widget_Template/Author",
            "Widget_Template/Milestone",
            "Widget_Template/Project",
            "Widget_Template/PullRequests",
            "Widget_Template_Label/Url",
            "Widget_Template_Tooltip/Submit",
            "Widget_Template_Button/Submit",
            "Widget_Template_Tooltip/OpenPullRequest",
            "Widget_Template_Input/UrlPlaceholder",
            "Widget_Template_Tooltip/OpenIssue",
            "Widget_Template_Button/SignIn",
            "Widget_Template_Tooltip/SignIn",
            "Widget_Template/Assigned",
            "Widget_Template/AssignedTo",
            "Widget_Template/AssignedTitle",
            "Widget_Template/Mentioned",
            "Widget_Template/MentionedLow",
            "Widget_Template/Mentioned_user",
            "Widget_Template/Open",
        };
    }
}
