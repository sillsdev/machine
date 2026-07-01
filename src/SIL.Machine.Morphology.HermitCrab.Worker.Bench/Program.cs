using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace SIL.Machine.Morphology.HermitCrab.Worker.Bench
{
    /// <summary>
    /// Integration benchmark for RUSTIFY-fieldworks-worker-design.md: drives the REAL out-of-
    /// process worker (spawns SIL.Machine.Morphology.HermitCrab.Worker.exe, a real net.pipe WCF
    /// channel, the worker's real Server GC) against the Sena/Indonesian grammar+wordlist pairs
    /// in samples/data/, sweeping thread counts, and reports timing. Unlike the in-process
    /// RustifyBenchmark.SenaParallel (single process, Parallel.ForEach over a shared in-proc
    /// Morpher under Workstation GC), this exercises the whole pipeline this session built:
    /// process spawn, IPC, grammar transfer, and the worker's own internal parallelism.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            Options options;
            try
            {
                options = Options.Parse(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(Options.Usage);
                return 1;
            }

            string repoRoot = FindRepoRoot();
            string samplesDir = options.SamplesDir ?? Path.Combine(repoRoot, "samples", "data");
            string workerExe = options.WorkerExe ?? FindWorkerExe(repoRoot);

            Console.WriteLine($"worker exe: {workerExe}");
            Console.WriteLine($"samples dir: {samplesDir}");
            Console.WriteLine(
                $"grammars={string.Join(",", options.Grammars)} threads={string.Join(",", options.Threads)} "
                    + $"maxWords={options.MaxWords} maxUnapplications={options.MaxUnapplications} "
                    + $"guessRoots={options.GuessRoots} repeat={options.Repeat}"
            );
            Console.WriteLine();

            var allResults = new List<RunResult>();
            foreach (string grammarName in options.Grammars)
            {
                string grammarPath = Path.Combine(samplesDir, $"{grammarName}-hc.xml");
                string wordsPath = Path.Combine(samplesDir, $"{grammarName}-words.txt");
                if (!File.Exists(grammarPath) || !File.Exists(wordsPath))
                {
                    Console.Error.WriteLine($"skipping '{grammarName}': expected {grammarPath} and {wordsPath}");
                    continue;
                }

                string grammarXml = File.ReadAllText(grammarPath);
                List<string> words = ReadWords(wordsPath, options.MaxWords);
                Console.WriteLine($"== {grammarName} ({words.Count} words) ==");

                double baseWallMs = 0;
                foreach (int dop in options.Threads)
                {
                    var runTimes = new List<double>(options.Repeat);
                    int analysesFound = 0;
                    for (int rep = 0; rep < options.Repeat; rep++)
                    {
                        RunOutcome outcome = RunOnce(workerExe, grammarXml, words, dop, options);
                        runTimes.Add(outcome.WallMs);
                        analysesFound = outcome.AnalysesFound;
                    }
                    double wallMs = runTimes.Average();
                    if (dop == options.Threads[0])
                        baseWallMs = wallMs;
                    double wordsPerSec = words.Count / (wallMs / 1000.0);
                    double speedup = baseWallMs / wallMs;
                    var result = new RunResult
                    {
                        Grammar = grammarName,
                        Threads = dop,
                        WallMs = wallMs,
                        WordsPerSec = wordsPerSec,
                        Speedup = speedup,
                        AnalysesFound = analysesFound
                    };
                    allResults.Add(result);
                    Console.WriteLine(
                        $"  threads={dop,2} wallMs={wallMs,9:F0} words/sec={wordsPerSec,8:F0} "
                            + $"speedup={speedup,5:F2}x analyses={analysesFound}"
                    );
                }
                Console.WriteLine();
            }

            string report = BuildMarkdownReport(allResults, options);
            if (options.ReportPath != null)
            {
                File.WriteAllText(options.ReportPath, report);
                Console.WriteLine($"report written to {options.ReportPath}");
            }
            return 0;
        }

        private static List<string> ReadWords(string path, int maxWords)
        {
            var words = new List<string>();
            foreach (string line in File.ReadLines(path))
            {
                string w = line.Trim();
                if (w.Length == 0)
                    continue;
                words.Add(w);
                if (maxWords > 0 && words.Count >= maxWords)
                    break;
            }
            return words;
        }

        private class RunOutcome
        {
            public double WallMs;
            public int AnalysesFound;
        }

        private static RunOutcome RunOnce(string workerExe, string grammarXml, List<string> words, int dop, Options options)
        {
            string pipeName = "hcworker-bench-" + Guid.NewGuid().ToString("N");
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = workerExe,
                Arguments = $"{pipeName} {Process.GetCurrentProcess().Id}",
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            startInfo.EnvironmentVariables["HCWORKER_MAX_DOP"] = dop.ToString();
            startInfo.EnvironmentVariables["HCWORKER_MAX_UNAPPLICATIONS"] = options.MaxUnapplications.ToString();

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                try
                {
                    WaitForReady(process, TimeSpan.FromSeconds(30));

                    NetNamedPipeBinding pipeBinding = PipeBindingFactory.Create();

                    var factory = new ChannelFactory<IHCWorkerService>(
                        pipeBinding,
                        new EndpointAddress("net.pipe://localhost/" + pipeName)
                    );
                    IHCWorkerService client = factory.CreateChannel();

                    // Grammar load/JIT warmup is a once-per-session cost in real usage (design
                    // §4), not part of the per-batch parse cost this benchmark measures - so it
                    // is deliberately excluded from the timed region below.
                    client.UpdateGrammar(
                        new HCGrammarDto
                        {
                            CompiledGrammarXml = grammarXml,
                            DeletionReapplications = 0,
                            MaxStemCount = 2,
                            MergeEquivalentAnalyses = false
                        }
                    );

                    var sw = Stopwatch.StartNew();
                    IDictionary<string, WordAnalysisDto[]> results = client.ParseWordsBatch(
                        words.ToArray(),
                        options.GuessRoots
                    );
                    sw.Stop();

                    ((IClientChannel)client).Close();
                    factory.Close();

                    int analysesFound = results.Values.Sum(v => v.Length);
                    return new RunOutcome { WallMs = sw.Elapsed.TotalMilliseconds, AnalysesFound = analysesFound };
                }
                finally
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill();
                    }
                    catch (Exception)
                    {
                        // best-effort cleanup
                    }
                }
            }
        }

        private static void WaitForReady(Process process, TimeSpan timeout)
        {
            var readTask = process.StandardOutput.ReadLineAsync();
            if (!readTask.Wait(timeout))
                throw new TimeoutException("Worker process did not signal READY in time.");
            if (readTask.Result != "READY")
                throw new InvalidOperationException($"Worker process printed unexpected startup line: '{readTask.Result}'");
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Machine.sln")))
                dir = dir.Parent;
            if (dir == null)
                throw new InvalidOperationException("Could not locate repo root (no Machine.sln found above " + AppContext.BaseDirectory + ").");
            return dir.FullName;
        }

        private static string FindWorkerExe(string repoRoot)
        {
            string projectDir = Path.Combine(repoRoot, "src", "SIL.Machine.Morphology.HermitCrab.Worker");
            foreach (string config in new[] { "Debug", "Release" })
            {
                string candidate = Path.Combine(projectDir, "bin", config, "net48", "SIL.Machine.Morphology.HermitCrab.Worker.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
            throw new InvalidOperationException(
                "Could not find the built worker exe under " + projectDir
                    + @"\bin\{Debug,Release}\net48 - build src\SIL.Machine.Morphology.HermitCrab.Worker first."
            );
        }

        private static string BuildMarkdownReport(List<RunResult> results, Options options)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# HermitCrab out-of-process worker benchmark");
            sb.AppendLine();
            sb.AppendLine(
                $"maxWords={options.MaxWords}, maxUnapplications={options.MaxUnapplications}, "
                    + $"guessRoots={options.GuessRoots}, repeat={options.Repeat}"
            );
            sb.AppendLine();
            foreach (var group in results.GroupBy(r => r.Grammar))
            {
                sb.AppendLine($"## {group.Key}");
                sb.AppendLine();
                sb.AppendLine("| Threads | Wall ms | Words/sec | Speedup vs 1st | Analyses found |");
                sb.AppendLine("|---:|---:|---:|---:|---:|");
                foreach (RunResult r in group)
                {
                    sb.AppendLine(
                        $"| {r.Threads} | {r.WallMs:F0} | {r.WordsPerSec:F0} | {r.Speedup:F2}x | {r.AnalysesFound} |"
                    );
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private class RunResult
        {
            public string Grammar;
            public int Threads;
            public double WallMs;
            public double WordsPerSec;
            public double Speedup;
            public int AnalysesFound;
        }

        private class Options
        {
            public List<string> Grammars = new List<string> { "sena", "indonesian" };
            public List<int> Threads = new List<int> { 1, 2, 4, 8, 16 };
            public int MaxWords = 800;
            public int MaxUnapplications = 5;
            public bool GuessRoots = false;
            public int Repeat = 1;
            public string ReportPath;
            public string WorkerExe;
            public string SamplesDir;

            public const string Usage =
                "Usage: SIL.Machine.Morphology.HermitCrab.Worker.Bench.exe "
                    + "[--grammars sena,indonesian] [--threads 1,2,4,8,16] [--max-words N] "
                    + "[--max-unapplications N] [--guess-roots true|false] [--repeat N] "
                    + "[--report <path>] [--worker-exe <path>] [--samples-dir <path>]";

            public static Options Parse(string[] args)
            {
                var options = new Options();
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    string Next() =>
                        ++i < args.Length ? args[i] : throw new ArgumentException($"missing value for {arg}");
                    switch (arg)
                    {
                        case "--grammars":
                            options.Grammars = Next().Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
                            break;
                        case "--threads":
                            options.Threads = Next().Split(',').Select(s => int.Parse(s.Trim())).ToList();
                            break;
                        case "--max-words":
                            options.MaxWords = int.Parse(Next());
                            break;
                        case "--max-unapplications":
                            options.MaxUnapplications = int.Parse(Next());
                            break;
                        case "--guess-roots":
                            options.GuessRoots = bool.Parse(Next());
                            break;
                        case "--repeat":
                            options.Repeat = int.Parse(Next());
                            break;
                        case "--report":
                            options.ReportPath = Next();
                            break;
                        case "--worker-exe":
                            options.WorkerExe = Next();
                            break;
                        case "--samples-dir":
                            options.SamplesDir = Next();
                            break;
                        case "-h":
                        case "--help":
                            Console.WriteLine(Usage);
                            Environment.Exit(0);
                            break;
                        default:
                            throw new ArgumentException($"unrecognized argument: {arg}");
                    }
                }
                return options;
            }
        }
    }
}
