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

		protected RewriteRuleSpec(bool isTargetEmpty)
		{
			_pattern = new Pattern<Word, ShapeNode>();
			_subruleSpecs = new List<RewriteSubruleSpec>();
			_isTargetEmpty = isTargetEmpty;
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

		public bool MatchSubrule(PhonologicalPatternRule rule, Match<Word, ShapeNode> match,
			out PhonologicalSubruleMatch subruleMatch)
		{
			foreach (RewriteSubruleSpec subruleSpec in _subruleSpecs)
			{
				if (!subruleSpec.IsApplicable(match.Input))
					continue;

				ShapeNode leftNode, rightNode, startNode, endNode;
				if (_isTargetEmpty)
				{
					if (match.Matcher.Direction == Direction.LeftToRight)
					{
						leftNode = match.Span.Start;
						rightNode = match.Span.End.Next;
					}
					else
					{
						leftNode = match.Span.Start.Prev;
						rightNode = match.Span.End;
					}

					startNode = leftNode;
					endNode = rightNode;
				}
				else
				{
					leftNode = match.Span.Start.Prev;
					rightNode = match.Span.End.Next;
					startNode = match.Span.Start;
					endNode = match.Span.End;
				}

				if (leftNode == null || rightNode == null)
				{
					subruleMatch = null;
					return false;
				}

				VariableBindings varBindings = match.VariableBindings;
				Match<Word, ShapeNode> leftEnvMatch = subruleSpec.LeftEnvironmentMatcher == null ? null
					: subruleSpec.LeftEnvironmentMatcher.Match(match.Input, leftNode, varBindings);
				if (leftEnvMatch == null || leftEnvMatch.Success)
				{
					if (leftEnvMatch != null && leftEnvMatch.VariableBindings != null)
						varBindings = leftEnvMatch.VariableBindings;

					Match<Word, ShapeNode> rightEnvMatch = subruleSpec.RightEnvironmentMatcher == null ? null
						: subruleSpec.RightEnvironmentMatcher.Match(match.Input, rightNode, varBindings);
					if (rightEnvMatch == null || rightEnvMatch.Success)
					{
						if (rightEnvMatch != null && rightEnvMatch.VariableBindings != null)
							varBindings = rightEnvMatch.VariableBindings;

						subruleMatch = new PhonologicalSubruleMatch(subruleSpec,
							Span<ShapeNode>.Create(startNode, endNode), varBindings);
						return true;
					}
				}
			}

			subruleMatch = null;
			return false;
		}
	}
}
