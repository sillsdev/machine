using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public class SynthesisMetathesisRule : InstrumentedRule<Word, ShapeNode>
    {
        private readonly Morpher _morpher;
        private readonly MetathesisRule _rule;
        private readonly PhonologicalPatternRule _patternRule;

        public SynthesisMetathesisRule(Morpher morpher, MetathesisRule rule)
        {
            Name = rule.Name;
            _morpher = morpher;
            _rule = rule;

            var ruleSpec = new SynthesisMetathesisRuleSpec(rule.Pattern, rule.LeftSwitchName, rule.RightSwitchName);

            var settings = new MatcherSettings<ShapeNode>
            {
                Direction = rule.Direction,
                Filter = ann =>
                    ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Boundary, HCFeatureSystem.Anchor)
                    && !ann.IsDeleted(),
                UseDefaults = true,
            };

            _patternRule = new IterativePhonologicalPatternRule(ruleSpec, settings);
        }

        public override IEnumerable<Word> Apply(Word input)
        {
            if (!_morpher.RuleSelector(_rule))
                return Enumerable.Empty<Word>();

            Word origInput = null;
            if (_morpher.TraceManager.IsTracing)
                origInput = input.Clone();

            if (_patternRule.Apply(input).Any())
            {
                if (_morpher.TraceManager.IsTracing)
                    _morpher.TraceManager.PhonologicalRuleApplied(_rule, -1, origInput, input);
                IEnumerable<Word> output = input.ToEnumerable();
                AddRuleStats(output.Count());
                return output;
            }

            if (_morpher.TraceManager.IsTracing)
                _morpher.TraceManager.PhonologicalRuleNotApplied(_rule, -1, input, FailureReason.Pattern, null);
            return Enumerable.Empty<Word>();
        }
    }
}
