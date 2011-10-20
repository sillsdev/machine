using System;
using System.Collections.Generic;
using System.Linq;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	public class FeatureAnalysisRewriteRule : AnalysisRewriteRule
	{
		private readonly Expression<Word, ShapeNode> _analysisRhs;
		private readonly AnalysisReapplyType _reapplyType;

		public FeatureAnalysisRewriteRule(SpanFactory<ShapeNode> spanFactory, Direction synthesisDir, ApplicationMode synthesisAppMode,
			Expression<Word, ShapeNode> lhs, Expression<Word, ShapeNode> rhs, Expression<Word, ShapeNode> leftEnv, Expression<Word, ShapeNode> rightEnv)
			: base(spanFactory, synthesisDir == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight, ApplicationMode.Iterative,
				CreateAcceptable(rhs, synthesisDir))
		{
			_analysisRhs = new Expression<Word, ShapeNode>();
			AddEnvironment("leftEnv", leftEnv);
			int i = 0;
			foreach (Tuple<PatternNode<Word, ShapeNode>, PatternNode<Word, ShapeNode>> tuple in lhs.Children.Zip(rhs.Children))
			{
				var lhsConstraint = (Constraint<Word, ShapeNode>)tuple.Item1;
				var rhsConstraint = (Constraint<Word, ShapeNode>)tuple.Item2;

				if (lhsConstraint.Type == HCFeatureSystem.SegmentType && rhsConstraint.Type == HCFeatureSystem.SegmentType)
				{
					var targetConstraint = (Constraint<Word, ShapeNode>)lhsConstraint.Clone();
					targetConstraint.FeatureStruct.PriorityUnion(rhsConstraint.FeatureStruct);
					targetConstraint.FeatureStruct.AddValue(HCFeatureSystem.Backtrack, HCFeatureSystem.NotSearched);
					Lhs.Children.Add(new Group<Word, ShapeNode>("target" + i, targetConstraint));

					var fs = GetAntiFeatureStruct(rhsConstraint.FeatureStruct);
					fs.Subtract(GetAntiFeatureStruct(lhsConstraint.FeatureStruct));
					_analysisRhs.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.SegmentType, fs));

					i++;
				}
			}
			AddEnvironment("rightEnv", rightEnv);

			if (synthesisAppMode == ApplicationMode.Simultaneous)
			{
				foreach (Constraint<Word, ShapeNode> constraint in rhs.Children)
				{
					if (constraint.Type == HCFeatureSystem.SegmentType)
					{
						if (!IsUnifiable(constraint, leftEnv) || !IsUnifiable(constraint, rightEnv))
						{
							_reapplyType = AnalysisReapplyType.SelfOpaquing;
							break;
						}
					}
				}
			}
		}

		private static Func<Word, PatternMatch<ShapeNode>, bool> CreateAcceptable(Expression<Word, ShapeNode> rhs, Direction synthesisDir)
		{
			var rhsAntiFSs = new List<FeatureStruct>();
			foreach (Constraint<Word, ShapeNode> constraint in rhs.Children.OfType<Constraint<Word, ShapeNode>>().Where(c => c.Type == HCFeatureSystem.SegmentType))
			{
				FeatureStruct fs = GetAntiFeatureStruct(constraint.FeatureStruct);
				fs.RemoveValue(AnnotationFeatureSystem.Type);
				rhsAntiFSs.Add(fs);
			}
			Direction dir = synthesisDir == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight;
			return (input, match) => IsUnapplicationNonvacuous(match, rhsAntiFSs, dir);
		}

		private static bool IsUnapplicationNonvacuous(PatternMatch<ShapeNode> match, IEnumerable<FeatureStruct> rhsAntiFSs, Direction dir)
		{
			int i = 0;
			foreach (FeatureStruct fs in rhsAntiFSs)
			{
				ShapeNode node = match["target" + i].GetStart(dir);
				if (!node.Annotation.FeatureStruct.IsUnifiable(fs, match.VariableBindings))
					return true;
				i++;
			}

			return false;
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

		private static bool IsUnifiable(Constraint<Word, ShapeNode> constraint, Expression<Word, ShapeNode> env)
		{
			foreach (Constraint<Word, ShapeNode> curConstraint in env.GetNodes().OfType<Constraint<Word, ShapeNode>>())
			{
				if (curConstraint.Type == HCFeatureSystem.SegmentType
					&& !curConstraint.FeatureStruct.IsUnifiable(constraint.FeatureStruct))
				{
					return false;
				}
			}

			return true;
		}

		public override AnalysisReapplyType AnalysisReapplyType
		{
			get { return _reapplyType; }
		}

		public override Annotation<ShapeNode> ApplyRhs(Word input, PatternMatch<ShapeNode> match, out Word output)
		{
			ShapeNode endNode = null;
			int i = 0;
			foreach (Constraint<Word, ShapeNode> constraint in _analysisRhs.Children)
			{
				ShapeNode node = match["target" + i].GetStart(Lhs.Direction);
				if (endNode == null || node.CompareTo(endNode, Lhs.Direction) > 0)
					endNode = node;
				node.Annotation.FeatureStruct.Merge(constraint.FeatureStruct, match.VariableBindings);
				i++;
			}

			ShapeNode resumeNode = match.GetStart(Lhs.Direction).GetNext(Lhs.Direction);
			MarkSearchedNodes(resumeNode, endNode);

			output = input;
			return resumeNode.Annotation;
		}
	}
}
