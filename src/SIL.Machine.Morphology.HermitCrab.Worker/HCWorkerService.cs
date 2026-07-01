using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using SIL.Machine.Annotations;

namespace SIL.Machine.Morphology.HermitCrab.Worker
{
    /// <summary>
    /// Hosts one Morpher for the lifetime of the worker process. One instance is shared across
    /// all WCF calls (InstanceContextMode.Single) and calls are allowed to run concurrently
    /// (ConcurrencyMode.Multiple) - Morpher.ParseWord is already called this way in-process today
    /// (ParserWorker.ParseAndUpdateWordforms' Parallel.ForEach over wordforms, each iteration
    /// calling HCParser.ParseWord -> Morpher.ParseWord with no external locking), so no new
    /// thread-safety requirement is introduced by moving it out-of-process.
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class HCWorkerService : IHCWorkerService
    {
        private volatile Morpher _morpher;

        public void UpdateGrammar(HCGrammarDto grammar)
        {
            if (grammar == null)
                throw new ArgumentNullException(nameof(grammar));

            // XmlLanguageLoader.Load only takes a file path (no changes to
            // SIL.Machine.Morphology.HermitCrab needed - see design §3/§9), so round-trip the
            // grammar XML through a temp file rather than adding a string/stream overload there.
            string tempPath = Path.Combine(Path.GetTempPath(), $"hcworker-grammar-{Guid.NewGuid():N}.xml");
            try
            {
                File.WriteAllText(tempPath, grammar.CompiledGrammarXml);
                Language language = XmlLanguageLoader.Load(tempPath);
                var morpher = new Morpher(new TraceManager(), language)
                {
                    DeletionReapplications = grammar.DeletionReapplications,
                    MaxStemCount = grammar.MaxStemCount,
                    MergeEquivalentAnalyses = grammar.MergeEquivalentAnalyses
                };
                // Benchmark-only knob (HCWORKER_MAX_UNAPPLICATIONS env var, not part of the wire
                // contract - FieldWorks never sets this today; see HCParser.LoadParser). Lets the
                // integration bench harness reproduce the MaxUnapplications=5 setting the
                // RustifyBenchmark/CompareBench in-process Sena/Indonesian benchmarks use, for
                // apples-to-apples analysis counts against this session's earlier numbers.
                if (int.TryParse(Environment.GetEnvironmentVariable("HCWORKER_MAX_UNAPPLICATIONS"), out int maxUnapp))
                    morpher.MaxUnapplications = maxUnapp;
                _morpher = morpher;
            }
            finally
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (IOException)
                {
                    // best-effort cleanup; a stray temp file is not worth failing the grammar update over
                }
            }
        }

        public WordAnalysisDto[] ParseWord(string word, bool guessRoots)
        {
            Morpher morpher = RequireMorpher();
            return morpher.ParseWord(word, out _, guessRoots).Select(ToWordAnalysisDto).ToArray();
        }

        public IDictionary<string, WordAnalysisDto[]> ParseWordsBatch(string[] words, bool guessRoots)
        {
            Morpher morpher = RequireMorpher();
            var results = new ConcurrentDictionary<string, WordAnalysisDto[]>();
            // Mirrors ParserWorker.ParseAndUpdateWordforms' parallel path (Src\LexText\ParserCore\
            // ParserWorker.cs), moved server-side and uncapped by default: that cap existed only
            // to keep FieldWorks' own UI thread responsive under Workstation GC, which no longer
            // applies once parsing lives in this process (design §1/§2). HCWORKER_MAX_DOP
            // (benchmark-only, not part of the wire contract) lets the integration bench harness
            // sweep thread counts against the real worker process instead of always maxing out.
            int maxDop = int.TryParse(Environment.GetEnvironmentVariable("HCWORKER_MAX_DOP"), out int dop) && dop > 0
                ? dop
                : Environment.ProcessorCount;
            Parallel.ForEach(
                words,
                new ParallelOptions { MaxDegreeOfParallelism = maxDop },
                word =>
                {
                    try
                    {
                        results[word] = morpher
                            .ParseWord(word, out _, guessRoots)
                            .Select(ToWordAnalysisDto)
                            .ToArray();
                    }
                    catch (Exception)
                    {
                        // Guard each word so one unexpected exception (e.g. an out-of-vocabulary
                        // character - CharacterDefinitionTable.Segment throws InvalidShapeException
                        // for those) cannot abort the whole batch, mirroring ParserWorker.
                        // ParseAndUpdateWordformGuarded's identical guard around the equivalent
                        // in-process per-wordform Parallel.ForEach body (Src\LexText\ParserCore\
                        // ParserWorker.cs) - this call replaces that loop, so it must replace its
                        // fault isolation too. HCParser.ParseWord's single-word path already wraps
                        // the same call in a try/catch for the same reason.
                        results[word] = Array.Empty<WordAnalysisDto>();
                    }
                }
            );
            // Return a plain Dictionary rather than the ConcurrentDictionary built above -
            // DataContractSerializer's IDictionary<TKey,TValue> support is defined in terms of
            // the concrete Dictionary<TKey,TValue> shape (KeyValuePair-array roundtrip); no need
            // to rely on ConcurrentDictionary also matching that.
            return new Dictionary<string, WordAnalysisDto[]>(results);
        }

        private Morpher RequireMorpher()
        {
            Morpher morpher = _morpher;
            if (morpher == null)
                throw new InvalidOperationException("UpdateGrammar must be called before parsing.");
            return morpher;
        }

        /// <summary>
        /// The ID-collection half of HCParser.GetMorphs (Src\LexText\ParserCore\HCParser.cs),
        /// ported to run here where the Word/Allomorph/Morpheme object graph lives, leaving the
        /// LCM-object-resolution half (IMoForm/IMoMorphSynAnalysis/ILexEntryInflType lookups,
        /// circumfix/infix placement, which need a live LcmCache) for FieldWorks to run over the
        /// resulting MorphDto[] - see the MorphDto doc comment.
        /// </summary>
        internal static WordAnalysisDto ToWordAnalysisDto(Word ws)
        {
            var morphemeIndices = new Dictionary<Morpheme, int>();
            var morphs = new List<MorphDto>();
            foreach (Annotation<ShapeNode> morph in ws.Morphs)
            {
                Allomorph allomorph = ws.GetAllomorph(morph);
                int formId = ParseNullableIntProperty(allomorph.Properties, HCWorkerConstants.FormId) ?? 0;
                if (formId == 0)
                    continue;

                if (!morphemeIndices.TryGetValue(allomorph.Morpheme, out int morphemeIndex))
                {
                    morphemeIndex = morphemeIndices.Count;
                    morphemeIndices[allomorph.Morpheme] = morphemeIndex;
                }

                string formStr = ws.Shape.GetNodes(morph.Range).ToString(ws.Stratum.CharacterDefinitionTable, false);
                morphs.Add(
                    new MorphDto
                    {
                        FormId = formId,
                        FormId2 = ParseNullableIntProperty(allomorph.Properties, HCWorkerConstants.FormId2) ?? 0,
                        IsAffixProcessAllomorph = allomorph is MorphologicalRules.AffixProcessAllomorph,
                        FormStr = formStr,
                        Guessed = allomorph.Guessed,
                        MsaId = ParseIntProperty(allomorph.Morpheme.Properties, HCWorkerConstants.MsaId),
                        InflTypeId = ParseNullableIntProperty(
                            allomorph.Morpheme.Properties,
                            HCWorkerConstants.InflTypeId
                        ) ?? 0,
                        MorphemeIndex = morphemeIndex
                    }
                );
            }
            return new WordAnalysisDto { Morphs = morphs.ToArray() };
        }

        private static int ParseIntProperty(IDictionary<string, object> properties, string key)
        {
            // Properties round-trip through XmlLanguageWriter/XmlLanguageLoader as strings
            // (WriteProperties/LoadProperties serialize via ToString(), see
            // XmlLanguageWriter.cs/XmlLanguageLoader.cs) even though FieldWorks originally stored
            // them as ints (HCLoader.cs: hcEntry.Properties[HCParser.MsaID] = msa.Hvo), so parse
            // rather than unbox here.
            if (!properties.TryGetValue(key, out object value) || value == null)
                throw new InvalidOperationException($"Morpheme is missing required property '{key}'.");
            return int.Parse(value.ToString());
        }

        private static int? ParseNullableIntProperty(IDictionary<string, object> properties, string key)
        {
            if (!properties.TryGetValue(key, out object value) || value == null)
                return null;
            return int.Parse(value.ToString());
        }
    }

    /// <summary>
    /// Property-bag key strings that must match HCParser.cs's FormID/FormID2/MsaID/InflTypeID
    /// constants exactly (Src\LexText\ParserCore\HCParser.cs) - these are the keys FieldWorks'
    /// HCLoader.cs writes into the grammar's Allomorph/Morpheme Properties bags, which then
    /// survive the XmlLanguageWriter/XmlLanguageLoader round trip generically (see
    /// WriteProperties/LoadProperties).
    /// </summary>
    internal static class HCWorkerConstants
    {
        public const string FormId = "ID";
        public const string FormId2 = "ID2";
        public const string MsaId = "ID";
        public const string InflTypeId = "InflTypeID";
    }
}
