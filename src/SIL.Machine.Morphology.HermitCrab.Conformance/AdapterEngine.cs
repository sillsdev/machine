using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// Runs a fixture against an external engine adapter process, per the contract in
/// conformance/PROTOCOL.md section 1: <c>&lt;engine-binary&gt; batch &lt;grammar&gt; &lt;words.txt&gt;
/// &lt;output.tsv&gt;</c>. The command template is supplied by the caller (--adapter) with
/// <c>{grammar}</c>/<c>{words}</c>/<c>{output}</c> placeholders, e.g.:
/// <c>"C:\...\hc-rs.exe batch {grammar} {words} {output}"</c>.
/// </summary>
public class AdapterEngine : IEngine
{
    private readonly List<string> _templateTokens;
    private readonly int _timeoutMs;

    public AdapterEngine(string commandTemplate, IReadOnlySet<string> capabilities, int timeoutMs = 5 * 60 * 1000)
    {
        // Tokenize + validate the template once here, not per fixture run: a bad/empty template is
        // a configuration error that should fail fast, not surface 41 times over. Tokenization
        // itself is shared with the Tool project's own command-line parsing (see
        // Program.SplitCommandLine's doc comment) rather than a second, divergent copy.
        _templateTokens = new List<string>(SIL.Machine.Morphology.HermitCrab.Program.SplitCommandLine(commandTemplate));
        if (_templateTokens.Count == 0)
            throw new InvalidOperationException("empty --adapter command template");
        _timeoutMs = timeoutMs;
        Capabilities = capabilities;
    }

    public string Name => "adapter";

    public IReadOnlySet<string> Capabilities { get; }

    public List<TsvRow> Run(MaterializedFixture fixture)
    {
        string outputPath = Path.Combine(Path.GetTempPath(), "hc-conformance-" + Guid.NewGuid().ToString("N") + ".tsv");
        try
        {
            var tokens = new List<string>(_templateTokens.Count);
            foreach (string token in _templateTokens)
            {
                tokens.Add(
                    token
                        .Replace("{grammar}", Path.GetFullPath(fixture.GrammarPath))
                        .Replace("{words}", Path.GetFullPath(fixture.WordsPath))
                        .Replace("{output}", outputPath)
                );
            }

            var psi = new ProcessStartInfo
            {
                FileName = tokens[0],
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            for (int i = 1; i < tokens.Count; i++)
                psi.ArgumentList.Add(tokens[i]);

            using Process process =
                Process.Start(psi)
                ?? throw new InvalidOperationException($"failed to start adapter process '{tokens[0]}'");

            // Start draining both streams asynchronously BEFORE waiting for exit: reading them
            // synchronously (ReadToEnd) first would block forever on a child that fills its
            // stderr/stdout buffer before exiting, making the timeout below unreachable.
            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();

            // A fixture's own manifest budget (pathological fixtures) takes over the process
            // timeout for that one run instead of the default 5 minutes, so a runaway adapter gets
            // killed at the same wall-clock bound MaterializedRunner enforces, not five minutes later.
            int effectiveTimeoutMs =
                fixture.Manifest.Budget != null ? (int)fixture.Manifest.Budget.WallClockMs : _timeoutMs;

            bool exited = process.WaitForExit(effectiveTimeoutMs);
            if (!exited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException) { }
                throw new TimeoutException(
                    $"adapter process for fixture '{fixture.Id}' did not exit within {effectiveTimeoutMs}ms"
                );
            }

            string stdout = stdoutTask.GetAwaiter().GetResult();
            string stderr = stderrTask.GetAwaiter().GetResult();

            if (process.ExitCode != 0)
            {
                // The engine under test itself failed (as opposed to a harness-level problem like
                // a bad --adapter template, a hung process, or exiting 0 with no output) -- see
                // EngineCrashException's doc comment for the PASS/FAIL contract this feeds MaterializedRunner.
                throw new EngineCrashException(
                    $"adapter process exited {process.ExitCode} for fixture '{fixture.Id}'.\nstdout:\n{stdout}\nstderr:\n{stderr}"
                );
            }
            if (!File.Exists(outputPath))
            {
                throw new InvalidOperationException(
                    $"adapter process for fixture '{fixture.Id}' did not produce its output file.\nstdout:\n{stdout}\nstderr:\n{stderr}"
                );
            }
            return SignatureTsv.ReadFile(outputPath);
        }
        finally
        {
            try
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
            catch (IOException) { }
        }
    }
}
