// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubExtension.Telemetry;

/// <summary>
/// Creates instance of Logger
/// This would be useful for future when we have updated interfaces for logger like ILogger2, ILogger3 and so on
public class LoggerFactory
{
    private static readonly object _lockObj = new();

    private static Logger _loggerInstance;

    private static Logger GetLoggerInstance()
    {
        if (_loggerInstance == null)
        {
            lock (_lockObj)
            {
                _loggerInstance ??= new Logger();
                _loggerInstance.AddWellKnownSensitiveStrings();
            }
        }

        return _loggerInstance;
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
