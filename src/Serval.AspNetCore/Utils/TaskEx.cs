namespace Serval.AspNetCore.Utils;

public static class TaskEx
{
    public static async Task<T?> Timeout<T>(
        Func<CancellationToken, Task<T?>> action,
        TimeSpan timeout,
        CancellationToken cancellationToken = default
    )
    {
        if (timeout == System.Threading.Timeout.InfiniteTimeSpan)
            return await action(cancellationToken);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task<T?> task = action(cts.Token);
        Task<T?> completedTask = await Task.WhenAny(task, Delay<T?>(timeout, cancellationToken));
        if (task != completedTask)
            cts.Cancel();
        return await completedTask;
    }

    public static async Task Timeout(
        Func<CancellationToken, Task> action,
        TimeSpan timeout,
        CancellationToken cancellationToken = default
    )
    {
        if (timeout == System.Threading.Timeout.InfiniteTimeSpan)
        {
            await action(cancellationToken);
        }
        else
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task task = action(cts.Token);
            Task completedTask = await Task.WhenAny(task, Task.Delay(timeout, cancellationToken));
            if (task != completedTask)
                cts.Cancel();
            await completedTask;
        }
    }

    public static async Task<T?> Delay<T>(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        await Task.Delay(timeout, cancellationToken);
        return default;
    }
}
