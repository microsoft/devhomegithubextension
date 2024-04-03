// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Serilog;

namespace GitHubExtension.DataManager;

public class DataUpdater : IDisposable
{
    // This is the default interval the timer will run. It is not the interval that we necessarily do work.
    private static readonly TimeSpan TimerUpdateInterval = TimeSpan.FromSeconds(5);

    private readonly ILogger _logger;
    private readonly PeriodicTimer _timer;
    private readonly Func<Task> _action;
    private CancellationTokenSource _cancelSource;
    private bool _started;

    public bool IsRunning => _started;

    public DataUpdater(TimeSpan interval, Func<Task> action)
    {
        _logger = Log.Logger.ForContext("SourceContext", nameof(DataUpdater));
        _timer = new PeriodicTimer(interval);
        _cancelSource = new CancellationTokenSource();
        _started = false;
        _action = action;
    }

    public DataUpdater(Func<Task> action)
        : this(TimerUpdateInterval, action)
    {
    }

    public async Task Start()
    {
        if (_started)
        {
            // Do nothing if already started.
            return;
        }

        _started = true;
        _cancelSource = new CancellationTokenSource();
        await Task.Run(async () =>
        {
            while (await _timer.WaitForNextTickAsync(_cancelSource.Token))
            {
                await _action();
            }
        });
    }

    public void Stop()
    {
        if (_started)
        {
            _cancelSource.Cancel();
            _started = false;
        }
    }

    public override string ToString() => "DataUpdater";

    private bool disposed; // To detect redundant calls.

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            _logger.Debug("Disposing of all updater resources.");

            if (disposing)
            {
                _timer.Dispose();
            }

            disposed = true;
        }
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
