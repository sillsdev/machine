using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public class AnalysisMetathesisRuleSpec : IPhonologicalPatternRuleSpec, IPhonologicalPatternSubruleSpec
    {
        private readonly Pattern<Word, ShapeNode> _pattern;
        private readonly string _leftGroupName;
        private readonly string _rightGroupName;

        public AnalysisMetathesisRuleSpec(Pattern<Word, ShapeNode> pattern, string leftGroupName, string rightGroupName)
        {
            _leftGroupName = leftGroupName;
            _rightGroupName = rightGroupName;

            Group<Word, ShapeNode>[] groupOrder = pattern.Children.OfType<Group<Word, ShapeNode>>().ToArray();
            Dictionary<string, Group<Word, ShapeNode>> groups = groupOrder.ToDictionary(g => g.Name);
            _pattern = new Pattern<Word, ShapeNode>();
            foreach (
                PatternNode<Word, ShapeNode> node in pattern.Children.TakeWhile(n => !(n is Group<Word, ShapeNode>))
            )
            {
                _pattern.Children.Add(node.Clone());
            }

            AddGroup(groups, leftGroupName);
            AddGroup(groups, rightGroupName);

            foreach (
                PatternNode<Word, ShapeNode> node in pattern
                    .Children.GetNodes(Direction.RightToLeft)
                    .TakeWhile(n => !(n is Group<Word, ShapeNode>))
                    .Reverse()
            )
            {
                _pattern.Children.Add(node.Clone());
            }
            _pattern.Freeze();
        }

        private void AddGroup(Dictionary<string, Group<Word, ShapeNode>> groups, string name)
        {
            var newGroup = new Group<Word, ShapeNode>(name);
            foreach (
                Constraint<Word, ShapeNode> constraint in groups[name].Children.Cast<Constraint<Word, ShapeNode>>()
            )
            {
                Constraint<Word, ShapeNode> newConstraint = constraint.Clone();
                newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
                newGroup.Children.Add(newConstraint);
            }
            _pattern.Children.Add(newGroup);
        }

        public Pattern<Word, ShapeNode> Pattern
        {
            get { return _pattern; }
        }

        public bool MatchSubrule(
            PhonologicalPatternRule rule,
            Match<Word, ShapeNode> match,
            out PhonologicalSubruleMatch subruleMatch
        )
        {
            subruleMatch = new PhonologicalSubruleMatch(this, match.Range, match.VariableBindings);
            return true;
        }

        Matcher<Word, ShapeNode> IPhonologicalPatternSubruleSpec.LeftEnvironmentMatcher
        {
            get { return null; }
        }

        Matcher<Word, ShapeNode> IPhonologicalPatternSubruleSpec.RightEnvironmentMatcher
        {
            get { return null; }
        }

        bool IPhonologicalPatternSubruleSpec.IsApplicable(Word input)
        {
            return true;
        }

        void IPhonologicalPatternSubruleSpec.ApplyRhs(
            Match<Word, ShapeNode> targetMatch,
            Range<ShapeNode> range,
            VariableBindings varBindings
        )
        {
            ShapeNode start = null,
                end = null;
            foreach (GroupCapture<ShapeNode> gc in targetMatch.GroupCaptures)
            {
                if (start == null || gc.Range.Start.CompareTo(start) < 0)
                    start = gc.Range.Start;
                if (end == null || gc.Range.End.CompareTo(end) > 0)
                    end = gc.Range.End;
            }
            Debug.Assert(start != null && end != null);

            GroupCapture<ShapeNode> leftGroup = targetMatch.GroupCaptures[_leftGroupName];
            GroupCapture<ShapeNode> rightGroup = targetMatch.GroupCaptures[_rightGroupName];

            foreach (
                Tuple<ShapeNode, ShapeNode> tuple in targetMatch
                    .Input.Shape.GetNodes(leftGroup.Range)
                    .Zip(targetMatch.Input.Shape.GetNodes(rightGroup.Range))
            )
            {
                if (tuple.Item1.Type() != HCFeatureSystem.Segment || tuple.Item2.Type() != HCFeatureSystem.Segment)
                    continue;

                FeatureStruct fs = tuple.Item1.Annotation.FeatureStruct.Clone();
                tuple.Item1.Annotation.FeatureStruct.Union(tuple.Item2.Annotation.FeatureStruct);
                tuple.Item1.SetDirty(true);
                tuple.Item2.Annotation.FeatureStruct.Union(fs);
                tuple.Item2.SetDirty(true);
            }
        }
    }
}
