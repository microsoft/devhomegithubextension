// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
