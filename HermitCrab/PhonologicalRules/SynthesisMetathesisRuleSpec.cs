using System.Diagnostics;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class SynthesisMetathesisRuleSpec : IPhonologicalPatternRuleSpec, IPhonologicalPatternSubruleSpec
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly Pattern<Word, ShapeNode> _pattern;
		private readonly string _leftGroupName;
		private readonly string _rightGroupName;

		public SynthesisMetathesisRuleSpec(SpanFactory<ShapeNode> spanFactory, Pattern<Word, ShapeNode> pattern, string leftGroupName, string rightGroupName)
		{
			_spanFactory = spanFactory;
			_leftGroupName = leftGroupName;
			_rightGroupName = rightGroupName;

			_pattern = new Pattern<Word, ShapeNode>();
			foreach (PatternNode<Word, ShapeNode> node in pattern.Children)
			{
				var group = node as Group<Word, ShapeNode>;
				if (group != null)
				{
					var newGroup = new Group<Word, ShapeNode>(group.Name);
					foreach (Constraint<Word, ShapeNode> constraint in group.Children.Cast<Constraint<Word, ShapeNode>>())
					{
						Constraint<Word, ShapeNode> newConstraint = constraint.DeepClone();
						newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
						newGroup.Children.Add(newConstraint);
					}
					_pattern.Children.Add(newGroup);
				}
				else
				{
					_pattern.Children.Add(node.DeepClone());
				}
			}
			_pattern.Freeze();
		}

		public Pattern<Word, ShapeNode> Pattern
		{
			get { return _pattern; }
		}

		public bool MatchSubrule(PhonologicalPatternRule rule, Match<Word, ShapeNode> match, out PhonologicalSubruleMatch subruleMatch)
		{
			subruleMatch = new PhonologicalSubruleMatch(this, match.Span, match.VariableBindings);
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

		public void ApplyRhs(Match<Word, ShapeNode> targetMatch, Span<ShapeNode> span, VariableBindings varBindings)
		{
			ShapeNode start = null, end = null;
			foreach (GroupCapture<ShapeNode> gc in targetMatch.GroupCaptures)
			{
				if (start == null || gc.Span.Start.CompareTo(start) < 0)
					start = gc.Span.Start;
				if (end == null || gc.Span.End.CompareTo(end) > 0)
					end = gc.Span.End;
			}
			Debug.Assert(start != null && end != null);

			var morphs = targetMatch.Input.Morphs.Where(ann => ann.Span.Overlaps(start, end))
				.Select(ann => new {Annotation = ann, Children = ann.Children.ToList()}).ToArray();
			foreach (var morph in morphs)
				morph.Annotation.Remove();

			GroupCapture<ShapeNode> leftGroup = targetMatch.GroupCaptures[_leftGroupName];
			GroupCapture<ShapeNode> rightGroup = targetMatch.GroupCaptures[_rightGroupName];

			ShapeNode beforeRightGroup = rightGroup.Span.Start.Prev;
			MoveNodesAfter(targetMatch.Input.Shape, leftGroup.Span.End, rightGroup.Span);
			MoveNodesAfter(targetMatch.Input.Shape, beforeRightGroup, leftGroup.Span);

			foreach (var morph in morphs)
			{
				Annotation<ShapeNode>[] children = morph.Children.OrderBy(ann => ann.Span).ToArray();
				var newMorphAnn = new Annotation<ShapeNode>(_spanFactory.Create(children[0].Span.Start, children[children.Length - 1].Span.Start), morph.Annotation.FeatureStruct);
				newMorphAnn.Children.AddRange(morph.Children);
				targetMatch.Input.Annotations.Add(newMorphAnn, false);
			}
		}

		private static void MoveNodesAfter(Shape shape, ShapeNode cur, Span<ShapeNode> span)
		{
			foreach (ShapeNode node in shape.GetNodes(span).ToArray())
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
