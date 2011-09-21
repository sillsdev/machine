using System;
using System.Linq;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	internal class AnalysisStandardPhonologicalSubrule : PatternRuleBase<PhoneticShapeNode>
	{
		private readonly SpanFactory<PhoneticShapeNode> _spanFactory;
		private readonly Expression<PhoneticShapeNode> _analysisRhs;
		private readonly Expression<PhoneticShapeNode> _lhs;
		private readonly Expression<PhoneticShapeNode> _rhs;
		private readonly Expression<PhoneticShapeNode> _leftEnv;
		private readonly Expression<PhoneticShapeNode> _rightEnv;
		private readonly int _delReapplications;
		private readonly bool _selfOpaquing;

		private readonly StringFeature _searchedFeature;

		public AnalysisStandardPhonologicalSubrule(SpanFactory<PhoneticShapeNode> spanFactory, int delReapplications, Direction synthesisDir,
			bool synthesisSimult, Expression<PhoneticShapeNode> lhs, Expression<PhoneticShapeNode> rhs, Expression<PhoneticShapeNode> leftEnv,
			Expression<PhoneticShapeNode> rightEnv)
			: base(CreatePattern(spanFactory, synthesisDir, lhs, rhs), lhs.Children.Count > rhs.Children.Count)
		{
			_spanFactory = spanFactory;
			_lhs = lhs;
			_rhs = rhs;
			_leftEnv = leftEnv;
			_rightEnv = rightEnv;
			_delReapplications = delReapplications;
			_searchedFeature = new StringFeature(Guid.NewGuid().ToString(), "Searched");

			if (leftEnv.Children.Count > 0)
			{
				if (leftEnv.Children.First is Anchor<PhoneticShapeNode>)
					Lhs.Children.Add(new Anchor<PhoneticShapeNode>(AnchorTypes.LeftSide));
				Lhs.Children.Add(new Group<PhoneticShapeNode>("leftEnv", from node in leftEnv.Children
																		 where !(node is Anchor<PhoneticShapeNode>) && (!(node is Constraint<PhoneticShapeNode>)
																		 || ((Constraint<PhoneticShapeNode>) node).FeatureStruct.GetValue<SymbolicFeatureValue>("type").Contains("seg"))
																		 select node.Clone()));
			}
			if (lhs.Children.Count == 0 || lhs.Children.Count > rhs.Children.Count)
			{
				_analysisRhs = _lhs;
				if (rhs.Children.Count > 0)
					Lhs.Children.Add(new Group<PhoneticShapeNode>("target", rhs.Children.Clone()));
			}
			else if (lhs.Children.Count == rhs.Children.Count)
			{
				_analysisRhs = new Expression<PhoneticShapeNode>();
				int i = 0;
				foreach (Tuple<PatternNode<PhoneticShapeNode>, PatternNode<PhoneticShapeNode>> tuple in lhs.Children.Zip(rhs.Children))
				{
					var lhsConstraint = (Constraint<PhoneticShapeNode>) tuple.Item1;
					var rhsConstraint = (Constraint<PhoneticShapeNode>) tuple.Item2;

					if (lhsConstraint.FeatureStruct.GetValue<SymbolicFeatureValue>("type").Contains("seg")
						&& rhsConstraint.FeatureStruct.GetValue<SymbolicFeatureValue>("type").Contains("seg"))
					{
						var targetConstraint = (Constraint<PhoneticShapeNode>) lhsConstraint.Clone();
						targetConstraint.FeatureStruct.Replace(rhsConstraint.FeatureStruct);
						targetConstraint.FeatureStruct.AddValue(_searchedFeature, new StringFeatureValue("false"));
						Lhs.Children.Add(new Group<PhoneticShapeNode>("target" + i, targetConstraint));

						var fs = GetAntiFeatureStruct(rhsConstraint.FeatureStruct);
						fs.Subtract(GetAntiFeatureStruct(lhsConstraint.FeatureStruct));
						_analysisRhs.Children.Add(new Constraint<PhoneticShapeNode>(fs));

						i++;
					}
				}
			}
			if (rightEnv.Children.Count > 0)
			{
				Lhs.Children.Add(new Group<PhoneticShapeNode>("rightEnv", from node in rightEnv.Children
																		  where !(node is Anchor<PhoneticShapeNode>) && (!(node is Constraint<PhoneticShapeNode>)
																		  || ((Constraint<PhoneticShapeNode>) node).FeatureStruct.GetValue<SymbolicFeatureValue>("type").Contains("seg"))
																		  select node.Clone()));
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

		private static FeatureStruct GetAntiFeatureStruct(FeatureStruct fs)
		{
			var result = new FeatureStruct();
			foreach (Feature feature in fs.Features)
			{
				FeatureValue value = fs.GetValue(feature);
				var childFS = value as FeatureStruct;
				FeatureValue newValue;
				if (childFS != null)
				{
					newValue = GetAntiFeatureStruct(childFS);
				}
				else
				{
					value.Negation(out newValue);
				}
				result.AddValue(feature, newValue);
			}
			return result;
		}

		private static Pattern<PhoneticShapeNode> CreatePattern(SpanFactory<PhoneticShapeNode> spanFactory, Direction synthesisDir,
			Expression<PhoneticShapeNode> lhs, Expression<PhoneticShapeNode> rhs)
		{
			Direction dir;
			if (lhs.Children.Count > rhs.Children.Count || synthesisDir == Direction.RightToLeft)
				dir = Direction.LeftToRight;
			else
				dir = Direction.RightToLeft;

			return new Pattern<PhoneticShapeNode>(spanFactory, dir,
				ann => ann.FeatureStruct.GetValue<SymbolicFeatureValue>("type").Contains("seg"),
				(input, match) => IsUnapplicationNonvacuous(match, lhs, rhs));
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
			Span<PhoneticShapeNode> target;
			if (!match.TryGetGroup("target", out target))
				return true;

			if (lhs.Children.Count == 0)
			{
				foreach (PhoneticShapeNode node in target.Start.GetNodes(target.End))
				{
					if (!node.Annotation.Optional)
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
			if (_lhs.Children.Count > _rhs.Children.Count)
			{
				int i = 0;
				while (i <= _delReapplications && base.Apply(input))
				{
					RemoveSearchedValue(input);
					i++;
				}
				return i > 0;
			}

			bool applied = false;
			if (_selfOpaquing)
			{
				while (base.Apply(input))
				{
					RemoveSearchedValue(input);
					applied = true;
				}
			}
			else
			{
				applied = base.Apply(input);
				RemoveSearchedValue(input);
			}
			return applied;
		}

		private void RemoveSearchedValue(IBidirList<Annotation<PhoneticShapeNode>> input)
		{
			foreach (Annotation<PhoneticShapeNode> ann in input)
				ann.FeatureStruct.RemoveValue(_searchedFeature);
		}

		public override Annotation<PhoneticShapeNode> ApplyRhs(IBidirList<Annotation<PhoneticShapeNode>> input, PatternMatch<PhoneticShapeNode> match)
		{
			if (_lhs.Children.Count == _rhs.Children.Count)
			{
				int i = 0;
				foreach (Constraint<PhoneticShapeNode> constraint in _analysisRhs.Children)
				{
					PhoneticShapeNode node = match["target" + i].GetStart(Lhs.Direction);
					node.Annotation.FeatureStruct.Merge(constraint.FeatureStruct, match.VariableBindings);
					i++;
				}

			}
			else if (_lhs.Children.Count == 0)
			{
				Span<PhoneticShapeNode> target = match["target"];
				foreach (PhoneticShapeNode node in target.Start.GetNodes(target.End))
					node.Annotation.Optional = true;
			}
			else if (_lhs.Children.Count > _rhs.Children.Count)
			{
				PhoneticShape shape;
				PhoneticShapeNode curNode;
				Span<PhoneticShapeNode> target;
				if (match.TryGetGroup("target", out target))
				{
					shape = (PhoneticShape) target.Start.List;
					curNode = target.End;
				}
				else
				{
					Span<PhoneticShapeNode> leftEnv;
					if (match.TryGetGroup("leftEnv", out leftEnv))
					{
						shape = (PhoneticShape) leftEnv.Start.List;
						curNode = leftEnv.End;
					}
					else
					{
						Span<PhoneticShapeNode> rightEnv = match["rightEnv"];
						shape = (PhoneticShape) rightEnv.Start.List;
						curNode = rightEnv.Start.Prev;
					}
				}
				foreach (Constraint<PhoneticShapeNode> constraint in _analysisRhs.Children)
				{
					var newNode = new PhoneticShapeNode(_spanFactory, (FeatureStruct) constraint.FeatureStruct.Clone());
					newNode.Annotation.FeatureStruct.ReplaceVariables(match.VariableBindings);
					newNode.Annotation.Optional = true;
					shape.Insert(newNode, curNode, Direction.LeftToRight);
					curNode = newNode;
				}

				if (_rhs.Children.Count > 0)
				{
					foreach (PhoneticShapeNode node in target.Start.GetNodes(match.End))
						node.Annotation.Optional = true;
				}
			}

			return match.GetStart(Lhs.Direction).Annotation;
		}
	}
}
