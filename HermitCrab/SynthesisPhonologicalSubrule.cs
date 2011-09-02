using System;
using System.Linq;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	public class SynthesisPhonologicalSubrule : PatternRuleBase<PhoneticShapeNode>
	{
		private readonly SpanFactory<PhoneticShapeNode> _spanFactory; 
		private readonly Expression<PhoneticShapeNode> _lhs;
		private readonly Expression<PhoneticShapeNode> _rhs;
		private readonly FeatureStruct _applicableFs;

		public SynthesisPhonologicalSubrule(SpanFactory<PhoneticShapeNode> spanFactory, Expression<PhoneticShapeNode> lhs,
			Expression<PhoneticShapeNode> rhs, Expression<PhoneticShapeNode> leftEnv, Expression<PhoneticShapeNode> rightEnv,
			Direction dir, bool simult, FeatureStruct applicableFs)
			: base(CreatePattern(spanFactory, lhs, leftEnv, rightEnv, dir), simult)
		{
			_spanFactory = spanFactory;
			_lhs = lhs;
			_rhs = rhs;
			_applicableFs = applicableFs;
		}

		private static Pattern<PhoneticShapeNode> CreatePattern(SpanFactory<PhoneticShapeNode> spanFactory, Expression<PhoneticShapeNode> lhs,
			Expression<PhoneticShapeNode> leftEnv, Expression<PhoneticShapeNode> rightEnv, Direction dir)
		{
			var pattern = new Pattern<PhoneticShapeNode>(spanFactory, dir);
			pattern.Children.Add(new Group<PhoneticShapeNode>("leftEnv", leftEnv.Children.Clone()));
			pattern.Children.Add(new Group<PhoneticShapeNode>("target", lhs.Children.Clone()));
			pattern.Children.Add(new Group<PhoneticShapeNode>("rightEnv", rightEnv.Children.Clone()));
			return pattern;
		}

		public override bool IsApplicable(IBidirList<Annotation<PhoneticShapeNode>> input)
		{
			if (_applicableFs == null)
				return true;

			return input.First.FeatureStruct.IsUnifiable(_applicableFs);
		}

		public override Annotation<PhoneticShapeNode> ApplyRhs(IBidirList<Annotation<PhoneticShapeNode>> input, PatternMatch<PhoneticShapeNode> match)
		{
			Span<PhoneticShapeNode> target = match["target"];
			PhoneticShapeNode endNode = target.GetEnd(Lhs.Direction);
			if (_lhs.Children.Count == _rhs.Children.Count)
			{
				foreach (Tuple<PhoneticShapeNode, PatternNode<PhoneticShapeNode>> tuple in target.Start.GetNodes(target.End).Zip(_rhs.Children))
				{
					var constraints = (Constraint<PhoneticShapeNode>) tuple.Item2;
					tuple.Item1.Annotation.FeatureStruct.AddValues(constraints.FeatureStruct);
					tuple.Item1.Annotation.FeatureStruct.ReplaceVariables(match.VariableBindings);
				}
			}
			else if (_lhs.Children.Count == 0)
			{
				endNode = ApplyInsertion(target);
			}
			else if (_lhs.Children.Count > _rhs.Children.Count)
			{
				endNode = ApplyInsertion(target);
				foreach (PhoneticShapeNode node in target.Start.GetNodes(target.End).ToArray())
					node.Remove();
			}

			return endNode.Annotation;
		}

		private PhoneticShapeNode ApplyInsertion(Span<PhoneticShapeNode> target)
		{
			PhoneticShapeNode cur = target.GetEnd(Lhs.Direction);
			foreach (PatternNode<PhoneticShapeNode> node in _rhs.Children.GetNodes(Lhs.Direction))
			{
				var constraints = (Constraint<PhoneticShapeNode>) node;
				var newNode = new PhoneticShapeNode(_spanFactory, (FeatureStruct) constraints.FeatureStruct.Clone());
				cur.Insert(newNode, Lhs.Direction);
				cur = newNode;
			}
			return cur;
		}
	}
}
