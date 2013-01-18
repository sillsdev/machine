using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class AnalysisSimpleCompoundingSubruleRule : IRule<Word, ShapeNode>
	{
		private readonly SpanFactory<ShapeNode> _spanFactory; 
		private readonly CompoundingSubrule _subrule;
		private readonly Matcher<Word, ShapeNode> _headMatcher;
		private readonly AnalysisMorphologicalTransform _headTransform;
		private readonly Matcher<Word, ShapeNode> _nonheadMatcher;
		private readonly AnalysisMorphologicalTransform _nonheadTransform;

		public AnalysisSimpleCompoundingSubruleRule(SpanFactory<ShapeNode> spanFactory, CompoundingSubrule sr)
		{
			_spanFactory = spanFactory;
			_subrule = sr;

			var leftRhs = new List<MorphologicalOutputAction>();
			var rightRhs = new List<MorphologicalOutputAction>();
			bool right = false;
			bool leftHeaded = true;
			foreach (MorphologicalOutputAction action in sr.Rhs)
			{
				if (right)
				{
					rightRhs.Add(action);
				}
				else
				{
					leftRhs.Add(action);
					if (action.PartName == sr.HeadLhs[0].Name)
					{
						right = true;
					}
					else if (action.PartName == sr.NonHeadLhs[0].Name)
					{
						right = true;
						leftHeaded = false;
					}
				}
			}

			_headTransform = new AnalysisMorphologicalTransform(sr.HeadLhs, leftHeaded ? leftRhs : rightRhs);
			_headMatcher = new Matcher<Word, ShapeNode>(spanFactory, _headTransform.Pattern, new MatcherSettings<ShapeNode>
				{
					Direction = leftHeaded ? Direction.LeftToRight : Direction.RightToLeft,
					Filter = ann => ann.Type() == HCFeatureSystem.Segment,
					AnchoredToStart = true
				});
			_nonheadTransform = new AnalysisMorphologicalTransform(sr.NonHeadLhs, leftHeaded ? rightRhs : leftRhs);
			_nonheadMatcher = new Matcher<Word, ShapeNode>(spanFactory, _nonheadTransform.Pattern, new MatcherSettings<ShapeNode>
				{
					Filter = ann => ann.Type() == HCFeatureSystem.Segment,
					AnchoredToStart = true,
					AnchoredToEnd = true
				});
		}

		public IEnumerable<Word> Apply(Word input)
		{
			var results = new HashSet<Word>();
			foreach (Match<Word, ShapeNode> headMatch in _headMatcher.AllMatches(input))
			{
				if (headMatch.Span.Start == input.Shape.GetFirst(n => n.Annotation.Type() == HCFeatureSystem.Segment)
				    && headMatch.Span.End == input.Shape.GetLast(n => n.Annotation.Type() == HCFeatureSystem.Segment))
				{
					continue;
				}
				Word headWord = input.DeepClone();
				_headTransform.GenerateShape(_subrule.HeadLhs, headWord.Shape, headMatch);
				var nonHeadShape = new Shape(_spanFactory, begin => new ShapeNode(_spanFactory, begin ? HCFeatureSystem.LeftSideAnchor : HCFeatureSystem.RightSideAnchor));
				input.Shape.CopyTo(_spanFactory.Create(headMatch.Span.GetEnd(_headMatcher.Direction).GetNext(_headMatcher.Direction),
					input.Shape.GetLast(_headMatcher.Direction), _headMatcher.Direction), nonHeadShape);
				var nonheadInput = new Word(input.Stratum, nonHeadShape);
				foreach (Match<Word, ShapeNode> nonheadMatch in _nonheadMatcher.AllMatches(nonheadInput))
				{
					Word nonheadWord = nonheadInput.DeepClone();
					_nonheadTransform.GenerateShape(_subrule.NonHeadLhs, nonheadWord.Shape, nonheadMatch);
					Word result = headWord.DeepClone();
					result.NonHeadUnapplied(nonheadWord);
					results.Add(result);
				}
			}

			return results;
		}
	}
}
