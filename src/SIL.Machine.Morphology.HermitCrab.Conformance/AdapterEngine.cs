using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

public class AdapterEngine : IEngine
{
    private readonly List<string> _templateTokens;
    private readonly int _timeoutMs;

    public AdapterEngine(string commandTemplate, IReadOnlySet<string> capabilities, int timeoutMs = 5 * 60 * 1000)
    {
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

            var psi = SemanticCoverage.ChildProcessEnvironment.CreateStartInfo(tokens[0]);
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            for (int i = 1; i < tokens.Count; i++)
                psi.ArgumentList.Add(tokens[i]);

            using Process process =
                Process.Start(psi)
                ?? throw new InvalidOperationException($"failed to start adapter process '{tokens[0]}'");

            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();

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
