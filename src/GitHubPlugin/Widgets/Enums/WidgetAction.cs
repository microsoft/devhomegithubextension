// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace GitHubPlugin.Widgets.Enums;
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
}
