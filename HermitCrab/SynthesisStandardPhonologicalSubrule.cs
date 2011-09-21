using System;
using System.Linq;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	internal class SynthesisStandardPhonologicalSubrule : PatternRuleBase<PhoneticShapeNode>
	{
		private readonly SpanFactory<PhoneticShapeNode> _spanFactory; 
		private readonly Expression<PhoneticShapeNode> _lhs;
		private readonly Expression<PhoneticShapeNode> _rhs;
		private readonly FeatureStruct _applicableFS;
		private readonly StringFeature _searchedFeature;

		public SynthesisStandardPhonologicalSubrule(SpanFactory<PhoneticShapeNode> spanFactory, Direction dir, bool simult, Expression<PhoneticShapeNode> lhs,
			Expression<PhoneticShapeNode> rhs, Expression<PhoneticShapeNode> leftEnv, Expression<PhoneticShapeNode> rightEnv,
			FeatureStruct applicableFS, StringFeature searchedFeature)
			: base(new Pattern<PhoneticShapeNode>(spanFactory, dir, ann => true, (input, match) => CheckTarget(match, lhs)), simult)
		{
			_spanFactory = spanFactory;
			_lhs = lhs;
			_rhs = rhs;
			_applicableFS = applicableFS;
			_searchedFeature = searchedFeature;

			if (leftEnv.Children.Count > 0)
			{
				if (leftEnv.Children.First is Anchor<PhoneticShapeNode>)
					Lhs.Children.Add(new Anchor<PhoneticShapeNode>(AnchorTypes.LeftSide));
				Lhs.Children.Add(new Group<PhoneticShapeNode>("leftEnv", leftEnv.Children.Where(node => !(node is Anchor<PhoneticShapeNode>)).Clone()));
			}

			var target = new Group<PhoneticShapeNode>("target");
			foreach (Constraint<PhoneticShapeNode> constraint in lhs.Children)
			{
				var newConstraint = (Constraint<PhoneticShapeNode>) constraint.Clone();
				newConstraint.FeatureStruct.AddValue(_searchedFeature, new StringFeatureValue("false"));
				target.Children.Add(newConstraint);
			}
			Lhs.Children.Add(target);
			if (rightEnv.Children.Count > 0)
			{
				Lhs.Children.Add(new Group<PhoneticShapeNode>("rightEnv", rightEnv.Children.Where(node => !(node is Anchor<PhoneticShapeNode>)).Clone()));
				if (rightEnv.Children.Last is Anchor<PhoneticShapeNode>)
					Lhs.Children.Add(new Anchor<PhoneticShapeNode>(AnchorTypes.RightSide));
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
					if (!tuple.Item1.Annotation.FeatureStruct.GetValue<SymbolicFeatureValue>("type").Equals(constraints.FeatureStruct.GetValue<SymbolicFeatureValue>("type")))
						return false;
				}
			}
			return true;
		}

		public override bool IsApplicable(IBidirList<Annotation<PhoneticShapeNode>> input)
		{
			return input.First.FeatureStruct.IsUnifiable(_applicableFS);
		}

		public override Annotation<PhoneticShapeNode> ApplyRhs(IBidirList<Annotation<PhoneticShapeNode>> input, PatternMatch<PhoneticShapeNode> match)
		{
			PhoneticShapeNode endNode = null;
			if (_lhs.Children.Count == _rhs.Children.Count)
			{
				Span<PhoneticShapeNode> target = match["target"];
				foreach (Tuple<PhoneticShapeNode, PatternNode<PhoneticShapeNode>> tuple in target.Start.GetNodes(target.End).Zip(_rhs.Children))
				{
					var constraints = (Constraint<PhoneticShapeNode>) tuple.Item2;
					tuple.Item1.Annotation.FeatureStruct.Replace(constraints.FeatureStruct);
					tuple.Item1.Annotation.FeatureStruct.ReplaceVariables(match.VariableBindings);
					if (HasVariable(tuple.Item1.Annotation.FeatureStruct))
						throw new MorphException(MorphException.MorphErrorType.UninstantiatedFeature);
				}
				endNode = target.GetEnd(Lhs.Direction);
			}
			else if (_lhs.Children.Count == 0)
			{
				PhoneticShape shape;
				PhoneticShapeNode curNode;
				Span<PhoneticShapeNode> leftEnv;
				if (match.TryGetGroup("leftEnv", out leftEnv))
				{
					shape = (PhoneticShape)leftEnv.Start.List;
					curNode = leftEnv.End;
				}
				else
				{
					Span<PhoneticShapeNode> rightEnv = match["rightEnv"];
					shape = (PhoneticShape)rightEnv.Start.List;
					curNode = rightEnv.Start.Prev;
				}
				endNode = ApplyInsertion(shape, curNode, match.VariableBindings);
			}
			else if (_lhs.Children.Count > _rhs.Children.Count)
			{
				Span<PhoneticShapeNode> target = match["target"];
				PhoneticShapeNode curNode = target.GetEnd(Lhs.Direction);
				endNode = ApplyInsertion((PhoneticShape) curNode.List, curNode, match.VariableBindings);
				PhoneticShapeNode[] nodes = target.GetStart(Lhs.Direction).GetNodes(target.GetEnd(Lhs.Direction)).ToArray();
				for (int i = 0; i < _lhs.Children.Count; i++)
					nodes[i].Remove();
			}



			if (endNode != null)
			{
				foreach (PhoneticShapeNode node in match.GetStart(Lhs.Direction).GetNodes(endNode, Lhs.Direction))
					node.Annotation.FeatureStruct.AddValue(_searchedFeature, new StringFeatureValue("true"));
			}

			return match.GetStart(Lhs.Direction).Annotation;
		}

		private PhoneticShapeNode ApplyInsertion(PhoneticShape shape, PhoneticShapeNode cur, VariableBindings varBindings)
		{
			foreach (PatternNode<PhoneticShapeNode> node in _rhs.Children.GetNodes(Lhs.Direction))
			{
				var constraints = (Constraint<PhoneticShapeNode>) node;
				var newNode = new PhoneticShapeNode(_spanFactory, (FeatureStruct) constraints.FeatureStruct.Clone());
				newNode.Annotation.FeatureStruct.ReplaceVariables(varBindings);
				if (HasVariable(newNode.Annotation.FeatureStruct))
					throw new MorphException(MorphException.MorphErrorType.UninstantiatedFeature);
				shape.Insert(newNode, cur, Lhs.Direction);
				cur = newNode;
			}
			return cur;
		}

		private bool HasVariable(FeatureStruct fs)
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
