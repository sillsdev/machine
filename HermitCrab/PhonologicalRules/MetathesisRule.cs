using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
    /// <summary>
    /// This class represents a metathesis rule. Metathesis rules are phonological rules that
    /// reorder segments.
    /// </summary>
    public class MetathesisRule : IDBearerBase, IPhonologicalRule
    {
    	private readonly List<string> _groupOrder;

    	public MetathesisRule(string id)
			: base(id)
    	{
			Pattern = new Pattern<Word, ShapeNode>();
			_groupOrder = new List<string>();
    	}

		public Direction Direction { get; set; }

		public Pattern<Word, ShapeNode> Pattern { get; set; }

    	public IList<string> GroupOrder
    	{
    		get { return _groupOrder; }
    	}

    	public IRule<Word, ShapeNode> CompileAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
    	{
    		Group<Word, ShapeNode>[] groupOrder = Pattern.Children.OfType<Group<Word, ShapeNode>>().ToArray();
			Dictionary<string, Group<Word, ShapeNode>> groups = groupOrder.ToDictionary(g => g.Name);
    		var pattern = new Pattern<Word, ShapeNode>();
			foreach (PatternNode<Word, ShapeNode> node in Pattern.Children.TakeWhile(n => !(n is Group<Word, ShapeNode>)))
				pattern.Children.Add(node.DeepClone());
			foreach (string name in _groupOrder)
			{
				var newGroup = new Group<Word, ShapeNode>(name);
				foreach (Constraint<Word, ShapeNode> constraint in groups[name].Children)
				{
					Constraint<Word, ShapeNode> newConstraint = constraint.DeepClone();
					newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
					newGroup.Children.Add(newConstraint);
				}
				pattern.Children.Add(newGroup);
			}
    		foreach (PatternNode<Word, ShapeNode> node in Pattern.Children.GetNodes(Direction.RightToLeft).TakeWhile(n => !(n is Group<Word, ShapeNode>)).Reverse())
				pattern.Children.Add(node.DeepClone());

    		return new BacktrackingPatternRule(spanFactory, new DefaultPatternRuleSpec<Word, ShapeNode>(pattern,
				(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output) => Reorder(groupOrder.Select(g => g.Name), rule, match, out output)),
				ApplicationMode.Iterative, new MatcherSettings<ShapeNode>
											{
												Direction = Direction == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight,
												Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Anchor)
											});
    	}

    	public IRule<Word, ShapeNode> CompileSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
    	{
			var pattern = new Pattern<Word, ShapeNode>();
			foreach (PatternNode<Word, ShapeNode> node in Pattern.Children)
			{
				var group = node as Group<Word, ShapeNode>;
				if (group != null)
				{
					var newGroup = new Group<Word, ShapeNode>(group.Name);
					foreach (Constraint<Word, ShapeNode> constraint in group.Children)
					{
						Constraint<Word, ShapeNode> newConstraint = constraint.DeepClone();
						newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
						newGroup.Children.Add(newConstraint);
					}
					pattern.Children.Add(newGroup);
				}
				else
				{
					pattern.Children.Add(node.DeepClone());
				}
			}

			return new BacktrackingPatternRule(spanFactory, new DefaultPatternRuleSpec<Word, ShapeNode>(pattern,
				(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output) => Reorder(_groupOrder, rule, match, out output)),
				ApplicationMode.Iterative, new MatcherSettings<ShapeNode>
											{
												Direction = Direction,
												Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Boundary, HCFeatureSystem.Anchor),
												UseDefaults = true
											});
    	}

		private ShapeNode Reorder(IEnumerable<string> groupOrder, PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
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
			foreach (string name in groupOrder)
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