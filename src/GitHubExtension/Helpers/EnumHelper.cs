﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.DataManager;

namespace GitHubExtension.Helpers;

public class EnumHelper
{
    public static string SearchCategoryToString(SearchCategory searchCategory) => searchCategory switch
    {
        SearchCategory.Issues => "Issues",
        SearchCategory.PullRequests => "PullRequests",
        SearchCategory.IssuesAndPullRequests => "IssuesAndPullRequests",
        _ => "unknown",
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
