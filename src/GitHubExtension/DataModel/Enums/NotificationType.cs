// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubExtension.DataModel;

public enum NotificationType
{
    Unknown = 0,
    CheckRunFailed = 1,
    CheckRunSucceeded = 2,
    NewReview = 3,
}
