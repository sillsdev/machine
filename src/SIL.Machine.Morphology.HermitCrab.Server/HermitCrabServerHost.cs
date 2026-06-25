using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// The worker side: loads a compiled HermitCrab grammar and serves analyze requests over
    /// stdin/stdout (newline-delimited JSON). Each parse uses a single-threaded morpher; a batch
    /// is parsed with parallelism ACROSS words. Intended to run in its own process with Server GC
    /// so the host application's GC is unaffected.
    /// </summary>
    public static class HermitCrabServerHost
    {
        public static void Run(string configPath, int maxDegreeOfParallelism)
        {
            if (configPath == null)
                throw new ArgumentNullException(nameof(configPath));

            // Parse each word single-threaded; parallelism is across words within a batch.
            Language language = XmlLanguageLoader.Load(configPath);
            var morpher = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1);

            int dop = maxDegreeOfParallelism > 0 ? maxDegreeOfParallelism : Environment.ProcessorCount;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = dop };

            var utf8 = new UTF8Encoding(false);
            using var input = new StreamReader(Console.OpenStandardInput(), utf8);
            using var output = new StreamWriter(Console.OpenStandardOutput(), utf8) { AutoFlush = false };

            // Signal readiness (and report the worker's GC mode) so the client can wait for grammar
            // load to finish and verify Server GC took effect.
            output.WriteLine("READY " + (System.Runtime.GCSettings.IsServerGC ? "Server" : "Workstation"));
            output.Flush();

            string? line;
            while ((line = input.ReadLine()) != null)
            {
                if (line.Length == 0)
                    continue;

                HermitCrabAnalyzeRequest? request = JsonSerializer.Deserialize<HermitCrabAnalyzeRequest>(line);
                if (request == null)
                {
                    output.WriteLine(JsonSerializer.Serialize(new HermitCrabAnalyzeResponse()));
                    output.Flush();
                    continue;
                }
                var results = new HermitCrabWordResult[request.Words.Count];
                Parallel.For(
                    0,
                    request.Words.Count,
                    parallelOptions,
                    i =>
                    {
                        results[i] = Analyze(morpher, request.Words[i]);
                    }
                );

                var response = new HermitCrabAnalyzeResponse { Results = results.ToList() };
                output.WriteLine(JsonSerializer.Serialize(response));
                output.Flush();
            }
        }

        private static HermitCrabWordResult Analyze(Morpher morpher, string word)
        {
            var result = new HermitCrabWordResult { Word = word };
            try
            {
                foreach (WordAnalysis analysis in morpher.AnalyzeWord(word))
                {
                    result.Analyses.Add(
                        new HermitCrabAnalysisDto
                        {
                            Morphemes = analysis
                                .Morphemes.Select(m => new HermitCrabMorphemeDto
                                {
                                    Id = m.Id,
                                    Category = m.Category,
                                    Gloss = m.Gloss,
                                    MorphemeType = m.MorphemeType,
                                })
                                .ToList(),
                            RootMorphemeIndex = analysis.RootMorphemeIndex,
                            Category = analysis.Category,
                        }
                    );
                }
            }
            catch (InvalidShapeException ise)
            {
                result.Error = $"invalid segment at position {ise.Position + 1}";
            }
            return result;
        }
    }
}
