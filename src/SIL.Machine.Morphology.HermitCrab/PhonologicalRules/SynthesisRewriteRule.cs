using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public class SynthesisRewriteRule : IRule<Word, ShapeNode>
    {
        private readonly Morpher _morpher;
        private readonly RewriteRule _rule;
        private readonly PhonologicalPatternRule _patternRule;

        public SynthesisRewriteRule(Morpher morpher, RewriteRule rule)
        {
            _morpher = morpher;
            _rule = rule;

            var settings = new MatcherSettings<ShapeNode>
            {
                Direction = rule.Direction,
                Filter = ann =>
                    ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Boundary, HCFeatureSystem.Anchor)
                    && !ann.IsDeleted(),
                UseDefaults = true
            };

            var ruleSpec = new SynthesisRewriteRuleSpec(
                settings,
                rule.ApplicationMode == RewriteApplicationMode.Iterative,
                _rule.Lhs,
                _rule.Subrules
            );

            _patternRule = null;
            switch (rule.ApplicationMode)
            {
                case RewriteApplicationMode.Iterative:
                    _patternRule = new IterativePhonologicalPatternRule(ruleSpec, settings);
                    break;

                case RewriteApplicationMode.Simultaneous:
                    _patternRule = new SimultaneousPhonologicalPatternRule(ruleSpec, settings);
                    break;
            }
        }

        public IEnumerable<Word> Apply(Word input)
        {
            if (!_morpher.RuleSelector(_rule))
                return Enumerable.Empty<Word>();

            Word origInput = null;
            if (_morpher.TraceManager.IsTracing)
            {
                origInput = input.Clone();
                input.CurrentRuleResults = new Dictionary<int, Tuple<FailureReason, object>>();
            }

            bool applied = _patternRule.Apply(input).Any();

            if (_morpher.TraceManager.IsTracing)
            {
                for (int i = 0; i < _rule.Subrules.Count; i++)
                {
                    Tuple<FailureReason, object> reason;
                    if (input.CurrentRuleResults.TryGetValue(i, out reason))
                    {
                        if (reason.Item1 == FailureReason.None)
                        {
                            _morpher.TraceManager.PhonologicalRuleApplied(_rule, i, origInput, input);
                            break;
                        }
                        _morpher.TraceManager.PhonologicalRuleNotApplied(_rule, i, input, reason.Item1, reason.Item2);
                    }
                    else
                        _morpher.TraceManager.PhonologicalRuleNotApplied(_rule, i, input, FailureReason.Pattern, null);
                }
                input.CurrentRuleResults = null;
            }
            if (applied)
                return input.ToEnumerable();
            return Enumerable.Empty<Word>();
        }
    }
}
