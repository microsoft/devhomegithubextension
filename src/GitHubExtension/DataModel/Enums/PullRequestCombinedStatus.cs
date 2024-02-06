// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubExtension.DataModel;

public enum PullRequestCombinedStatus
{
    Unknown = -1,

    /// <summary>
    /// There are no statuses.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Error in the build.
    /// </summary>
    Failed = 1,

    /// <summary>
    /// Reported failure.
    /// </summary>
    Success = 2,
}
