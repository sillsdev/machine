using System;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public abstract class SynthesisRewriteRuleSpec : IPatternRuleSpec<Word, ShapeNode>
	{
		private readonly Pattern<Word, ShapeNode> _pattern; 
		private readonly FeatureStruct _requiredSyntacticFS;

		protected SynthesisRewriteRuleSpec(Pattern<Word, ShapeNode> lhs,
			Pattern<Word, ShapeNode> leftEnv, Pattern<Word, ShapeNode> rightEnv, FeatureStruct requiredSyntacticFS)
		{
			_pattern = new Pattern<Word, ShapeNode> {Acceptable = match => CheckTarget(match, lhs)};
			if (leftEnv.Children.Count > 0)
				_pattern.Children.Add(new Group<Word, ShapeNode>("leftEnv", leftEnv.Children.Clone()));

			var target = new Group<Word, ShapeNode>("target");
			foreach (Constraint<Word, ShapeNode> constraint in lhs.Children)
			{
				var newConstraint = (Constraint<Word, ShapeNode>)constraint.Clone();
				newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Backtrack, HCFeatureSystem.NotSearched);
				target.Children.Add(newConstraint);
			}
			_pattern.Children.Add(target);
			if (rightEnv.Children.Count > 0)
				_pattern.Children.Add(new Group<Word, ShapeNode>("rightEnv", rightEnv.Children.Clone()));

			_requiredSyntacticFS = requiredSyntacticFS;
		}

		private static bool CheckTarget(Match<Word, ShapeNode> match, Pattern<Word, ShapeNode> lhs)
		{
			GroupCapture<ShapeNode> target = match["target"];
			if (target.Success)
			{
				foreach (Tuple<ShapeNode, PatternNode<Word, ShapeNode>> tuple in target.Span.Start.GetNodes(target.Span.End).Zip(lhs.Children))
				{
					var constraints = (Constraint<Word, ShapeNode>) tuple.Item2;
					if (tuple.Item1.Annotation.Type != constraints.Type)
						return false;
				}
			}
			return true;
		}

		public Pattern<Word, ShapeNode> Pattern
		{
			get { return _pattern; }
		}

		public bool IsApplicable(Word input)
		{
			return input.SyntacticFeatureStruct.IsUnifiable(_requiredSyntacticFS);
		}

		public abstract ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output);

		protected void MarkSearchedNodes(ShapeNode startNode, ShapeNode endNode, Direction dir)
		{
			foreach (ShapeNode node in startNode.GetNodes(endNode, dir))
				node.Annotation.FeatureStruct.AddValue(HCFeatureSystem.Backtrack, HCFeatureSystem.Searched);
		}
	}
}
