using System;
using System.Collections.Generic;
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
		private readonly Expression<PhoneticShapeNode> _lhs;
		private readonly Expression<PhoneticShapeNode> _rhs;
		private readonly Expression<PhoneticShapeNode> _leftEnv;
		private readonly Expression<PhoneticShapeNode> _rightEnv;
		private readonly int _delReapplications;
		private readonly bool _selfOpaquing;

		public AnalysisPhonologicalSubrule(SpanFactory<PhoneticShapeNode> spanFactory, Expression<PhoneticShapeNode> lhs,
			Expression<PhoneticShapeNode> rhs, Expression<PhoneticShapeNode> leftEnv, Expression<PhoneticShapeNode> rightEnv,
			Direction synthesisDir, bool synthesisSimult, int delReapplications)
			: base(CreatePattern(spanFactory, lhs, rhs, leftEnv, rightEnv, synthesisDir), lhs.Children.Count > rhs.Children.Count)
		{
			_spanFactory = spanFactory;
			_lhs = lhs;
			_rhs = rhs;
			_leftEnv = leftEnv;
			_rightEnv = rightEnv;
			_delReapplications = delReapplications;

			if (_lhs.Children.Count > _rhs.Children.Count)
			{
				_selfOpaquing = true;
			}
			else if (synthesisSimult)
			{
				if (_lhs.Children.Count == _rhs.Children.Count)
				{
					foreach (Constraint<PhoneticShapeNode> constraint in _rhs.Children.OfType<Constraint<PhoneticShapeNode>>())
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

		private static Pattern<PhoneticShapeNode> CreatePattern(SpanFactory<PhoneticShapeNode> spanFactory, Expression<PhoneticShapeNode> lhs,
			Expression<PhoneticShapeNode> rhs, Expression<PhoneticShapeNode> leftEnv, Expression<PhoneticShapeNode> rightEnv, Direction dir)
		{
			var pattern = new Pattern<PhoneticShapeNode>(spanFactory, dir == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight,
				ann => ann.FeatureStruct.GetValue<SymbolicFeatureValue>("type").Overlaps(new[] {"seg", "bdry"}),
				(input, match) => IsUnapplicationNonvacuous(match, lhs, rhs));
			pattern.Children.Add(new Group<PhoneticShapeNode>("leftEnv", leftEnv.Children.Clone()));
			if (lhs.Children.Count == 0 || lhs.Children.Count > rhs.Children.Count)
			{
				pattern.Children.Add(new Group<PhoneticShapeNode>("target", rhs.Children.Clone()));
			}
			else if (lhs.Children.Count == rhs.Children.Count)
			{
				IEnumerable<Constraint<PhoneticShapeNode>> lhsConstraints = lhs.Children.OfType<Constraint<PhoneticShapeNode>>();
				IEnumerable<Constraint<PhoneticShapeNode>> rhsConstraints = rhs.Children.OfType<Constraint<PhoneticShapeNode>>();
				pattern.Children.Add(new Group<PhoneticShapeNode>("target", lhsConstraints.Zip(rhsConstraints,
					(lhsNode, rhsNode) =>
						{
							var newNode = (Constraint<PhoneticShapeNode>) lhsNode.Clone();
							newNode.FeatureStruct.AddValues(rhsNode.FeatureStruct);
							return (PatternNode<PhoneticShapeNode>) newNode;
						})));
			}
			pattern.Children.Add(new Group<PhoneticShapeNode>("rightEnv", rightEnv.Children.Clone()));
			return pattern;
		}

		private static bool IsUnapplicationNonvacuous(PatternMatch<PhoneticShapeNode> match, Expression<PhoneticShapeNode> lhs,
			Expression<PhoneticShapeNode> rhs)
		{
			Span<PhoneticShapeNode> target = match["target"];
			foreach (Tuple<PhoneticShapeNode, PatternNode<PhoneticShapeNode>> tuple in target.Start.GetNodes(target.End).Zip(rhs.Children))
			{
				if (lhs.Children.Count == 0)
				{
					if (!tuple.Item1.Annotation.IsOptional)
						return true;
				}
				else
				{
					var constraint = tuple.Item2 as Constraint<PhoneticShapeNode>;
					if (constraint != null && constraint.FeatureStruct.GetValue<SymbolicFeatureValue>("type").Contains("seg"))
					{
						if (!tuple.Item1.Annotation.FeatureStruct.IsUnifiable(constraint.FeatureStruct, match.VariableBindings))
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
			if (_lhs.Children.Count > _rhs.Children.Count)
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
			if (_lhs.Children.Count == _rhs.Children.Count)
			{
				PhoneticShapeNode node = target.Start;
				IEnumerable<Constraint<PhoneticShapeNode>> lhsConstraints = _lhs.Children.OfType<Constraint<PhoneticShapeNode>>();
				IEnumerable<Constraint<PhoneticShapeNode>> rhsConstraints = _rhs.Children.OfType<Constraint<PhoneticShapeNode>>();
				foreach (Tuple<Constraint<PhoneticShapeNode>, Constraint<PhoneticShapeNode>> tuple in rhsConstraints.Zip(lhsConstraints))
				{
					if (tuple.Item1.FeatureStruct.GetValue<SymbolicFeatureValue>("type").Contains("seg"))
					{
						node.Annotation.FeatureStruct.MergeValues(tuple.Item1.FeatureStruct);
						node.Annotation.FeatureStruct.AddValues(tuple.Item2.FeatureStruct);
						node.Annotation.FeatureStruct.ReplaceVariables(match.VariableBindings);
					}
					node = node.Next;
				}
			}
			else if (_lhs.Children.Count == 0)
			{
				foreach (PhoneticShapeNode node in target.Start.GetNodes(match.End))
					node.Annotation.IsOptional = true;
			}
			else if (_lhs.Children.Count > _rhs.Children.Count)
			{
				PhoneticShapeNode curNode = target.End;
				foreach (Constraint<PhoneticShapeNode> constraint in _lhs.Children.OfType<Constraint<PhoneticShapeNode>>())
				{
					var newNode = new PhoneticShapeNode(_spanFactory, (FeatureStruct) constraint.FeatureStruct.Clone());
					newNode.Annotation.FeatureStruct.ReplaceVariables(match.VariableBindings);
					newNode.Annotation.IsOptional = true;
					curNode.Insert(newNode, Direction.LeftToRight);
					curNode = newNode;
				}

				if (_rhs.Children.Count > 0)
				{
					foreach (PhoneticShapeNode node in target.Start.GetNodes(match.End))
						node.Annotation.IsOptional = true;
				}
			}

			return input.GetView(target).GetLast(Lhs.Direction);
		}
	}
}
