#nullable enable

using System.Diagnostics;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Runs one child process (a test-side <c>dotnet hc-conformance.dll</c> invocation) and guarantees its
/// lifetime is bounded by the caller's: the whole process tree is killed via
/// <see cref="Process.Kill(bool)"/> on cancellation, on the backstop timeout, and on any exception, not
/// only on a plain timeout. A bare <c>Process.WaitForExit()</c>/<c>ReadToEnd()</c> pair does not observe a
/// <see cref="CancellationToken"/> at all, which is exactly how an NUnit <c>[CancelAfter]</c> test leaves
/// its child running after the test itself is reported cancelled.
/// </summary>
internal static class ChildProcessHarness
{
    /// <summary>
    /// Safety-net ceiling applied whenever a call site supplies no tighter <c>backstop</c>: bounds the
    /// child even for tests with no <c>[CancelAfter]</c> of their own, without changing the timeout
    /// behavior of call sites that already pass one.
    /// </summary>
    private static readonly TimeSpan DefaultBackstop = TimeSpan.FromMinutes(10);

    internal readonly record struct Result(int ExitCode, string StandardOutput, string StandardError);

    /// <summary>
    /// Synchronous front end for callers (most NUnit test methods here) that are not themselves async.
    /// </summary>
    internal static Result Run(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken,
        TimeSpan? backstop = null
    ) => RunAsync(startInfo, cancellationToken, backstop).GetAwaiter().GetResult();

    internal static async Task<Result> RunAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken,
        TimeSpan? backstop = null
    )
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        using var backstopSource = new CancellationTokenSource(backstop ?? DefaultBackstop);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            backstopSource.Token
        );

        using Process process =
            Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start child process '{startInfo.FileName}'.");
        try
        {
            Task<string> stdout = process.StandardOutput.ReadToEndAsync(linked.Token);
            Task<string> stderr = process.StandardError.ReadToEndAsync(linked.Token);
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            return new Result(process.ExitCode, await stdout.ConfigureAwait(false), await stderr.ConfigureAwait(false));
        }
        catch (OperationCanceledException exception)
            when (backstopSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"'{startInfo.FileName} {string.Join(' ', startInfo.ArgumentList)}' exceeded its "
                    + $"{(backstop ?? DefaultBackstop).TotalSeconds:0}-second backstop.",
                exception
            );
        }
        finally
        {
            KillTree(process);
        }
    }

    /// <summary>
    /// Kills the whole process tree if it is still alive, and confirms the kill actually took, so a caller
    /// never reports "done" while a descendant is still exiting.
    /// </summary>
    private static void KillTree(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }
            process.Kill(entireProcessTree: true);
            if (!process.WaitForExit(5_000) || !process.HasExited)
            {
                throw new InvalidOperationException(
                    $"Child process {process.Id} did not exit within 5s of Process.Kill(entireProcessTree: true)."
                );
            }
        }
        catch (InvalidOperationException) when (process.HasExited)
        {
            // Process.Kill throws "no process associated" if the child exited between the HasExited
            // check and the call; that race means the tree is already gone, which is the goal anyway.
        }
    }
}
