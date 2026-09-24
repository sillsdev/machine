using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    internal sealed class CopyAgreementPatternRule : MultiplePatternRule<Word, ShapeNode>
    {
        private readonly Morpher _morpher;
        private readonly AnalysisAffixProcessAllomorphRuleSpec _spec;

        public CopyAgreementPatternRule(
            Morpher morpher,
            AnalysisAffixProcessAllomorphRuleSpec ruleSpec,
            MatcherSettings<ShapeNode> matcherSettings
        )
            : base(ruleSpec, matcherSettings)
        {
            _morpher = morpher;
            _spec = ruleSpec;
        }

        protected override IEnumerable<Word> ApplyImpl(Word input, ShapeNode start)
        {
            bool prune = _morpher.PruneDisagreeingCopies && _spec.HasRepeatedParts;
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
