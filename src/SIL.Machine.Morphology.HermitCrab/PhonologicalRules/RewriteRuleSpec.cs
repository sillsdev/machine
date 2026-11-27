using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public abstract class RewriteRuleSpec : IPhonologicalPatternRuleSpec
    {
        private readonly Pattern<Word, ShapeNode> _pattern;
        private readonly List<RewriteSubruleSpec> _subruleSpecs;
        private readonly bool _isTargetEmpty;
        private readonly bool _removeRedundantDeletionMatches;

        protected RewriteRuleSpec(bool isTargetEmpty, bool removeRedundantDeletionMatches = false)
        {
            _pattern = new Pattern<Word, ShapeNode>();
            _subruleSpecs = new List<RewriteSubruleSpec>();
            _isTargetEmpty = isTargetEmpty;
            _removeRedundantDeletionMatches = isTargetEmpty && removeRedundantDeletionMatches;
        }

        public Pattern<Word, ShapeNode> Pattern
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
            Match<Word, ShapeNode> match,
            out PhonologicalSubruleMatch subruleMatch
        )
        {
            foreach (RewriteSubruleSpec subruleSpec in _subruleSpecs)
            {
                if (!subruleSpec.IsApplicable(match.Input))
                    continue;

                ShapeNode leftNode,
                    rightNode,
                    startNode,
                    endNode;
                if (_isTargetEmpty)
                {
                    if (match.Matcher.Direction == Direction.LeftToRight)
                    {
                        leftNode = match.Range.Start;
                        rightNode = match.Range.End.Next;
                    }
                    else
                    {
                        leftNode = match.Range.Start.Prev;
                        rightNode = match.Range.End;
                    }

                    startNode = leftNode;
                    endNode = rightNode;
                }
                else
                {
                    leftNode = match.Range.Start.Prev;
                    rightNode = match.Range.End.Next;
                    startNode = match.Range.Start;
                    endNode = match.Range.End;
                }

                if (leftNode == null || rightNode == null)
                {
                    subruleMatch = null;
                    return false;
                }

                VariableBindings varBindings = match.VariableBindings;
                Match<Word, ShapeNode> leftEnvMatch = subruleSpec.LeftEnvironmentMatcher?.Match(
                    match.Input,
                    leftNode,
                    varBindings
                );
                if (leftEnvMatch == null || leftEnvMatch.Success)
                {
                    if (RedundantMatch(subruleSpec.LeftEnvironmentMatcher, match.Input, leftNode, varBindings))
                    {
                        // The current match is redundant with the match before leftNode.
                        continue;
                    }
                    if (leftEnvMatch != null && leftEnvMatch.VariableBindings != null)
                        varBindings = leftEnvMatch.VariableBindings;

                    Match<Word, ShapeNode> rightEnvMatch = subruleSpec.RightEnvironmentMatcher?.Match(
                        match.Input,
                        rightNode,
                        varBindings
                    );
                    if (rightEnvMatch == null || rightEnvMatch.Success)
                    {
                        if (
                            RedundantMatch(subruleSpec.RightEnvironmentMatcher, match.Input, rightNode, varBindings)
                            && !RedundantMatch(
                                subruleSpec.LeftEnvironmentMatcher,
                                match.Input,
                                rightNode,
                                match.VariableBindings
                            )
                        )
                        {
                            // The current match is redundant with the match after rightNode,
                            // and the match after rightNode is not redundant with this one.
                            continue;
                        }
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

        private bool RedundantMatch(
            Matcher<Word, ShapeNode> matcher,
            Word input,
            ShapeNode node,
            VariableBindings varBindings
        )
        {
            if (_removeRedundantDeletionMatches && matcher != null && node.Annotation.Optional)
            {
                try
                {
                    node.Annotation.Optional = false;
                    Match<Word, ShapeNode> match = matcher.Match(input, node, varBindings);
                    if (match == null || !match.Success)
                    {
                        return true;
                    }
                }
                finally
                {
                    node.Annotation.Optional = true;
                }
            }
            return false;
        }
    }
}
