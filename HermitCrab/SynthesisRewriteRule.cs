using System;
using System.Linq;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	public abstract class SynthesisRewriteRule : PatternRuleBase<PhoneticShapeNode>
	{
		private readonly SpanFactory<PhoneticShapeNode> _spanFactory;
		private readonly FeatureStruct _applicableFS;

		protected SynthesisRewriteRule(SpanFactory<PhoneticShapeNode> spanFactory, Direction dir, bool simult, Expression<PhoneticShapeNode> lhs,
			Expression<PhoneticShapeNode> leftEnv, Expression<PhoneticShapeNode> rightEnv, FeatureStruct applicableFS)
			: base(new Pattern<PhoneticShapeNode>(spanFactory, dir, ann => true, (input, match) => CheckTarget(match, lhs)), simult)
		{
			_spanFactory = spanFactory;
			_applicableFS = applicableFS;

			if (leftEnv.Children.Count > 0)
			{
				if (leftEnv.Children.First is Anchor<PhoneticShapeNode>)
					Lhs.Children.Add(new Anchor<PhoneticShapeNode>(AnchorType.LeftSide));
				Lhs.Children.Add(new Group<PhoneticShapeNode>("leftEnv", leftEnv.Children.Where(node => !(node is Anchor<PhoneticShapeNode>)).Clone()));
			}

			var target = new Group<PhoneticShapeNode>("target");
			foreach (Constraint<PhoneticShapeNode> constraint in lhs.Children)
			{
				var newConstraint = (Constraint<PhoneticShapeNode>) constraint.Clone();
				newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Backtrack, HCFeatureSystem.NotSearched);
				target.Children.Add(newConstraint);
			}
			Lhs.Children.Add(target);
			if (rightEnv.Children.Count > 0)
			{
				Lhs.Children.Add(new Group<PhoneticShapeNode>("rightEnv", rightEnv.Children.Where(node => !(node is Anchor<PhoneticShapeNode>)).Clone()));
				if (rightEnv.Children.Last is Anchor<PhoneticShapeNode>)
					Lhs.Children.Add(new Anchor<PhoneticShapeNode>(AnchorType.RightSide));
			}
		}

		private static bool CheckTarget(PatternMatch<PhoneticShapeNode> match, Expression<PhoneticShapeNode> lhs)
		{
			Span<PhoneticShapeNode> target;
			if (match.TryGetGroup("target", out target))
			{
				foreach (Tuple<PhoneticShapeNode, PatternNode<PhoneticShapeNode>> tuple in target.Start.GetNodes(target.End).Zip(lhs.Children))
				{
					var constraints = (Constraint<PhoneticShapeNode>) tuple.Item2;
					if (tuple.Item1.Annotation.Type != constraints.Type)
						return false;
				}
			}
			return true;
		}

		public override bool IsApplicable(IBidirList<Annotation<PhoneticShapeNode>> input)
		{
			return input.First.FeatureStruct.IsUnifiable(_applicableFS);
		}

		protected void MarkSearchedNodes(PhoneticShapeNode startNode, PhoneticShapeNode endNode)
		{
			foreach (PhoneticShapeNode node in startNode.GetNodes(endNode, Lhs.Direction))
				node.Annotation.FeatureStruct.AddValue(HCFeatureSystem.Backtrack, HCFeatureSystem.Searched);
		}

		protected PhoneticShapeNode CreateNodeFromConstraint(Constraint<PhoneticShapeNode> constraint, VariableBindings varBindings)
		{
			var newNode = new PhoneticShapeNode(constraint.Type, _spanFactory, (FeatureStruct) constraint.FeatureStruct.Clone());
			newNode.Annotation.FeatureStruct.ReplaceVariables(varBindings);
			if (HasVariable(newNode.Annotation.FeatureStruct))
				throw new MorphException(MorphErrorCode.UninstantiatedFeature);
			return newNode;
		}

		protected bool HasVariable(FeatureStruct fs)
		{
			foreach (Feature feature in fs.Features)
			{
				FeatureValue value = fs.GetValue(feature);
				var childFS = value as FeatureStruct;
				if (childFS != null)
				{
					if (HasVariable(childFS))
						return true;
				}
				else if (((SimpleFeatureValue)value).IsVariable)
				{
					return true;
				}
			}
			return false;
		}
	}
}
