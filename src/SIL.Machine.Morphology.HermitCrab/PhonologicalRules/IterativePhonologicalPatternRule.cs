using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public class IterativePhonologicalPatternRule : PhonologicalPatternRule
    {
        public IterativePhonologicalPatternRule(
            IPhonologicalPatternRuleSpec ruleSpec,
            MatcherSettings<ShapeNode> matcherSettings
        )
            : base(ruleSpec, matcherSettings) { }

        public override IEnumerable<Word> Apply(Word input)
        {
            bool applied = false;
            Match<Word, ShapeNode> targetMatch = Matcher.Match(input);
            while (targetMatch.Success)
            {
                ShapeNode start;
                PhonologicalSubruleMatch srMatch;
                if (RuleSpec.MatchSubrule(this, targetMatch, out srMatch))
                {
                    srMatch.SubruleSpec.ApplyRhs(targetMatch, srMatch.Range, srMatch.VariableBindings);
                    applied = true;
                    start = targetMatch.Range.GetEnd(Matcher.Direction).GetNext(Matcher.Direction);
                }
                else
                    start = targetMatch.Range.GetStart(Matcher.Direction).GetNext(Matcher.Direction);

                if (start == null)
                    break;

                targetMatch = Matcher.Match(input, start);
            }

            if (applied)
            {
                input.ResetDirty();
                return input.ToEnumerable();
            }
            return Enumerable.Empty<Word>();
        }
    }
}
