// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubPlugin.Helpers;
internal class TimeSpanHelper
{
    public static string TimeSpanToDisplayString(TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds < 1)
        {
            return "now";
        }

        if (timeSpan.TotalMinutes < 1)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} second{1} ago", timeSpan.Seconds, timeSpan.Seconds > 1 ? "s" : string.Empty);
        }

        if (timeSpan.TotalHours < 1)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} minute{1} ago", timeSpan.Minutes, timeSpan.Minutes > 1 ? "s" : string.Empty);
        }

        if (timeSpan.TotalDays < 1)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} hour{1} ago", timeSpan.Hours, timeSpan.Hours > 1 ? "s" : string.Empty);
        }

        return string.Format(CultureInfo.CurrentCulture, "{0} day{1} ago", timeSpan.Days, timeSpan.Days > 1 ? "s" : string.Empty);
    }
}
