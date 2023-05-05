// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.
using System.Globalization;
using GitHubPlugin.DataManager;
using GitHubPlugin.Widgets.Enums;

namespace GitHubPlugin.Helpers;
public class EnumHelper
{
    public static string SearchCategoryToString(SearchCategory searchCategory) => searchCategory switch
    {
        SearchCategory.Issues => "Issues",
        SearchCategory.PullRequests => "PullRequests",
        SearchCategory.IssuesAndPullRequests => "IssuesAndPullRequests",
        _ => "unknown"
    };

    public static SearchCategory StringToSearchCategory(string value)
    {
        try
        {
            return Enum.Parse<SearchCategory>(value);
        }
        catch (Exception)
        {
            // Invalid value.
            return SearchCategory.Unknown;
        }
    }
}
