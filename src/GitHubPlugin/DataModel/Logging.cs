﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Logging;
using Windows.Storage;

namespace GitHubPlugin.DataModel;
public class Log
{
    private static Logger? _logger;

    public static Logger? Logger()
    {
        try
        {
            _logger ??= new Logger("DataStore", GetLoggingOptions());
        }
        catch
        {
            // Do nothing if logger fails.
        }

        return _logger;
    }

    public static void Attach(Logger logger)
    {
        if (logger is not null)
        {
            _logger?.Dispose();
            _logger = logger;
        }
    }

    public static Options GetLoggingOptions()
    {
        return new Options
        {
            LogFileFolderRoot = ApplicationData.Current.TemporaryFolder.Path,
            LogFileName = "DataStore_{now}.log",
            LogFileFolderName = "DataStore",
            DebugListenerEnabled = true,
#if DEBUG
            LogStdoutEnabled = true,
            LogStdoutFilter = SeverityLevel.Info,
            LogFileFilter = SeverityLevel.Info,
#else
            LogStdoutEnabled = false,
            LogStdoutFilter = SeverityLevel.Info,
            LogFileFilter = SeverityLevel.Info,
#endif
            FailFastSeverity = FailFastSeverityLevel.Critical,
        };
    }
}
