// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace GitHubExtension.Test;

public class TestContextSink : ILogEventSink
{
    private readonly TestContext? _testContext;

    private readonly MessageTemplateTextFormatter _formatter;

    public TestContextSink(IFormatProvider formatProvider, TestContext testContext, string outputTemplate)
    {
        _testContext = testContext;
        _formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
    }

    public void Emit(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        _testContext?.Write(writer.ToString());
    }
}
