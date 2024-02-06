// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubExtension.Widgets;

public enum WidgetDataState
{
    Unknown,
    Requested,      // Request is out, waiting on a response. Current data is stale.
    Okay,           // Received and updated data, stable state.
    Failed,         // Failed retrieving data.
}
