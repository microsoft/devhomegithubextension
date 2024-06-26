// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Jeffijoe.MessageFormat;
using Serilog;

namespace GitHubExtension.Helpers;

internal sealed class TimeSpanHelper
{
    public static string TimeSpanToDisplayString(TimeSpan timeSpan, ILogger? log = null)
    {
        if (timeSpan.TotalSeconds < 1)
        {
            return Resources.GetResource("WidgetTemplate_Now", log);
        }

        if (timeSpan.TotalMinutes < 1)
        {
            return MessageFormatter.Format(Resources.GetResource("WidgetTemplate_SecondsAgo", log), new { seconds = timeSpan.Seconds });
        }

        if (timeSpan.TotalHours < 1)
        {
            return MessageFormatter.Format(Resources.GetResource("WidgetTemplate_MinutesAgo", log), new { minutes = timeSpan.Minutes });
        }

        if (timeSpan.TotalDays < 1)
        {
            return MessageFormatter.Format(Resources.GetResource("WidgetTemplate_HoursAgo", log), new { hours = timeSpan.Hours });
        }

        return MessageFormatter.Format(Resources.GetResource("WidgetTemplate_DaysAgo", log), new { days = timeSpan.Days });
    }

    internal static string DateTimeOffsetToDisplayString(DateTimeOffset? dateTime, ILogger? log)
    {
        if (dateTime == null)
        {
            return Resources.GetResource("WidgetTemplate_UnknownTime", log);
        }

        return TimeSpanToDisplayString(DateTime.UtcNow - dateTime.Value.DateTime, log);
    }
}
