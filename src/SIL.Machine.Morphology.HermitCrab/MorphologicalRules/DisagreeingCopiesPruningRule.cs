using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    internal sealed class DisagreeingCopiesPruningRule : MultiplePatternRule<Word, ShapeNode>
    {
        private readonly Morpher _morpher;
        private readonly AnalysisMorphologicalTransformRuleSpec _spec;
        private readonly bool _hasRepeatedParts;

        public DisagreeingCopiesPruningRule(
            Morpher morpher,
            AnalysisMorphologicalTransformRuleSpec ruleSpec,
            MatcherSettings<ShapeNode> matcherSettings
        )
            : base(ruleSpec, matcherSettings)
        {
            _morpher = morpher;
            _spec = ruleSpec;
            _hasRepeatedParts = ruleSpec.HasRepeatedParts;
        }

        protected override IEnumerable<Word> ApplyImpl(Word input, ShapeNode start)
        {
            bool prune = _morpher.PruneDisagreeingCopies && _hasRepeatedParts && !_morpher.TraceManager.IsTracing;
            var results = new List<Word>();
            foreach (Match<Word, ShapeNode> match in Matcher.AllMatches(input, start))
            {
                if (prune && _spec.HasDisagreeingCopies(match))
                    continue;

                results.Add(RuleSpec.ApplyRhs(this, match));
            }
            return results;
        }
    }
}
