using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Drives a HermitCrab parser running in a separate <see cref="HermitCrabServerHost"/> process.
    /// The worker process is launched with Server GC so the parse allocation churn is collected in
    /// its own heap, leaving the host application's GC untouched. Implements the standard
    /// <see cref="IMorphologicalAnalyzer"/> contract, so it is a drop-in for an in-process Morpher.
    /// Reusable by any consumer that has a compiled HermitCrab config (e.g. produced by FieldWorks'
    /// GenerateHCConfig).
    /// </summary>
    public sealed class HermitCrabServerClient : IMorphologicalAnalyzer, IDisposable
    {
        private readonly Process _process;
        private readonly object _sync = new object();
        private bool _disposed;

        /// <param name="configPath">Path to the compiled HermitCrab grammar (XML).</param>
        /// <param name="maxDegreeOfParallelism">
        /// How many words the worker parses concurrently (across-word parallelism). Defaults to the
        /// worker's processor count. Each individual parse is single-threaded.
        /// </param>
        /// <param name="serverGarbageCollection">
        /// Launch the worker with Server GC (default true) — the whole point of running out of process.
        /// </param>
        /// <param name="workerAssemblyPath">
        /// Path to the worker assembly (this Server assembly). Defaults to this assembly's own
        /// location; override it when the running copy lacks a runtimeconfig.json (e.g. when this
        /// assembly was copied into another project's output).
        /// </param>
        public HermitCrabServerClient(
            string configPath,
            int? maxDegreeOfParallelism = null,
            bool serverGarbageCollection = true,
            string? workerAssemblyPath = null
        )
        {
            if (configPath == null)
                throw new ArgumentNullException(nameof(configPath));

            _process = StartWorker(
                configPath,
                maxDegreeOfParallelism,
                serverGarbageCollection,
                workerAssemblyPath ?? typeof(HermitCrabServerClient).Assembly.Location
            );

            // Wait for the worker to finish loading the grammar; it reports its GC mode.
            string? ready = _process.StandardOutput.ReadLine();
            if (ready == null || !ready.StartsWith("READY", StringComparison.Ordinal))
                throw new InvalidOperationException("HermitCrab worker failed to start: " + (ready ?? "<no output>"));
            WorkerGarbageCollectorMode = ready.Length > 6 ? ready.Substring(6) : "unknown";
        }

        /// <summary>The GC mode the worker process actually started with ("Server" / "Workstation").</summary>
        public string WorkerGarbageCollectorMode { get; }

        public IEnumerable<WordAnalysis> AnalyzeWord(string word)
        {
            return AnalyzeWords(new[] { word })[0];
        }

        /// <summary>
        /// Analyzes a batch of words in one round-trip; the worker parses them concurrently. This is
        /// the efficient entry point for corpus parsing.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<WordAnalysis>> AnalyzeWords(IReadOnlyList<string> words)
        {
            var request = new HermitCrabAnalyzeRequest { Words = words.ToList() };
            HermitCrabAnalyzeResponse response;
            lock (_sync)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(HermitCrabServerClient));
                _process.StandardInput.WriteLine(JsonSerializer.Serialize(request));
                _process.StandardInput.Flush();
                string? line = _process.StandardOutput.ReadLine();
                if (line == null)
                    throw new InvalidOperationException("HermitCrab worker terminated unexpectedly.");
                response =
                    JsonSerializer.Deserialize<HermitCrabAnalyzeResponse>(line)
                    ?? throw new InvalidOperationException("HermitCrab worker returned a malformed response: " + line);
            }

            var results = new IReadOnlyList<WordAnalysis>[response.Results.Count];
            for (int i = 0; i < response.Results.Count; i++)
            {
                HermitCrabWordResult wordResult = response.Results[i];
                var analyses = new List<WordAnalysis>(wordResult.Analyses.Count);
                foreach (HermitCrabAnalysisDto dto in wordResult.Analyses)
                {
                    // The morpheme DTOs already implement IMorpheme, so hand them straight to WordAnalysis.
                    analyses.Add(new WordAnalysis(dto.Morphemes, dto.RootMorphemeIndex, dto.Category));
                }
                results[i] = analyses;
            }
            return results;
        }

        private static Process StartWorker(string configPath, int? maxDop, bool serverGc, string workerAssembly)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                StandardInputEncoding = new UTF8Encoding(false),
                StandardOutputEncoding = new UTF8Encoding(false),
            };
            psi.ArgumentList.Add(workerAssembly);
            psi.ArgumentList.Add("--serve");
            psi.ArgumentList.Add("--config");
            psi.ArgumentList.Add(configPath);
            if (maxDop.HasValue)
            {
                psi.ArgumentList.Add("--max-dop");
                psi.ArgumentList.Add(maxDop.Value.ToString());
            }
            // Server GC for the worker only — inherited by the child, not the host process.
            psi.Environment["DOTNET_gcServer"] = serverGc ? "1" : "0";

            Process? process = Process.Start(psi);
            if (process == null)
                throw new InvalidOperationException("Failed to start the HermitCrab worker process.");
            return process;
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;
                _disposed = true;
            }
            try
            {
                if (!_process.HasExited)
                {
                    _process.StandardInput.Close(); // EOF -> worker loop ends
                    if (!_process.WaitForExit(2000))
                        _process.Kill();
                }
            }
            catch
            {
                // best effort
            }
            _process.Dispose();
        }
    }
}
