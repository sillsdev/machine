using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public class AnalysisAffixProcessRule : IRule<Word, int>
    {
        private readonly Morpher _morpher;
        private readonly AffixProcessRule _rule;
        private readonly List<PatternRule<Word, int>> _rules;

        public AnalysisAffixProcessRule(Morpher morpher, AffixProcessRule rule)
        {
            _morpher = morpher;
            _rule = rule;

            _rules = new List<PatternRule<Word, int>>();
            foreach (AffixProcessAllomorph allo in rule.Allomorphs)
            {
                _rules.Add(
                    new MultiplePatternRule<Word, int>(
                        new AnalysisAffixProcessAllomorphRuleSpec(allo),
                        new MatcherSettings<int>
                        {
                            Filter = ann => ann.Type() == HCFeatureSystem.Segment,
                            MatchingMethod = MatchingMethod.Unification,
                            AnchoredToStart = true,
                            AnchoredToEnd = true,
                            AllSubmatches = true,
                        }
                    )
                );
            }
        }

        public IEnumerable<Word> Apply(Word input)
        {
            if (input.ParseContext?.Step(_rule) == false)
                return Enumerable.Empty<Word>();

            if (!_morpher.RuleSelector(_rule))
                return Enumerable.Empty<Word>();

            if (
                input.GetUnapplicationCount(_rule) >= _rule.MaxApplicationCount
                || !_rule.OutSyntacticFeatureStruct.IsUnifiable(input.SyntacticFeatureStruct)
            )
            {
                return Enumerable.Empty<Word>();
            }

            var output = new List<Word>();
            for (int i = 0; i < _rules.Count; i++)
            {
                bool unapplied = false;
                foreach (Word outWord in _rules[i].Apply(input).RemoveDuplicates())
                {
                    // Clone-then-reassign, not an in-place mutation: outWord may already be frozen by
                    // the pattern rule that produced it, and a frozen FeatureStruct must not be
                    // mutated in place (see Word.FreezeImpl's comment).
                    if (!_rule.RequiredSyntacticFeatureStruct.IsEmpty)
                    {
                        FeatureStruct sfs = outWord.SyntacticFeatureStruct.Clone();
                        sfs.Add(_rule.RequiredSyntacticFeatureStruct);
                        outWord.SyntacticFeatureStruct = sfs;
                    }
                    else if (_rule.OutSyntacticFeatureStruct.IsEmpty)
                    {
                        outWord.SyntacticFeatureStruct = new FeatureStruct();
                    }
                    outWord.MorphologicalRuleUnapplied(_rule);
                    outWord.Freeze();
                    if (_morpher.TraceManager.IsTracing)
                        _morpher.TraceManager.MorphologicalRuleUnapplied(_rule, i, input, outWord);
                    output.Add(outWord);
                    unapplied = true;
                }

                if (_morpher.TraceManager.IsTracing && !unapplied)
                    _morpher.TraceManager.MorphologicalRuleNotUnapplied(_rule, i, input);
            }
            return output;
        }
    }
}
