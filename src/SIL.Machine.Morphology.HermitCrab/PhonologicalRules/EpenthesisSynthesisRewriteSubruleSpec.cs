using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public class EpenthesisSynthesisRewriteSubruleSpec : SynthesisRewriteSubruleSpec
    {
        private readonly Pattern<Word, int> _rhs;

        public EpenthesisSynthesisRewriteSubruleSpec(
            MatcherSettings<int> matcherSettings,
            bool isIterative,
            RewriteSubrule subrule,
            int index
        )
            : base(matcherSettings, isIterative, subrule, index)
        {
            _rhs = subrule.Rhs;
        }

        public override void ApplyRhs(
            Match<Word, int> targetMatch,
            Range<ShapeNode> range,
            VariableBindings varBindings
        )
        {
            ShapeNode curNode = range.Start;
            foreach (PatternNode<Word, int> node in _rhs.Children.GetNodes(targetMatch.Matcher.Direction))
            {
                if (targetMatch.Input.Shape.Count == 256)
                    throw new InfiniteLoopException("An epenthesis rewrite rule is stuck in an infinite loop.");
                var constraint = (Constraint<Word, int>)node;
                FeatureStruct fs = constraint.FeatureStruct.Clone();
                if (varBindings != null)
                    fs.ReplaceVariables(varBindings);
                curNode = targetMatch.Input.Shape.AddAfter(curNode, fs);
                if (IsIterative)
                    curNode.SetDirty(true);
            }
            MarkSuccessfulApply(targetMatch.Input);
        }
    }
}
