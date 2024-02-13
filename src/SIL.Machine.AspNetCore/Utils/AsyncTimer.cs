﻿namespace SIL.Machine.AspNetCore.Utils;

public class AsyncTimer : AsyncDisposableBase
{
#pragma warning disable CA2213 // Disposed in DisposeManagedResources
    private readonly Timer _timer;
#pragma warning restore CA2213
    private readonly Func<Task> _callback;
    private readonly AsyncLock _lock;
    private bool _running;

    public AsyncTimer(Func<Task> callback)
    {
        _callback = callback;
        _lock = new AsyncLock();
        _timer = new Timer(FireTimerAsync, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start(TimeSpan period)
    {
        _running = true;
        _timer.Change(period, period);
    }

    private async void FireTimerAsync(object? state)
    {
        using (await _lock.LockAsync())
        {
            if (_running)
                await _callback();
        }
    }

    public async Task StopAsync()
    {
        using (await _lock.LockAsync())
        {
            // FireTimer is *not* running _callback (since we got the lock)
            StopTimer();
        }
        // Now FireTimer will *never* run _callback
    }

    public void Stop()
    {
        using (_lock.Lock())
        {
            // FireTimer is *not* running _callback (since we got the lock)
            StopTimer();
        }
        // Now FireTimer will *never* run _callback
    }

    private void StopTimer()
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
        _running = false;
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await StopAsync();
        _timer.Dispose();
    }

    protected override void DisposeManagedResources()
    {
        Stop();
        _timer.Dispose();
    }
}
