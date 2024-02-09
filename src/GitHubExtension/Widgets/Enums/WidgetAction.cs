// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubExtension.Widgets.Enums;

public enum WidgetAction
{
    /// <summary>
    /// Error condition where the action cannot be determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// Action to validate the URL provided by the user.
    /// </summary>
    CheckUrl,

    /// <summary>
    /// Action to initiate the user Sign-In.
    /// </summary>
    SignIn,

    /// <summary>
    /// Action to save after configuration.
    /// </summary>
    Save,

    /// <summary>
    /// Action to discard configuration changes and leave configuration flow.
    /// </summary>
    Cancel,
}
