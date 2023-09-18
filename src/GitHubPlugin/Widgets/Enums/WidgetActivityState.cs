﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace GitHubPlugin.Widgets.Enums;
public enum WidgetActivityState
{
    /// <summary>
    /// Error state and default initialization. This state is a clue something went terribly wrong.
    /// </summary>
    Unknown,

    /// <summary>
    /// Widget is in this state after it is created before it has data assigned to it and before it
    /// is pinned. Once data is assigned this state should never be reached.
    /// </summary>
    Configure,

    /// <summary>
    /// Widget cannot do more until signed in.
    /// </summary>
    SignIn,

    /// <summary>
    /// Widget is configured, pinned, and it is assumed user can interact and see it.
    /// </summary>
    Active,

    /// <summary>
    /// Widget is in good state, but host is minimized or disables the widget.
    /// </summary>
    Inactive,
}
