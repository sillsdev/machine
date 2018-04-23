using System;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
	public class AnalysisRewriteSubruleSpec : RewriteSubruleSpec
	{
		private readonly Action<Match<Word, ShapeNode>, Range<ShapeNode>, VariableBindings> _applyAction;

		public AnalysisRewriteSubruleSpec(MatcherSettings<ShapeNode> matcherSettings, RewriteSubrule subrule,
			Action<Match<Word, ShapeNode>, Range<ShapeNode>, VariableBindings> applyAction)
			: base(matcherSettings, CreateEnvironmentPattern(subrule.LeftEnvironment),
				  CreateEnvironmentPattern(subrule.RightEnvironment))
		{
			_applyAction = applyAction;
		}

		private static Pattern<Word, ShapeNode> CreateEnvironmentPattern(Pattern<Word, ShapeNode> env)
		{
			Pattern<Word, ShapeNode> pattern = null;
			if (!env.IsEmpty)
				pattern = new Pattern<Word, ShapeNode>(env.Children.DeepCloneExceptBoundaries());
			return pattern;
		}

		public override void ApplyRhs(Match<Word, ShapeNode> targetMatch, Range<ShapeNode> range,
			VariableBindings varBindings)
		{
			_applyAction(targetMatch, range, varBindings);
		}
	}
}
