﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace GitHubExtension.DataModel;

public enum CommitState
{
    Unknown = -1,

    /// <summary>
    /// There are no statuses.
    /// </summary>
    None = 0,

    /// <summary>
    /// Error in the build.
    /// </summary>
    Error = 1,

    /// <summary>
    /// Reported failure.
    /// </summary>
    Failure = 2,

    /// <summary>
    /// If no statuses or context is pending.
    /// </summary>
    Pending = 3,

    /// <summary>
    /// If latest status for all contexts is success.
    /// </summary>
    Success = 4,
}
