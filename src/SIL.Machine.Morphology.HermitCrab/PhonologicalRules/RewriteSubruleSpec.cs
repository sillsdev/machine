using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
	public abstract class RewriteSubruleSpec : IPhonologicalPatternSubruleSpec
	{
		private readonly Matcher<Word, ShapeNode> _leftEnvMatcher;
		private readonly Matcher<Word, ShapeNode> _rightEnvMatcher;

		protected RewriteSubruleSpec(SpanFactory<ShapeNode> spanFactory, MatcherSettings<ShapeNode> matcherSettings,
			Pattern<Word, ShapeNode> leftEnv, Pattern<Word, ShapeNode> rightEnv)
		{
			if (leftEnv != null && !leftEnv.IsEmpty)
			{
				MatcherSettings<ShapeNode> leftEnvMatcherSettings = matcherSettings.Clone();
				leftEnvMatcherSettings.Direction = Direction.RightToLeft;
				leftEnvMatcherSettings.AnchoredToStart = true;
				_leftEnvMatcher = new Matcher<Word, ShapeNode>(spanFactory, leftEnv, leftEnvMatcherSettings);
			}

			if (rightEnv != null && !rightEnv.IsEmpty)
			{
				MatcherSettings<ShapeNode> rightEnvMatcherSettings = matcherSettings.Clone();
				rightEnvMatcherSettings.Direction = Direction.LeftToRight;
				rightEnvMatcherSettings.AnchoredToStart = true;
				_rightEnvMatcher = new Matcher<Word, ShapeNode>(spanFactory, rightEnv, rightEnvMatcherSettings);
			}
		}

		public Matcher<Word, ShapeNode> LeftEnvironmentMatcher
		{
			get { return _leftEnvMatcher; }
		}

		public Matcher<Word, ShapeNode> RightEnvironmentMatcher
		{
			get { return _rightEnvMatcher; }
		}

		public virtual bool IsApplicable(Word input)
		{
			return true;
		}

		public abstract void ApplyRhs(Match<Word, ShapeNode> targetMatch, Span<ShapeNode> span, VariableBindings varBindings);
	}
}
