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
        private readonly Pattern<Word, int> _pattern;
        private readonly string _leftGroupName;
        private readonly string _rightGroupName;

        public AnalysisMetathesisRuleSpec(Pattern<Word, int> pattern, string leftGroupName, string rightGroupName)
        {
            _leftGroupName = leftGroupName;
            _rightGroupName = rightGroupName;

            Group<Word, int>[] groupOrder = pattern.Children.OfType<Group<Word, int>>().ToArray();
            Dictionary<string, Group<Word, int>> groups = groupOrder.ToDictionary(g => g.Name);
            _pattern = new Pattern<Word, int>();
            foreach (
                PatternNode<Word, int> node in pattern.Children.TakeWhile(n => !(n is Group<Word, int>))
            )
            {
                _pattern.Children.Add(node.Clone());
            }

            AddGroup(groups, leftGroupName);
            AddGroup(groups, rightGroupName);

            foreach (
                PatternNode<Word, int> node in pattern
                    .Children.GetNodes(Direction.RightToLeft)
                    .TakeWhile(n => !(n is Group<Word, int>))
                    .Reverse()
            )
            {
                _pattern.Children.Add(node.Clone());
            }
            _pattern.Freeze();
        }

        private void AddGroup(Dictionary<string, Group<Word, int>> groups, string name)
        {
            var newGroup = new Group<Word, int>(name);
            foreach (
                Constraint<Word, int> constraint in groups[name].Children.Cast<Constraint<Word, int>>()
            )
            {
                Constraint<Word, int> newConstraint = constraint.Clone();
                newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
                newGroup.Children.Add(newConstraint);
            }
            _pattern.Children.Add(newGroup);
        }

        public Pattern<Word, int> Pattern
        {
            get { return _pattern; }
        }

        public bool MatchSubrule(
            PhonologicalPatternRule rule,
            Match<Word, int> match,
            out PhonologicalSubruleMatch subruleMatch
        )
        {
            subruleMatch = new PhonologicalSubruleMatch(
                this,
                match.Input.Shape.ToShapeRange(match.Range),
                match.VariableBindings
            );
            return true;
        }

        Matcher<Word, int> IPhonologicalPatternSubruleSpec.LeftEnvironmentMatcher
        {
            get { return null; }
        }

        Matcher<Word, int> IPhonologicalPatternSubruleSpec.RightEnvironmentMatcher
        {
            get { return null; }
        }

        bool IPhonologicalPatternSubruleSpec.IsApplicable(Word input)
        {
            return true;
        }

        void IPhonologicalPatternSubruleSpec.ApplyRhs(
            Match<Word, int> targetMatch,
            Range<ShapeNode> range,
            VariableBindings varBindings
        )
        {
            int? startTag = null,
                endTag = null;
            foreach (GroupCapture<int> gc in targetMatch.GroupCaptures)
            {
                if (!gc.Success)
                    continue;
                if (startTag == null || gc.Range.Start < startTag)
                    startTag = gc.Range.Start;
                if (endTag == null || gc.Range.End > endTag)
                    endTag = gc.Range.End;
            }
            Debug.Assert(startTag != null && endTag != null);

            GroupCapture<int> leftGroup = targetMatch.GroupCaptures[_leftGroupName];
            GroupCapture<int> rightGroup = targetMatch.GroupCaptures[_rightGroupName];

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
