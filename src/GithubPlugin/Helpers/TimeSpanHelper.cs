// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.
using System.Text.Json.Nodes;
using DevHome.Logging;

namespace GitHubPlugin.Helpers;
internal class TimeSpanHelper
{
    public static string TimeSpanToDisplayString(TimeSpan timeSpan, Logger? log = null)
    {
        if (timeSpan.TotalSeconds < 1)
        {
            return Resources.GetResource("WidgetTemplate_Now", log);
        }

        if (timeSpan.TotalSeconds < 2)
        {
            return $"1 {Resources.GetResource("WidgetTemplate_SecondAgo", log)}";
        }

        if (timeSpan.TotalMinutes < 1)
        {
            return $"{timeSpan.Seconds} {Resources.GetResource("WidgetTemplate_SecondsAgo", log)}";
        }

        if (timeSpan.TotalMinutes < 2)
        {
            return $"1 {Resources.GetResource("WidgetTemplate_MinuteAgo", log)}";
        }

        if (timeSpan.TotalHours < 1)
        {
            return $"{timeSpan.Minutes} {Resources.GetResource("WidgetTemplate_MinutesAgo", log)}";
        }

        if (timeSpan.TotalHours < 2)
        {
            return $"1 {Resources.GetResource("WidgetTemplate_HourAgo", log)}";
        }

        if (timeSpan.TotalDays < 1)
        {
            return $"{timeSpan.Hours} {Resources.GetResource("WidgetTemplate_HoursAgo", log)}";

        }

        if (timeSpan.TotalDays < 2)
        {
            return $"1 {Resources.GetResource("WidgetTemplate_DayAgo", log)}";
        }

        return $"{timeSpan.Days} {Resources.GetResource("WidgetTemplate_DaysAgo", log)}";
    }

    internal static string DateTimeOffsetToDisplayString(DateTimeOffset? dateTime, Logger? log)
    {
        if (dateTime == null)
        {
            return Resources.GetResource("WidgetTemplate_UnknownTime", log);
        }

        return TimeSpanToDisplayString(DateTime.UtcNow - dateTime.Value.DateTime, log);
    }
}
