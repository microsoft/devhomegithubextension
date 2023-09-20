﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubExtension.Telemetry;

/// <summary>
/// Telemetry Levels.
/// These levels are defined by our telemetry system, so it's possible the sampling
/// could change in the future.
/// There aren't any convenient enums we can consume, so create our own.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Local.
    /// Only log telemetry locally on the machine (similar to an ETW event).
    /// </summary>
    Local,

    /// <summary>
    /// Info.
    /// Send telemetry from internal and flighted machines, but no external retail machines.
    /// </summary>
    Info,

    /// <summary>
    /// Measure.
    /// Send telemetry from internal and flighted machines, plus a small, sample % of retail machines.
    /// Should only be used for telemetry we use to derive measures from.
    /// </summary>
    Measure,

    /// <summary>
    /// Critical.
    /// Send telemetry from all devices sampled at 100%.
    /// Should only be used for approved events.
    /// </summary>
    Critical,
}
