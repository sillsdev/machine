using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public abstract class RewriteRuleSpec : IPhonologicalPatternRuleSpec
    {
        private readonly Pattern<Word, int> _pattern;
        private readonly List<RewriteSubruleSpec> _subruleSpecs;
        private readonly bool _isTargetEmpty;

        protected RewriteRuleSpec(bool isTargetEmpty)
        {
            _pattern = new Pattern<Word, int>();
            _subruleSpecs = new List<RewriteSubruleSpec>();
            _isTargetEmpty = isTargetEmpty;
        }

        public Pattern<Word, int> Pattern
        {
            get { return _pattern; }
        }

        protected IList<RewriteSubruleSpec> SubruleSpecs
        {
            get { return _subruleSpecs; }
        }

        protected bool IsTargetEmpty
        {
            get { return _isTargetEmpty; }
        }

        public bool MatchSubrule(
            PhonologicalPatternRule rule,
            Match<Word, int> match,
            out PhonologicalSubruleMatch subruleMatch
        )
        {
            foreach (RewriteSubruleSpec subruleSpec in _subruleSpecs)
            {
                if (!subruleSpec.IsApplicable(match.Input))
                    continue;

                // RUSTIFY Stage 2: match.Range is now Range<int> ([leftmostTag, rightmostTag+1)); resolve
                // its bracketing nodes via the shape, then navigate the segment graph as before.
                Shape shape = match.Input.Shape;
                ShapeNode rangeStart = shape.NodeAt(match.Range.Start);
                ShapeNode rangeEnd = shape.NodeAt(match.Range.End - 1);
                ShapeNode leftNode,
                    rightNode,
                    startNode,
                    endNode;
                if (_isTargetEmpty)
                {
                    if (match.Matcher.Direction == Direction.LeftToRight)
                    {
                        leftNode = rangeStart;
                        rightNode = rangeEnd.Next;
                    }
                    else
                    {
                        leftNode = rangeStart.Prev;
                        rightNode = rangeEnd;
                    }

                    startNode = leftNode;
                    endNode = rightNode;
                }
                else
                {
                    leftNode = rangeStart.Prev;
                    rightNode = rangeEnd.Next;
                    startNode = rangeStart;
                    endNode = rangeEnd;
                }

                if (leftNode == null || rightNode == null)
                {
                    subruleMatch = null;
                    return false;
                }

                VariableBindings varBindings = match.VariableBindings;
                // left environment is matched right-to-left (see RewriteSubruleSpec)
                Match<Word, int> leftEnvMatch = subruleSpec.LeftEnvironmentMatcher?.Match(
                    match.Input,
                    shape.MatchStartOffset(leftNode, Direction.RightToLeft),
                    varBindings
                );
                if (leftEnvMatch == null || leftEnvMatch.Success)
                {
                    if (leftEnvMatch != null && leftEnvMatch.VariableBindings != null)
                        varBindings = leftEnvMatch.VariableBindings;

                    // right environment is matched left-to-right (see RewriteSubruleSpec)
                    Match<Word, int> rightEnvMatch = subruleSpec.RightEnvironmentMatcher?.Match(
                        match.Input,
                        shape.MatchStartOffset(rightNode, Direction.LeftToRight),
                        varBindings
                    );
                    if (rightEnvMatch == null || rightEnvMatch.Success)
                    {
                        if (rightEnvMatch != null && rightEnvMatch.VariableBindings != null)
                            varBindings = rightEnvMatch.VariableBindings;

                        subruleMatch = new PhonologicalSubruleMatch(
                            subruleSpec,
                            Range<ShapeNode>.Create(startNode, endNode),
                            varBindings
                        );
                        return true;
                    }
                }
            }

            subruleMatch = null;
            return false;
        }
    }
}
