using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public class AnalysisAffixProcessRule : IRule<Word, ShapeNode>
    {
        private readonly Morpher _morpher;
        private readonly AffixProcessRule _rule;
        private readonly List<PatternRule<Word, ShapeNode>> _rules;
        private readonly FeatureStruct _outputFS;

        public AnalysisAffixProcessRule(Morpher morpher, AffixProcessRule rule)
        {
            _morpher = morpher;
            _rule = rule;

            // synthesis computes PriorityUnion(Unify(stem, Required), Out), so every word this rule produces is
            // subsumed by PriorityUnion(Required, Out): a tighter gate than Out alone that still admits them all
            _outputFS = rule.RequiredSyntacticFeatureStruct.Clone();
            _outputFS.PriorityUnion(rule.OutSyntacticFeatureStruct);
            _outputFS.Freeze();

            _rules = new List<PatternRule<Word, ShapeNode>>();
            foreach (AffixProcessAllomorph allo in rule.Allomorphs)
            {
                _rules.Add(
                    new CopyAgreementPatternRule(
                        morpher,
                        new AnalysisAffixProcessAllomorphRuleSpec(allo),
                        new MatcherSettings<ShapeNode>
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
            if (!_morpher.RuleSelector(_rule))
                return Enumerable.Empty<Word>();

            if (
                input.GetUnapplicationCount(_rule) >= _rule.MaxApplicationCount
                || !_outputFS.IsUnifiable(input.SyntacticFeatureStruct)
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
                    // the paths Out writes describe this rule's output, not the stem it applied to, so they are
                    // stripped rather than carried backwards; what is left must unify with Required
                    outWord.SyntacticFeatureStruct.Restrict(_rule.OutSyntacticFeatureStruct);
                    if (!_rule.RequiredSyntacticFeatureStruct.IsEmpty)
                    {
                        if (
                            !outWord.SyntacticFeatureStruct.Unify(
                                _rule.RequiredSyntacticFeatureStruct,
                                out FeatureStruct syntacticFS
                            )
                        )
                        {
                            continue;
                        }

                        outWord.SyntacticFeatureStruct = syntacticFS;
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
