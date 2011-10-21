using System;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	public abstract class SynthesisRewriteRule : PatternRuleBase<Word, ShapeNode>
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly FeatureStruct _applicableFS;

		protected SynthesisRewriteRule(SpanFactory<ShapeNode> spanFactory, Expression<Word, ShapeNode> lhs,
			Expression<Word, ShapeNode> leftEnv, Expression<Word, ShapeNode> rightEnv, FeatureStruct applicableFS)
			: base(new Pattern<Word, ShapeNode>(spanFactory))
		{
			Lhs.Filter = ann => ann.Type.IsOneOf(HCFeatureSystem.SegmentType, HCFeatureSystem.BoundaryType, HCFeatureSystem.AnchorType);
			Lhs.Acceptable = (input, match) => CheckTarget(match, lhs);

			_spanFactory = spanFactory;
			_applicableFS = applicableFS;

			if (leftEnv.Children.Count > 0)
			{
				Lhs.Children.Add(new Group<Word, ShapeNode>("leftEnv", leftEnv.Children.Clone()));
			}

			var target = new Group<Word, ShapeNode>("target");
			foreach (Constraint<Word, ShapeNode> constraint in lhs.Children)
			{
				var newConstraint = (Constraint<Word, ShapeNode>)constraint.Clone();
				newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Backtrack, HCFeatureSystem.NotSearched);
				target.Children.Add(newConstraint);
			}
			Lhs.Children.Add(target);
			if (rightEnv.Children.Count > 0)
			{
				Lhs.Children.Add(new Group<Word, ShapeNode>("rightEnv", rightEnv.Children.Clone()));
			}
		}

		private static bool CheckTarget(PatternMatch<ShapeNode> match, Expression<Word, ShapeNode> lhs)
		{
			Span<ShapeNode> target;
			if (match.TryGetGroup("target", out target))
			{
				foreach (Tuple<ShapeNode, PatternNode<Word, ShapeNode>> tuple in target.Start.GetNodes(target.End).Zip(lhs.Children))
				{
					var constraints = (Constraint<Word, ShapeNode>)tuple.Item2;
					if (tuple.Item1.Annotation.Type != constraints.Type)
						return false;
				}
			}
			return true;
		}

		public override bool IsApplicable(Word input)
		{
			return input.Annotation.FeatureStruct.IsUnifiable(_applicableFS);
		}

		protected void MarkSearchedNodes(ShapeNode startNode, ShapeNode endNode)
		{
			if (ApplicationMode == ApplicationMode.Iterative)
			{
				foreach (ShapeNode node in startNode.GetNodes(endNode, Lhs.Direction))
					node.Annotation.FeatureStruct.AddValue(HCFeatureSystem.Backtrack, HCFeatureSystem.Searched);
			}
		}

		protected ShapeNode CreateNodeFromConstraint(Constraint<Word, ShapeNode> constraint, VariableBindings varBindings)
		{
			var newNode = new ShapeNode(constraint.Type, _spanFactory, (FeatureStruct) constraint.FeatureStruct.Clone());
			newNode.Annotation.FeatureStruct.ReplaceVariables(varBindings);
			if (newNode.Annotation.FeatureStruct.HasVariables)
				throw new MorphException(MorphErrorCode.UninstantiatedFeature);
			return newNode;
		}
	}
}
