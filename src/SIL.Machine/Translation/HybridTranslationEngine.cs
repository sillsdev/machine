using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Tokenization;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
    public class HybridTranslationEngine : DisposableBase, IInteractiveTranslationEngine
    {
        internal const double InteractiveEngineThreshold = 0.05;

        public HybridTranslationEngine(
            IInteractiveTranslationEngine interactiveEngine,
            ITranslationEngine ruleEngine = null
        )
        {
            InteractiveEngine = interactiveEngine;
            RuleEngine = ruleEngine;
        }

        public IInteractiveTranslationEngine InteractiveEngine { get; }
        public ITranslationEngine RuleEngine { get; }
        public IDetokenizer<string, string> TargetDetokenizer { get; set; } = WhitespaceDetokenizer.Instance;

        public async Task<TranslationResult> TranslateAsync(
            string segment,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            TranslationResult result = await InteractiveEngine
                .TranslateAsync(segment, cancellationToken)
                .ConfigureAwait(false);
            if (RuleEngine == null)
                return result;

            TranslationResult ruleResult = await RuleEngine
                .TranslateAsync(segment, cancellationToken)
                .ConfigureAwait(false);
            return Merge(result, ruleResult);
        }

        public async Task<TranslationResult> TranslateAsync(
            IReadOnlyList<string> segment,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            TranslationResult result = await InteractiveEngine
                .TranslateAsync(segment, cancellationToken)
                .ConfigureAwait(false);
            if (RuleEngine == null)
                return result;

            TranslationResult ruleResult = await RuleEngine
                .TranslateAsync(segment, cancellationToken)
                .ConfigureAwait(false);
            return Merge(result, ruleResult);
        }

        public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(
            int n,
            string segment,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            IReadOnlyList<TranslationResult> hypotheses = await InteractiveEngine
                .TranslateAsync(n, segment, cancellationToken)
                .ConfigureAwait(false);
            if (RuleEngine == null || hypotheses.Count == 0)
                return hypotheses;

            TranslationResult ruleResult = await RuleEngine
                .TranslateAsync(segment, cancellationToken)
                .ConfigureAwait(false);
            return hypotheses.Select(hypothesis => Merge(hypothesis, ruleResult)).ToArray();
        }

        public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(
            int n,
            IReadOnlyList<string> segment,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            IReadOnlyList<TranslationResult> hypotheses = await InteractiveEngine
                .TranslateAsync(n, segment, cancellationToken)
                .ConfigureAwait(false);
            if (RuleEngine == null || hypotheses.Count == 0)
                return hypotheses;

            TranslationResult ruleResult = await RuleEngine
                .TranslateAsync(segment, cancellationToken)
                .ConfigureAwait(false);
            return hypotheses.Select(hypothesis => Merge(hypothesis, ruleResult)).ToArray();
        }

        public async Task<IReadOnlyList<TranslationResult>> TranslateBatchAsync(
            IReadOnlyList<string> segments,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            IReadOnlyList<TranslationResult> results = await InteractiveEngine
                .TranslateBatchAsync(segments, cancellationToken)
                .ConfigureAwait(false);
            if (RuleEngine == null)
                return results;

            IReadOnlyList<TranslationResult> ruleResults = await RuleEngine
                .TranslateBatchAsync(segments, cancellationToken)
                .ConfigureAwait(false);
            return results.Zip(ruleResults, (result, ruleResult) => Merge(result, ruleResult)).ToArray();
        }

        public async Task<IReadOnlyList<TranslationResult>> TranslateBatchAsync(
            IReadOnlyList<IReadOnlyList<string>> segments,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            IReadOnlyList<TranslationResult> results = await InteractiveEngine
                .TranslateBatchAsync(segments, cancellationToken)
                .ConfigureAwait(false);
            if (RuleEngine == null)
                return results;

            IReadOnlyList<TranslationResult> ruleResults = await RuleEngine
                .TranslateBatchAsync(segments, cancellationToken)
                .ConfigureAwait(false);
            return results.Zip(ruleResults, (result, ruleResult) => Merge(result, ruleResult)).ToArray();
        }

        public async Task<IReadOnlyList<IReadOnlyList<TranslationResult>>> TranslateBatchAsync(
            int n,
            IReadOnlyList<string> segments,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            IReadOnlyList<IReadOnlyList<TranslationResult>> results = await InteractiveEngine
                .TranslateBatchAsync(n, segments, cancellationToken)
                .ConfigureAwait(false);
            if (RuleEngine == null)
                return results;

            IReadOnlyList<TranslationResult> ruleResults = await RuleEngine
                .TranslateBatchAsync(segments, cancellationToken)
                .ConfigureAwait(false);
            return results
                .Zip(
                    ruleResults,
                    (hypotheses, ruleResult) => hypotheses.Select(hypothesis => Merge(hypothesis, ruleResult)).ToArray()
                )
                .ToArray();
        }

        public async Task<IReadOnlyList<IReadOnlyList<TranslationResult>>> TranslateBatchAsync(
            int n,
            IReadOnlyList<IReadOnlyList<string>> segments,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            IReadOnlyList<IReadOnlyList<TranslationResult>> results = await InteractiveEngine
                .TranslateBatchAsync(n, segments, cancellationToken)
                .ConfigureAwait(false);
            if (RuleEngine == null)
                return results;

            IReadOnlyList<TranslationResult> ruleResults = await RuleEngine
                .TranslateBatchAsync(segments, cancellationToken)
                .ConfigureAwait(false);
            return results
                .Zip(
                    ruleResults,
                    (hypotheses, ruleResult) => hypotheses.Select(hypothesis => Merge(hypothesis, ruleResult)).ToArray()
                )
                .ToArray();
        }

        public async Task<WordGraph> GetWordGraphAsync(string segment, CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            WordGraph wordGraph = await InteractiveEngine
                .GetWordGraphAsync(segment, cancellationToken)
                .ConfigureAwait(false);
            if (RuleEngine == null)
                return wordGraph;

            TranslationResult ruleResult = await RuleEngine
                .TranslateAsync(segment, cancellationToken)
                .ConfigureAwait(false);
            return Merge(wordGraph, ruleResult);
        }

        public async Task<WordGraph> GetWordGraphAsync(
            IReadOnlyList<string> segment,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            WordGraph wordGraph = await InteractiveEngine
                .GetWordGraphAsync(segment, cancellationToken)
                .ConfigureAwait(false);
            if (RuleEngine == null)
                return wordGraph;

            TranslationResult ruleResult = await RuleEngine
                .TranslateAsync(segment, cancellationToken)
                .ConfigureAwait(false);
            return Merge(wordGraph, ruleResult);
        }

        public Task TrainSegmentAsync(
            string sourceSegment,
            string targetSegment,
            bool sentenceStart = true,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            return InteractiveEngine.TrainSegmentAsync(sourceSegment, targetSegment, sentenceStart, cancellationToken);
        }

        public Task TrainSegmentAsync(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            bool sentenceStart = true,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            return InteractiveEngine.TrainSegmentAsync(sourceSegment, targetSegment, sentenceStart, cancellationToken);
        }

        public TranslationResult Translate(string segment)
        {
            CheckDisposed();

            TranslationResult result = InteractiveEngine.Translate(segment);
            if (RuleEngine == null)
                return result;

            TranslationResult ruleResult = RuleEngine.Translate(segment);
            return Merge(result, ruleResult);
        }

        public TranslationResult Translate(IReadOnlyList<string> segment)
        {
            CheckDisposed();

            TranslationResult result = InteractiveEngine.Translate(segment);
            if (RuleEngine == null)
                return result;

            TranslationResult ruleResult = RuleEngine.Translate(segment);
            return Merge(result, ruleResult);
        }

        public IReadOnlyList<TranslationResult> Translate(int n, string segment)
        {
            CheckDisposed();

            IReadOnlyList<TranslationResult> hypotheses = InteractiveEngine.Translate(n, segment);
            if (RuleEngine == null || hypotheses.Count == 0)
                return hypotheses;

            TranslationResult ruleResult = RuleEngine.Translate(segment);
            return hypotheses.Select(hypothesis => Merge(hypothesis, ruleResult)).ToArray();
        }

        public IReadOnlyList<TranslationResult> Translate(int n, IReadOnlyList<string> segment)
        {
            CheckDisposed();

            IReadOnlyList<TranslationResult> hypotheses = InteractiveEngine.Translate(n, segment);
            if (RuleEngine == null || hypotheses.Count == 0)
                return hypotheses;

            TranslationResult ruleResult = RuleEngine.Translate(segment);
            return hypotheses.Select(hypothesis => Merge(hypothesis, ruleResult)).ToArray();
        }

        public IReadOnlyList<TranslationResult> TranslateBatch(IReadOnlyList<string> segments)
        {
            CheckDisposed();

            IReadOnlyList<TranslationResult> results = InteractiveEngine.TranslateBatch(segments);
            if (RuleEngine == null)
                return results;

            IReadOnlyList<TranslationResult> ruleResults = RuleEngine.TranslateBatch(segments);
            return results.Zip(ruleResults, (result, ruleResult) => Merge(result, ruleResult)).ToArray();
        }

        public IReadOnlyList<TranslationResult> TranslateBatch(IReadOnlyList<IReadOnlyList<string>> segments)
        {
            CheckDisposed();

            IReadOnlyList<TranslationResult> results = InteractiveEngine.TranslateBatch(segments);
            if (RuleEngine == null)
                return results;

            IReadOnlyList<TranslationResult> ruleResults = RuleEngine.TranslateBatch(segments);
            return results.Zip(ruleResults, (result, ruleResult) => Merge(result, ruleResult)).ToArray();
        }

        public IReadOnlyList<IReadOnlyList<TranslationResult>> TranslateBatch(int n, IReadOnlyList<string> segments)
        {
            CheckDisposed();

            IReadOnlyList<IReadOnlyList<TranslationResult>> results = InteractiveEngine.TranslateBatch(n, segments);
            if (RuleEngine == null)
                return results;

            IReadOnlyList<TranslationResult> ruleResults = RuleEngine.TranslateBatch(segments);
            return results
                .Zip(
                    ruleResults,
                    (hypotheses, ruleResult) => hypotheses.Select(hypothesis => Merge(hypothesis, ruleResult)).ToArray()
                )
                .ToArray();
        }

        public IReadOnlyList<IReadOnlyList<TranslationResult>> TranslateBatch(
            int n,
            IReadOnlyList<IReadOnlyList<string>> segments
        )
        {
            CheckDisposed();

            IReadOnlyList<IReadOnlyList<TranslationResult>> results = InteractiveEngine.TranslateBatch(n, segments);
            if (RuleEngine == null)
                return results;

            IReadOnlyList<TranslationResult> ruleResults = RuleEngine.TranslateBatch(segments);
            return results
                .Zip(
                    ruleResults,
                    (hypotheses, ruleResult) => hypotheses.Select(hypothesis => Merge(hypothesis, ruleResult)).ToArray()
                )
                .ToArray();
        }

        public WordGraph GetWordGraph(string segment)
        {
            CheckDisposed();

            WordGraph wordGraph = InteractiveEngine.GetWordGraph(segment);
            if (RuleEngine == null)
                return wordGraph;

            TranslationResult ruleResult = RuleEngine.Translate(segment);
            return Merge(wordGraph, ruleResult);
        }

        public WordGraph GetWordGraph(IReadOnlyList<string> segment)
        {
            CheckDisposed();

            WordGraph wordGraph = InteractiveEngine.GetWordGraph(segment);
            if (RuleEngine == null)
                return wordGraph;

            TranslationResult ruleResult = RuleEngine.Translate(segment);
            return Merge(wordGraph, ruleResult);
        }

        public void TrainSegment(string sourceSegment, string targetSegment, bool sentenceStart = true)
        {
            CheckDisposed();

            InteractiveEngine.TrainSegment(sourceSegment, targetSegment, sentenceStart);
        }

        public void TrainSegment(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            bool sentenceStart = true
        )
        {
            CheckDisposed();

            InteractiveEngine.TrainSegment(sourceSegment, targetSegment, sentenceStart);
        }

        public TranslationResult GetBestPhraseAlignment(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment
        )
        {
            CheckDisposed();

            return InteractiveEngine.GetBestPhraseAlignment(sourceSegment, targetSegment);
        }

        public TranslationResult GetBestPhraseAlignment(string sourceSegment, string targetSegment)
        {
            CheckDisposed();

            return InteractiveEngine.GetBestPhraseAlignment(sourceSegment, targetSegment);
        }

        async Task<TranslationResult> IWordAlignerEngine.GetBestPhraseAlignmentAsync(
            string sourceSegment,
            string targetSegment,
            CancellationToken cancellationToken
        )
        {
            CheckDisposed();

            return await InteractiveEngine
                .GetBestPhraseAlignmentAsync(sourceSegment, targetSegment, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<TranslationResult> GetBestPhraseAlignmentAsync(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            return await InteractiveEngine
                .GetBestPhraseAlignmentAsync(sourceSegment, targetSegment, cancellationToken)
                .ConfigureAwait(false);
        }

        private TranslationResult Merge(TranslationResult interactiveResult, TranslationResult ruleResult)
        {
            var mergedTargetSegment = new List<string>();
            var mergedConfidences = new List<double>();
            var mergedSources = new List<TranslationSources>();
            var mergedAlignment = new HashSet<Tuple<int, int>>();
            for (int j = 0; j < interactiveResult.TargetTokens.Count; j++)
            {
                int[] sourceIndices = interactiveResult.Alignment.GetColumnAlignedIndices(j).ToArray();
                if (sourceIndices.Length == 0)
                {
                    // target word doesn't align with anything
                    mergedTargetSegment.Add(interactiveResult.TargetTokens[j]);
                    mergedConfidences.Add(interactiveResult.Confidences[j]);
                    mergedSources.Add(interactiveResult.Sources[j]);
                }
                else
                {
                    // target word aligns with some source words
                    if (interactiveResult.Confidences[j] >= InteractiveEngineThreshold)
                    {
                        // use target word of this result
                        mergedTargetSegment.Add(interactiveResult.TargetTokens[j]);
                        mergedConfidences.Add(interactiveResult.Confidences[j]);
                        TranslationSources sources = interactiveResult.Sources[j];
                        foreach (int i in sourceIndices)
                        {
                            // combine sources for any words that both this result
                            // and the other result translated the same
                            foreach (int jOther in ruleResult.Alignment.GetRowAlignedIndices(i))
                            {
                                TranslationSources otherSources = ruleResult.Sources[jOther];
                                if (
                                    otherSources != TranslationSources.None
                                    && ruleResult.TargetTokens[jOther] == interactiveResult.TargetTokens[j]
                                )
                                {
                                    sources |= otherSources;
                                }
                            }

                            mergedAlignment.Add(Tuple.Create(i, mergedTargetSegment.Count - 1));
                        }
                        mergedSources.Add(sources);
                    }
                    else
                    {
                        // use target words of other result
                        bool found = false;
                        foreach (int i in sourceIndices)
                        {
                            foreach (int jOther in ruleResult.Alignment.GetRowAlignedIndices(i))
                            {
                                // look for any translated words from other result
                                TranslationSources otherSources = ruleResult.Sources[jOther];
                                if (otherSources != TranslationSources.None)
                                {
                                    mergedTargetSegment.Add(ruleResult.TargetTokens[jOther]);
                                    mergedConfidences.Add(ruleResult.Confidences[jOther]);
                                    mergedSources.Add(otherSources);
                                    mergedAlignment.Add(Tuple.Create(i, mergedTargetSegment.Count - 1));
                                    found = true;
                                }
                            }
                        }

                        if (!found)
                        {
                            // the other result had no translated words, so just use this result's target word
                            mergedTargetSegment.Add(interactiveResult.TargetTokens[j]);
                            mergedConfidences.Add(interactiveResult.Confidences[j]);
                            mergedSources.Add(interactiveResult.Sources[j]);
                            foreach (int i in sourceIndices)
                                mergedAlignment.Add(Tuple.Create(i, mergedTargetSegment.Count - 1));
                        }
                    }
                }
            }

            var alignment = new WordAlignmentMatrix(interactiveResult.SourceTokens.Count, mergedTargetSegment.Count);
            foreach (Tuple<int, int> t in mergedAlignment)
                alignment[t.Item1, t.Item2] = true;
            return new TranslationResult(
                TargetDetokenizer.Detokenize(mergedTargetSegment),
                interactiveResult.SourceTokens,
                mergedTargetSegment,
                mergedConfidences,
                mergedSources,
                alignment,
                interactiveResult.Phrases
            );
        }

        public WordGraph Merge(WordGraph wordGraph, TranslationResult result)
        {
            return new WordGraph(
                wordGraph.SourceTokens,
                wordGraph.Arcs.Select(a => Merge(a, result)),
                wordGraph.FinalStates,
                wordGraph.InitialStateScore
            );
        }

        private WordGraphArc Merge(WordGraphArc arc, TranslationResult result)
        {
            var mergedWords = new List<string>();
            var mergedConfidences = new List<double>();
            var mergedSources = new List<TranslationSources>();
            var mergedAlignment = new HashSet<Tuple<int, int>>();
            for (int j = 0; j < arc.TargetTokens.Count; j++)
            {
                int[] sourceIndices = arc.Alignment.GetColumnAlignedIndices(j).ToArray();
                if (sourceIndices.Length == 0)
                {
                    // target word doesn't align with anything
                    mergedWords.Add(arc.TargetTokens[j]);
                    mergedConfidences.Add(arc.Confidences[j]);
                    mergedSources.Add(arc.Sources[j]);
                }
                else
                {
                    // target word aligns with some source words
                    if (arc.Confidences[j] >= InteractiveEngineThreshold)
                    {
                        // use target word of this result
                        mergedWords.Add(arc.TargetTokens[j]);
                        mergedConfidences.Add(arc.Confidences[j]);
                        TranslationSources sources = arc.Sources[j];
                        foreach (int i in sourceIndices)
                        {
                            // combine sources for any words that both this result
                            // and the other result translated the same
                            foreach (
                                int jOther in result.Alignment.GetRowAlignedIndices(arc.SourceSegmentRange.Start + i)
                            )
                            {
                                TranslationSources otherSources = result.Sources[jOther];
                                if (
                                    otherSources != TranslationSources.None
                                    && result.TargetTokens[jOther] == arc.TargetTokens[j]
                                )
                                {
                                    sources |= otherSources;
                                }
                            }

                            mergedAlignment.Add(Tuple.Create(i, mergedWords.Count - 1));
                        }
                        mergedSources.Add(sources);
                    }
                    else
                    {
                        // use target words of other result
                        bool found = false;
                        foreach (int i in sourceIndices)
                        {
                            foreach (
                                int jOther in result.Alignment.GetRowAlignedIndices(arc.SourceSegmentRange.Start + i)
                            )
                            {
                                // look for any translated words from other result
                                TranslationSources otherSources = result.Sources[jOther];
                                if (otherSources != TranslationSources.None)
                                {
                                    mergedWords.Add(result.TargetTokens[jOther]);
                                    mergedConfidences.Add(result.Confidences[jOther]);
                                    mergedSources.Add(otherSources);
                                    mergedAlignment.Add(Tuple.Create(i, mergedWords.Count - 1));
                                    found = true;
                                }
                            }
                        }

                        if (!found)
                        {
                            // the other result had no translated words, so just use this result's target word
                            mergedWords.Add(arc.TargetTokens[j]);
                            mergedConfidences.Add(arc.Confidences[j]);
                            mergedSources.Add(arc.Sources[j]);
                            foreach (int i in sourceIndices)
                                mergedAlignment.Add(Tuple.Create(i, mergedWords.Count - 1));
                        }
                    }
                }
            }

            var alignment = new WordAlignmentMatrix(arc.SourceSegmentRange.Length, mergedWords.Count);
            foreach (Tuple<int, int> t in mergedAlignment)
                alignment[t.Item1, t.Item2] = true;
            return new WordGraphArc(
                arc.PrevState,
                arc.NextState,
                arc.Score,
                mergedWords,
                alignment,
                arc.SourceSegmentRange,
                mergedSources,
                mergedConfidences
            );
        }

        protected override void DisposeManagedResources()
        {
            InteractiveEngine.Dispose();
            RuleEngine?.Dispose();
        }
    }
}
