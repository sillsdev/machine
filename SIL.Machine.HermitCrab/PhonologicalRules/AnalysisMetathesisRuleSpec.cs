using System;
using System.Diagnostics;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.HermitCrab.PhonologicalRules
{
	public class AnalysisMetathesisRuleSpec : IPatternRuleSpec<Word, ShapeNode>
	{
		private readonly Pattern<Word, ShapeNode> _pattern;
		private readonly string _leftGroupName;
		private readonly string _rightGroupName;

		public AnalysisMetathesisRuleSpec(Pattern<Word, ShapeNode> pattern, string leftGroupName, string rightGroupName)
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

			Direction dir = match.Matcher.Direction;
			ShapeNode beforeMatchStart = match.Span.GetStart(dir).GetPrev(dir);

			GroupCapture<ShapeNode> leftGroup = match.GroupCaptures[_leftGroupName];
			GroupCapture<ShapeNode> rightGroup = match.GroupCaptures[_rightGroupName];

			foreach (Tuple<ShapeNode, ShapeNode> tuple in match.Input.Shape.GetNodes(leftGroup.Span).Zip(match.Input.Shape.GetNodes(rightGroup.Span)))
			{
				if (tuple.Item1.Type() != HCFeatureSystem.Segment || tuple.Item2.Type() != HCFeatureSystem.Segment)
					continue;

				FeatureStruct fs = tuple.Item1.Annotation.FeatureStruct.Clone();
				tuple.Item1.Annotation.FeatureStruct.Union(tuple.Item2.Annotation.FeatureStruct);
				tuple.Item1.SetDirty(true);
				tuple.Item2.Annotation.FeatureStruct.Union(fs);
				tuple.Item2.SetDirty(true);
			}

			output = match.Input;
			ShapeNode matchStart = beforeMatchStart == null ? match.Input.Shape.GetBegin(dir) : beforeMatchStart.GetNext(dir);
			return matchStart.GetNext(dir);
		}
	}
}
