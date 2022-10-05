using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
    public class HybridTranslationEngine : DisposableBase, IInteractiveTranslationEngine
    {
        internal const double RuleEngineThreshold = 0.05;

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

        public async Task<TranslationResult> TranslateAsync(
            IReadOnlyList<string> segment,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            TranslationResult result = await InteractiveEngine.TranslateAsync(segment, cancellationToken);
            if (RuleEngine == null)
                return result;

            TranslationResult ruleResult = await RuleEngine.TranslateAsync(segment, cancellationToken);
            return result.Merge(RuleEngineThreshold, ruleResult);
        }

        public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(
            int n,
            IReadOnlyList<string> segment,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            IReadOnlyList<TranslationResult> hypotheses = await InteractiveEngine.TranslateAsync(
                n,
                segment,
                cancellationToken
            );
            if (RuleEngine == null || hypotheses.Count == 0)
                return hypotheses;

            TranslationResult ruleResult = await RuleEngine.TranslateAsync(segment, cancellationToken);
            return hypotheses.Select(hypothesis => hypothesis.Merge(RuleEngineThreshold, ruleResult)).ToArray();
        }

        public async Task<IReadOnlyList<TranslationResult>> TranslateBatchAsync(
            IReadOnlyList<IReadOnlyList<string>> segments,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            IReadOnlyList<TranslationResult> results = await InteractiveEngine.TranslateBatchAsync(
                segments,
                cancellationToken
            );
            if (RuleEngine == null)
                return results;

            IReadOnlyList<TranslationResult> ruleResults = await RuleEngine.TranslateBatchAsync(
                segments,
                cancellationToken
            );
            return results
                .Zip(ruleResults, (result, ruleResult) => result.Merge(RuleEngineThreshold, ruleResult))
                .ToArray();
        }

        public async Task<IReadOnlyList<IReadOnlyList<TranslationResult>>> TranslateBatchAsync(
            int n,
            IReadOnlyList<IReadOnlyList<string>> segments,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            IReadOnlyList<IReadOnlyList<TranslationResult>> results = await InteractiveEngine.TranslateBatchAsync(
                n,
                segments,
                cancellationToken
            );
            if (RuleEngine == null)
                return results;

            IReadOnlyList<TranslationResult> ruleResults = await RuleEngine.TranslateBatchAsync(
                segments,
                cancellationToken
            );
            return results
                .Zip(
                    ruleResults,
                    (hypotheses, ruleResult) =>
                        hypotheses.Select(hypothesis => hypothesis.Merge(RuleEngineThreshold, ruleResult)).ToArray()
                )
                .ToArray();
        }

        public async Task<WordGraph> GetWordGraphAsync(
            IReadOnlyList<string> segment,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            WordGraph wordGraph = await InteractiveEngine.GetWordGraphAsync(segment, cancellationToken);
            if (RuleEngine == null)
                return wordGraph;

            TranslationResult ruleResult = await RuleEngine.TranslateAsync(segment, cancellationToken);
            return wordGraph.Merge(RuleEngineThreshold, ruleResult);
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

        protected override void DisposeManagedResources()
        {
            InteractiveEngine.Dispose();
            RuleEngine?.Dispose();
        }
    }
}
