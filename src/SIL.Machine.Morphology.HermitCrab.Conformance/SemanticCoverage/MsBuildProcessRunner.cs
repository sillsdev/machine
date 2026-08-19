#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal sealed record ProcessCapture(int ExitCode, byte[] StandardOutput, string StandardError);

internal interface IMsBuildProcessRunner
{
    ValueTask<ProcessCapture> RunAsync(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        int maxStandardOutputBytes,
        CancellationToken cancellationToken);
}

internal sealed class MsBuildProcessRunner : IMsBuildProcessRunner
{
    public async ValueTask<ProcessCapture> RunAsync(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        int maxStandardOutputBytes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        if (startInfo.UseShellExecute || !startInfo.RedirectStandardOutput || !startInfo.RedirectStandardError)
            throw new ArgumentException("MSBuild must run shell-free with redirected output.", nameof(startInfo));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxStandardOutputBytes);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
                throw new InvalidDataException("MSBuild process did not start.");
        }
        catch (Exception exception) when (exception is not InvalidDataException)
        {
            throw new InvalidDataException("MSBuild process could not be started.", exception);
        }

        Task<byte[]> stdout = ReadBoundedAsync(
            process.StandardOutput.BaseStream,
            maxStandardOutputBytes,
            cancellationToken);
        Task<string> stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        Task exit = process.WaitForExitAsync(timeoutSource.Token);
        try
        {
            Task completed = await Task.WhenAny(exit, stdout).ConfigureAwait(false);
            if (completed == stdout)
            {
                await stdout.ConfigureAwait(false);
                await exit.ConfigureAwait(false);
            }
            else
            {
                await exit.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            TryTerminate(process);
            throw new InvalidDataException($"MSBuild exceeded the {timeout.TotalSeconds:0}-second timeout.", exception);
        }
        catch
        {
            TryTerminate(process);
            throw;
        }

        byte[] output;
        try
        {
            output = await stdout.ConfigureAwait(false);
        }
        catch
        {
            TryTerminate(process);
            throw;
        }
        return new ProcessCapture(process.ExitCode, output, await stderr.ConfigureAwait(false));
    }

    private static async Task<byte[]> ReadBoundedAsync(
        Stream stream,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        using var output = new MemoryStream();
        byte[] buffer = new byte[81920];
        while (true)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            if (read == 0)
                return output.ToArray();
            if (output.Length + read > maximumBytes)
                throw new InvalidDataException($"MSBuild standard output exceeded {maximumBytes} bytes.");
            output.Write(buffer, 0, read);
        }
    }

    private static void TryTerminate(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            if (!process.WaitForExit(5000) || !process.HasExited)
                throw new InvalidDataException("MSBuild process-tree termination could not be confirmed.");
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidDataException("MSBuild process-tree termination could not be confirmed.", exception);
        }
    }
}
