// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace GitHubExtension.DataModel;

public enum CheckStatus
{
    // Ordered by state from not started to completed.
    Unknown = -1,
    None = 0,
    Queued = 1,
    InProgress = 2,
    Completed = 3,
}
