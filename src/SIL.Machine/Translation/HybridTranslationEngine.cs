using System.Collections.Generic;
using System.Linq;
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

        public TranslationResult Translate(IReadOnlyList<string> segment)
        {
            CheckDisposed();

            TranslationResult result = InteractiveEngine.Translate(segment);
            if (RuleEngine == null)
                return result;

            TranslationResult ruleResult = RuleEngine.Translate(segment);
            return result.Merge(RuleEngineThreshold, ruleResult);
        }

        public IReadOnlyList<TranslationResult> Translate(int n, IReadOnlyList<string> segment)
        {
            CheckDisposed();

            IReadOnlyList<TranslationResult> hypotheses = InteractiveEngine.Translate(n, segment);
            if (RuleEngine == null || hypotheses.Count == 0)
                return hypotheses;

            TranslationResult ruleResult = RuleEngine.Translate(segment);
            return hypotheses.Select(hypothesis => hypothesis.Merge(RuleEngineThreshold, ruleResult)).ToArray();
        }

        public IEnumerable<TranslationResult> Translate(IEnumerable<IReadOnlyList<string>> segments)
        {
            CheckDisposed();

            IEnumerable<TranslationResult> results = InteractiveEngine.Translate(segments);
            if (RuleEngine == null)
                return results;

            IEnumerable<TranslationResult> ruleResults = RuleEngine.Translate(segments);
            return results.Zip(ruleResults, (result, ruleResult) => result.Merge(RuleEngineThreshold, ruleResult));
        }

        public IEnumerable<IReadOnlyList<TranslationResult>> Translate(
            int n,
            IEnumerable<IReadOnlyList<string>> segments
        )
        {
            CheckDisposed();

            IEnumerable<IReadOnlyList<TranslationResult>> results = InteractiveEngine.Translate(n, segments);
            if (RuleEngine == null)
                return results;

            IEnumerable<TranslationResult> ruleResults = RuleEngine.Translate(segments);
            return results.Zip(
                ruleResults,
                (hypotheses, ruleResult) =>
                    hypotheses.Select(hypothesis => hypothesis.Merge(RuleEngineThreshold, ruleResult)).ToArray()
            );
        }

        public WordGraph GetWordGraph(IReadOnlyList<string> segment)
        {
            CheckDisposed();

            WordGraph wordGraph = InteractiveEngine.GetWordGraph(segment);
            if (RuleEngine == null)
                return wordGraph;

            TranslationResult ruleResult = RuleEngine.Translate(segment);
            return wordGraph.Merge(RuleEngineThreshold, ruleResult);
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

        protected override void DisposeManagedResources()
        {
            InteractiveEngine.Dispose();
            RuleEngine?.Dispose();
        }
    }
}
