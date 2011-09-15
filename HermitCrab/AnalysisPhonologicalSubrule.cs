using System;
using System.Linq;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	public class AnalysisPhonologicalSubrule : PatternRuleBase<PhoneticShapeNode>
	{
		private readonly SpanFactory<PhoneticShapeNode> _spanFactory;
		private readonly Expression<PhoneticShapeNode> _analysisLhs;
		private readonly Expression<PhoneticShapeNode> _analysisRhs;
		private readonly Expression<PhoneticShapeNode> _leftEnv;
		private readonly Expression<PhoneticShapeNode> _rightEnv;
		private readonly int _delReapplications;
		private readonly bool _selfOpaquing;

		public AnalysisPhonologicalSubrule(SpanFactory<PhoneticShapeNode> spanFactory, int delReapplications, Direction synthesisDir,
			bool synthesisSimult, Expression<PhoneticShapeNode> lhs, Expression<PhoneticShapeNode> rhs, Expression<PhoneticShapeNode> leftEnv,
			Expression<PhoneticShapeNode> rightEnv)
			: base(new Pattern<PhoneticShapeNode>(spanFactory, synthesisDir == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight,
				ann => ann.FeatureStruct.GetValue<SymbolicFeatureValue>("type").Contains("seg"),
				(input, match) => IsUnapplicationNonvacuous(match, lhs, rhs)), lhs.Children.Count > rhs.Children.Count)
		{
			_spanFactory = spanFactory;
			_leftEnv = leftEnv;
			_rightEnv = rightEnv;
			_delReapplications = delReapplications;
			_analysisLhs = new Expression<PhoneticShapeNode>();
			_analysisRhs = new Expression<PhoneticShapeNode>();


			if (leftEnv.Children.Count > 0)
			{
				if (leftEnv.Children.First is Anchor<PhoneticShapeNode>)
					Lhs.Children.Add(new Anchor<PhoneticShapeNode>(AnchorTypes.LeftSide));
				Lhs.Children.Add(new Group<PhoneticShapeNode>("leftEnv", leftEnv.Children.Where(node => !(node is Anchor<PhoneticShapeNode>)).Clone()));
			}
			if (lhs.Children.Count == 0 || lhs.Children.Count > rhs.Children.Count)
			{
				Lhs.Children.Add(new Group<PhoneticShapeNode>("target", rhs.Children.Clone()));
			}
			else if (lhs.Children.Count == rhs.Children.Count)
			{
				var target = new Group<PhoneticShapeNode>("target");
				foreach (Tuple<PatternNode<PhoneticShapeNode>, PatternNode<PhoneticShapeNode>> tuple in lhs.Children.Zip(rhs.Children))
				{
					var lhsConstraint = (Constraint<PhoneticShapeNode>) tuple.Item1;
					var rhsConstraint = (Constraint<PhoneticShapeNode>) tuple.Item2;

					var targetConstraint = (Constraint<PhoneticShapeNode>) tuple.Item1.Clone();
					targetConstraint.FeatureStruct.Replace(rhsConstraint.FeatureStruct);
					target.Children.Add(targetConstraint);

					var analysisLhsConstraint = (Constraint<PhoneticShapeNode>) targetConstraint.Clone();
					analysisLhsConstraint.FeatureStruct.Subtract(rhsConstraint.FeatureStruct);
					_analysisLhs.Children.Add(analysisLhsConstraint);

					var analysisRhsConstraint = (Constraint<PhoneticShapeNode>) tuple.Item2.Clone();
					analysisRhsConstraint.FeatureStruct.Subtract(lhsConstraint.FeatureStruct);
					_analysisRhs.Children.Add(analysisRhsConstraint);
				}
				Lhs.Children.Add(target);
			}
			if (rightEnv.Children.Count > 0)
			{
				Lhs.Children.Add(new Group<PhoneticShapeNode>("rightEnv", rightEnv.Children.Where(node => !(node is Anchor<PhoneticShapeNode>)).Clone()));
				if (rightEnv.Children.Last is Anchor<PhoneticShapeNode>)
					Lhs.Children.Add(new Anchor<PhoneticShapeNode>(AnchorTypes.RightSide));
			}

			if (lhs.Children.Count > rhs.Children.Count)
			{
				_selfOpaquing = true;
			}
			else if (synthesisSimult)
			{
				if (lhs.Children.Count == rhs.Children.Count)
				{
					foreach (Constraint<PhoneticShapeNode> constraint in rhs.Children)
					{
						if (constraint.FeatureStruct.GetValue<SymbolicFeatureValue>("type").Contains("seg"))
						{
							if (!IsUnifiable(constraint, _leftEnv) || !IsUnifiable(constraint, _rightEnv))
							{
								_selfOpaquing = true;
								break;
							}
						}
					}
				}
				else
				{
					_selfOpaquing = true;
				}
			}
		}

		private static bool IsUnifiable(Constraint<PhoneticShapeNode> constraint, Expression<PhoneticShapeNode> env)
		{
			foreach (Constraint<PhoneticShapeNode> curConstraint in env.GetNodes().OfType<Constraint<PhoneticShapeNode>>())
			{
				if (curConstraint.FeatureStruct.GetValue<SymbolicFeatureValue>("type").Contains("seg")
					&& !curConstraint.FeatureStruct.IsUnifiable(constraint.FeatureStruct))
				{
					return false;
				}
			}

			return true;
		}

		private static bool IsUnapplicationNonvacuous(PatternMatch<PhoneticShapeNode> match, Expression<PhoneticShapeNode> lhs,
			Expression<PhoneticShapeNode> rhs)
		{
			Span<PhoneticShapeNode> target = match["target"];
			if (lhs.Children.Count == 0)
			{
				foreach (PhoneticShapeNode node in target.Start.GetNodes(target.End))
				{
					if (!node.Annotation.IsOptional)
						return true;
				}
			}
			else
			{
				foreach (Tuple<PhoneticShapeNode, PatternNode<PhoneticShapeNode>> tuple in target.Start.GetNodes(target.End).Zip(rhs.Children))
				{
					PhoneticShapeNode node = tuple.Item1;
					var constraint = (Constraint<PhoneticShapeNode>) tuple.Item2;
					if (node.Annotation.FeatureStruct.GetValue<SymbolicFeatureValue>("type").Contains("seg"))
					{
						if (node.Annotation.FeatureStruct.IsUnifiable(constraint.FeatureStruct, match.VariableBindings))
							return true;
					}
				}
			}

			return false;
		}

		public override bool IsApplicable(IBidirList<Annotation<PhoneticShapeNode>> input)
		{
			return true;
		}

		public override bool Apply(IBidirList<Annotation<PhoneticShapeNode>> input)
		{
			if (_analysisLhs.Children.Count > _analysisRhs.Children.Count)
			{
				int i = 0;
				while (i <= _delReapplications && base.Apply(input))
					i++;
				return i > 0;
			}

			bool applied = false;
			if (_selfOpaquing)
			{
				while (base.Apply(input))
				{
					applied = true;
				}
			}
			else
			{
				applied = base.Apply(input);
			}
			return applied;
		}

		public override Annotation<PhoneticShapeNode> ApplyRhs(IBidirList<Annotation<PhoneticShapeNode>> input, PatternMatch<PhoneticShapeNode> match)
		{
			Span<PhoneticShapeNode> target = match["target"];
			if (_analysisLhs.Children.Count == _analysisRhs.Children.Count)
			{
				PhoneticShapeNode node = target.Start;
				foreach (Tuple<PatternNode<PhoneticShapeNode>, PatternNode<PhoneticShapeNode>> tuple in _analysisLhs.Children.Zip(_analysisRhs.Children))
				{
					if (node.Annotation.FeatureStruct.GetValue<SymbolicFeatureValue>("type").Contains("seg"))
					{
						var lhsConstraint = (Constraint<PhoneticShapeNode>) tuple.Item1;
						var rhsConstraint = (Constraint<PhoneticShapeNode>) tuple.Item2;
						node.Annotation.FeatureStruct.Replace(lhsConstraint.FeatureStruct);
						node.Annotation.FeatureStruct.ReplaceVariables(match.VariableBindings);
						node.Annotation.FeatureStruct.Subtract(rhsConstraint.FeatureStruct, match.VariableBindings);
					}
					node = node.Next;
				}
			}
			else if (_analysisLhs.Children.Count == 0)
			{
				foreach (PhoneticShapeNode node in target.Start.GetNodes(match.End))
					node.Annotation.IsOptional = true;
			}
			else if (_analysisLhs.Children.Count > _analysisRhs.Children.Count)
			{
				PhoneticShapeNode curNode = target.End;
				foreach (Constraint<PhoneticShapeNode> constraint in _analysisLhs.Children)
				{
					var newNode = new PhoneticShapeNode(_spanFactory, (FeatureStruct) constraint.FeatureStruct.Clone());
					newNode.Annotation.FeatureStruct.ReplaceVariables(match.VariableBindings);
					newNode.Annotation.IsOptional = true;
					curNode.Insert(newNode, Direction.LeftToRight);
					curNode = newNode;
				}

				if (_analysisRhs.Children.Count > 0)
				{
					foreach (PhoneticShapeNode node in target.Start.GetNodes(match.End))
						node.Annotation.IsOptional = true;
				}
			}

			return match.GetStart(Lhs.Direction).Annotation;
		}
	}
}
