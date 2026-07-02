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
            MatcherSettings<int> matcherSettings
        )
            : base(ruleSpec, matcherSettings) { }

        public override IEnumerable<Word> Apply(Word input)
        {
            bool applied = false;
            Match<Word, int> targetMatch = Matcher.Match(input);
            while (targetMatch.Success)
            {
                ShapeNode start;
                PhonologicalSubruleMatch srMatch;
                // RUSTIFY Stage 2: int offsets in targetMatch.Range go stale once ApplyRhs mutates the
                // shape (the projection re-densifies), so resolve the directional end/start NODE now —
                // a ShapeNode handle survives mutation, exactly as the old ShapeNode match range did.
                // Only one of end/start is ever used per iteration, so resolve it inside its own
                // branch instead of paying for both NodeAt lookups unconditionally.
                if (RuleSpec.MatchSubrule(this, targetMatch, out srMatch))
                {
                    ShapeNode matchEndNode = input.Shape.GetEndNode(targetMatch.Range, Matcher.Direction);
                    srMatch.SubruleSpec.ApplyRhs(targetMatch, srMatch.Range, srMatch.VariableBindings);
                    applied = true;
                    start = matchEndNode.GetNext(Matcher.Direction);
                }
                else
                {
                    ShapeNode matchStartNode = input.Shape.GetStartNode(targetMatch.Range, Matcher.Direction);
                    start = matchStartNode.GetNext(Matcher.Direction);
                }

                if (start == null)
                    break;

                targetMatch = Matcher.Match(input, input.Shape.MatchStartOffset(start, Matcher.Direction));
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
