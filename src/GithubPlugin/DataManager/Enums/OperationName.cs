// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace GitHubPlugin.DataManager;
public enum OperationName
{
    UpdateAllDataForRepositoryAsync,
    UpdatePullRequestsForRepositoryAsync,
    UpdateIssuesForRepositoryAsync,
    UpdatePullRequestsForLoggedInDeveloperIdsAsync,
    UpdatePullRequestsReviewRequestedForRepositoryAsync,
    UpdateAssignedToAsync,
    UpdateMentionedInAsync,
    Unknown,
}
