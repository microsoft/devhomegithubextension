// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Globalization;
using DevHome.Logging;
using DevHome.Logging.Listeners;

namespace GithubExtension.Test;

public class TestListener : ListenerBase
{
    private TestContext? TestContext
    {
        get;
        set;
    }

    public Dictionary<SeverityLevel, int> EventCounts { get; } = new Dictionary<SeverityLevel, int>();

    public TestListener(string name, TestContext testContext)
        : base(name)
    {
        TestContext = testContext;
        EventCounts.Add(SeverityLevel.Critical, 0);
        EventCounts.Add(SeverityLevel.Error, 0);
        EventCounts.Add(SeverityLevel.Warn, 0);
        EventCounts.Add(SeverityLevel.Info, 0);
        EventCounts.Add(SeverityLevel.Debug, 0);
    }

    public void Reset()
    {
        foreach (var kvp in EventCounts)
        {
            EventCounts[kvp.Key] = 0;
        }
    }

    public bool FoundErrors()
    {
        return EventCounts[SeverityLevel.Critical] > 0 || EventCounts[SeverityLevel.Error] > 0;
    }

    public override void HandleLogEvent(LogEvent evt)
    {
        switch (evt.Severity)
        {
            case SeverityLevel.Critical:
                ++EventCounts[SeverityLevel.Critical];
                PrintEvent(evt);
                break;
            case SeverityLevel.Error:
                ++EventCounts[SeverityLevel.Error];
                PrintEvent(evt);
                break;
            case SeverityLevel.Warn:
                ++EventCounts[SeverityLevel.Warn];
                PrintEvent(evt);
                break;
            case SeverityLevel.Info:
                ++EventCounts[SeverityLevel.Info];
                PrintEvent(evt);
                break;
            case SeverityLevel.Debug:
                ++EventCounts[SeverityLevel.Debug];

                PrintEvent(evt);
                break;
        }
    }

    private void PrintEvent(LogEvent evt)
    {
        TestContext?.WriteLine($"[{evt.Source}] {evt.Severity.ToString().ToUpper(CultureInfo.InvariantCulture)}: {evt.Message}");
        if (evt.Exception != null)
        {
            TestContext?.WriteLine(evt.Exception.ToString());
        }
    }

    public void PrintEventCounts()
    {
        TestContext?.WriteLine($"Critical: {EventCounts[SeverityLevel.Critical]}  Error: {EventCounts[SeverityLevel.Error]}  Warning: {EventCounts[SeverityLevel.Warn]}");
    }
}
