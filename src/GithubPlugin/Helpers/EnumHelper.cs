// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.
using System.Globalization;
using GitHubPlugin.DataManager;

namespace GitHubPlugin.Helpers;
public class EnumHelper
{
    public static string SearchCategoryToString(SearchCategory searchCategory)
    {
        switch (searchCategory)
        {
            case SearchCategory.Issues:
                return "Issues";
            case SearchCategory.PullRequests:
                return "PRs";
            case SearchCategory.IssuesAndPullRequests:
                return "Issues & PRs";
            default:
                return "unknown";
        }
    }

    public static SearchCategory StringToSearchCategory(string value)
    {
        if (value == "Issues")
        {
            return SearchCategory.Issues;
        }

        if (value == "PRs")
        {
            return SearchCategory.PullRequests;
        }

        if (value == "Issues & PRs")
        {
            return SearchCategory.IssuesAndPullRequests;
        }

        return SearchCategory.Unknown;
    }
}
