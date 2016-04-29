using System.Diagnostics;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.HermitCrab.PhonologicalRules
{
	public class SynthesisMetathesisRuleSpec : IPatternRuleSpec<Word, ShapeNode>
	{
		private readonly Pattern<Word, ShapeNode> _pattern;
		private readonly string _leftGroupName;
		private readonly string _rightGroupName;

		public SynthesisMetathesisRuleSpec(Pattern<Word, ShapeNode> pattern, string leftGroupName, string rightGroupName)
		{
			_pattern = pattern;
			_leftGroupName = leftGroupName;
			_rightGroupName = rightGroupName;
		}

		public Pattern<Word, ShapeNode> Pattern
		{
			get { return _pattern; }
		}

		public bool IsApplicable(Word input)
		{
			return true;
		}

		public ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
		{
			ShapeNode start = null, end = null;
			foreach (GroupCapture<ShapeNode> gc in match.GroupCaptures)
			{
				if (start == null || gc.Span.Start.CompareTo(start) < 0)
					start = gc.Span.Start;
				if (end == null || gc.Span.End.CompareTo(end) > 0)
					end = gc.Span.End;
			}
			Debug.Assert(start != null && end != null);

			var morphs = match.Input.Morphs.Where(ann => ann.Span.Overlaps(start, end))
				.Select(ann => new { Annotation = ann, Children = ann.Children.ToList() }).ToArray();
			foreach (var morph in morphs)
				morph.Annotation.Remove();

			Direction dir = match.Matcher.Direction;
			ShapeNode beforeMatchStart = match.Span.GetStart(dir).GetPrev(dir);

			GroupCapture<ShapeNode> leftGroup = match.GroupCaptures[_leftGroupName];
			GroupCapture<ShapeNode> rightGroup = match.GroupCaptures[_rightGroupName];

			ShapeNode beforeRightGroup = rightGroup.Span.Start.Prev;
			MoveNodesAfter(match.Input.Shape, leftGroup.Span.End, rightGroup.Span);
			MoveNodesAfter(match.Input.Shape, beforeRightGroup, leftGroup.Span);

			foreach (var morph in morphs)
			{
				Annotation<ShapeNode>[] children = morph.Children.OrderBy(ann => ann.Span).ToArray();
				var newMorphAnn = new Annotation<ShapeNode>(rule.SpanFactory.Create(children[0].Span.Start, children[children.Length - 1].Span.Start), morph.Annotation.FeatureStruct);
				newMorphAnn.Children.AddRange(morph.Children);
				match.Input.Annotations.Add(newMorphAnn, false);
			}

			output = match.Input;
			ShapeNode matchStart = beforeMatchStart == null ? match.Input.Shape.GetBegin(dir) : beforeMatchStart.GetNext(dir);
			return matchStart.GetNext(dir);
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
