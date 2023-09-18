﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace GitHubPlugin.Widgets;
public enum WidgetDataState
{
    Unknown,
    Requested,      // Request is out, waiting on a response. Current data is stale.
    Okay,           // Received and updated data, stable state.
    Failed,         // Failed retrieving data.
}
