using System.Diagnostics;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public class SynthesisMetathesisRuleSpec : IPhonologicalPatternRuleSpec, IPhonologicalPatternSubruleSpec
    {
        private readonly Pattern<Word, int> _pattern;
        private readonly string _leftGroupName;
        private readonly string _rightGroupName;

        public SynthesisMetathesisRuleSpec(Pattern<Word, int> pattern, string leftGroupName, string rightGroupName)
        {
            _leftGroupName = leftGroupName;
            _rightGroupName = rightGroupName;

            _pattern = new Pattern<Word, int>();
            foreach (PatternNode<Word, int> node in pattern.Children)
            {
                if (node is Group<Word, int> group)
                {
                    var newGroup = new Group<Word, int>(group.Name);
                    foreach (Constraint<Word, int> constraint in group.Children.Cast<Constraint<Word, int>>())
                    {
                        Constraint<Word, int> newConstraint = constraint.Clone();
                        newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
                        newGroup.Children.Add(newConstraint);
                    }
                    _pattern.Children.Add(newGroup);
                }
                else
                {
                    _pattern.Children.Add(node.Clone());
                }
            }
            _pattern.Freeze();
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

        public void ApplyRhs(Match<Word, int> targetMatch, Range<ShapeNode> range, VariableBindings varBindings)
        {
            // RUSTIFY Stage 2: group captures are int offsets that go stale on the first structural
            // mutation (morph.Remove / MoveNodesAfter re-densify the projection), so resolve EVERYTHING
            // to ShapeNode refs up front — those survive the moves, as the old ShapeNode ranges did.
            Shape shape = targetMatch.Input.Shape;
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
            ShapeNode start = shape.NodeAt(startTag.Value);
            ShapeNode end = shape.NodeAt(endTag.Value - 1);

            GroupCapture<int> leftGroup = targetMatch.GroupCaptures[_leftGroupName];
            GroupCapture<int> rightGroup = targetMatch.GroupCaptures[_rightGroupName];
            Range<ShapeNode> leftRange = shape.ToShapeRange(leftGroup.Range);
            Range<ShapeNode> rightRange = shape.ToShapeRange(rightGroup.Range);
            ShapeNode leftEnd = shape.EndNode(leftGroup.Range);
            ShapeNode beforeRightGroup = shape.NodeAt(rightGroup.Range.Start).Prev;

            var morphs = targetMatch
                .Input.Morphs.Where(ann => ann.Range.Overlaps(start, end))
                .Select(ann => new { Annotation = ann, Children = ann.Children.ToList() })
                .ToArray();
            foreach (var morph in morphs)
                morph.Annotation.Remove();

            MoveNodesAfter(shape, leftEnd, rightRange);
            MoveNodesAfter(shape, beforeRightGroup, leftRange);

            foreach (var morph in morphs)
            {
                Annotation<ShapeNode>[] children = morph.Children.OrderBy(ann => ann.Range).ToArray();
                var newMorphAnn = new Annotation<ShapeNode>(
                    Range<ShapeNode>.Create(children[0].Range.Start, children[children.Length - 1].Range.Start),
                    morph.Annotation.FeatureStruct
                );
                newMorphAnn.Children.AddRange(morph.Children);
                shape.Annotations.Add(newMorphAnn, false);
            }
        }

        private static void MoveNodesAfter(Shape shape, ShapeNode cur, Range<ShapeNode> range)
        {
            foreach (ShapeNode node in shape.GetNodes(range).ToArray())
            {
                if (node.Type() == HCFeatureSystem.Segment)
                {
                    node.Remove();
                    cur.AddAfter(node);
                    node.SetDirty(true);
                }
                cur = node;
            }
        }
    }
}
