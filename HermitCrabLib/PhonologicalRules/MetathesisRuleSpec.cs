using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class MetathesisRuleSpec : IPatternRuleSpec<Word, ShapeNode>
	{
		private readonly Pattern<Word, ShapeNode> _pattern;
		private readonly List<string> _groupOrder; 

		public MetathesisRuleSpec(Pattern<Word, ShapeNode> pattern, IEnumerable<string> groupOrder)
		{
			_pattern = pattern;
			_groupOrder = groupOrder.ToList();
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
			ShapeNode cur = start.Prev;
			foreach (string name in _groupOrder)
			{
				GroupCapture<ShapeNode> group = match.GroupCaptures[name];
				if (!group.Success)
					continue;

				foreach (ShapeNode node in match.Input.Shape.GetNodes(group.Span))
				{
					if (node != cur.Next)
					{
						node.Remove();
						cur.AddAfter(node);
					}
					node.SetDirty(true);
					cur = node;
				}
			}

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
	}
}
