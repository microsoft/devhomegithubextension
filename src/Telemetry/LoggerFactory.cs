﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace GitHubExtension.Telemetry;

/// <summary>
/// Creates instance of Logger
/// This would be useful for future when we have updated interfaces for logger like ILogger2, ILogger3 and so on
public class LoggerFactory
{
    private static readonly object LockObj = new ();

    private static Logger loggerInstance;

    private static Logger GetLoggerInstance()
    {
        if (loggerInstance == null)
        {
            lock (LockObj)
            {
                loggerInstance ??= new Logger();
                loggerInstance.AddWellKnownSensitiveStrings();
            }
        }

        return loggerInstance;
    }

    /// <summary>
    /// Gets a singleton instance of Logger
    /// This would be useful for future when we have updated interfaces for logger like ILogger2, ILogger3 and so on
    public static T Get<T>()
        where T : ILogger
    {
        return (T)(object)GetLoggerInstance();
    }
}
