using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class FeatureAnalysisRewriteRuleSpec : AnalysisRewriteRuleSpec
	{
		private readonly Pattern<Word, ShapeNode> _rhs;
		private readonly Pattern<Word, ShapeNode> _leftEnv;
		private readonly Pattern<Word, ShapeNode> _rightEnv;
		private readonly Pattern<Word, ShapeNode> _analysisRhs;

		public FeatureAnalysisRewriteRuleSpec(Pattern<Word, ShapeNode> lhs, Pattern<Word, ShapeNode> rhs,
			Pattern<Word, ShapeNode> leftEnv, Pattern<Word, ShapeNode> rightEnv)
		{
			_rhs = rhs;
			_leftEnv = leftEnv;
			_rightEnv = rightEnv;

			var rhsAntiFSs = new List<FeatureStruct>();
			foreach (Constraint<Word, ShapeNode> constraint in rhs.Children.OfType<Constraint<Word, ShapeNode>>().Where(c => c.Type == HCFeatureSystem.SegmentType))
			{
				FeatureStruct fs = GetAntiFeatureStruct(constraint.FeatureStruct);
				fs.RemoveValue(AnnotationFeatureSystem.Type);
				rhsAntiFSs.Add(fs);
			}
			Pattern.Acceptable = match => IsUnapplicationNonvacuous(match, rhsAntiFSs);

			_analysisRhs = new Pattern<Word, ShapeNode>();
			AddEnvironment("leftEnv", leftEnv);
			int i = 0;
			foreach (Tuple<PatternNode<Word, ShapeNode>, PatternNode<Word, ShapeNode>> tuple in lhs.Children.Zip(rhs.Children))
			{
				var lhsConstraint = (Constraint<Word, ShapeNode>) tuple.Item1;
				var rhsConstraint = (Constraint<Word, ShapeNode>) tuple.Item2;

				if (lhsConstraint.Type == HCFeatureSystem.SegmentType && rhsConstraint.Type == HCFeatureSystem.SegmentType)
				{
					var targetConstraint = (Constraint<Word, ShapeNode>)lhsConstraint.Clone();
					targetConstraint.FeatureStruct.PriorityUnion(rhsConstraint.FeatureStruct);
					targetConstraint.FeatureStruct.AddValue(HCFeatureSystem.Backtrack, HCFeatureSystem.NotSearched);
					Pattern.Children.Add(new Group<Word, ShapeNode>("target" + i) {Children = {targetConstraint}});

					var fs = GetAntiFeatureStruct(rhsConstraint.FeatureStruct);
					fs.Subtract(GetAntiFeatureStruct(lhsConstraint.FeatureStruct));
					_analysisRhs.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.SegmentType, fs));

					i++;
				}
			}
			AddEnvironment("rightEnv", rightEnv);
		}

		public override ApplicationMode ApplicationMode
		{
			get { return ApplicationMode.Iterative; }
		}

		private bool IsUnapplicationNonvacuous(Match<Word, ShapeNode> match, IEnumerable<FeatureStruct> rhsAntiFSs)
		{
			int i = 0;
			foreach (FeatureStruct fs in rhsAntiFSs)
			{
				ShapeNode node = match["target" + i].Span.GetStart(match.Matcher.Direction);
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
					newValue = GetAntiFeatureStruct(childFS);
				else
					newValue = ((SimpleFeatureValue) value).Negation();
				result.AddValue(feature, newValue);
			}
			return result;
		}

		private static bool IsUnifiable(Constraint<Word, ShapeNode> constraint, Pattern<Word, ShapeNode> env)
		{
			foreach (Constraint<Word, ShapeNode> curConstraint in env.GetNodesDepthFirst().OfType<Constraint<Word, ShapeNode>>())
			{
				if (curConstraint.Type == HCFeatureSystem.SegmentType
					&& !curConstraint.FeatureStruct.IsUnifiable(constraint.FeatureStruct))
				{
					return false;
				}
			}

			return true;
		}

		public override AnalysisReapplyType GetAnalysisReapplyType(ApplicationMode synthesisAppMode)
		{
			if (synthesisAppMode == ApplicationMode.Simultaneous)
			{
				foreach (Constraint<Word, ShapeNode> constraint in _rhs.Children)
				{
					if (constraint.Type == HCFeatureSystem.SegmentType)
					{
						if (!IsUnifiable(constraint, _leftEnv) || !IsUnifiable(constraint, _rightEnv))
							return AnalysisReapplyType.SelfOpaquing;
					}
				}
			}
			return AnalysisReapplyType.Normal;
		}

		public override ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
		{
			ShapeNode endNode = null;
			int i = 0;
			foreach (Constraint<Word, ShapeNode> constraint in _analysisRhs.Children)
			{
				ShapeNode node = match["target" + i].Span.GetStart(match.Matcher.Direction);
				if (endNode == null || node.CompareTo(endNode, match.Matcher.Direction) > 0)
					endNode = node;
				node.Annotation.FeatureStruct.Union(constraint.FeatureStruct, match.VariableBindings);
				i++;
			}

			ShapeNode resumeNode = match.Span.GetStart(match.Matcher.Direction).GetNext(match.Matcher.Direction);
			MarkSearchedNodes(resumeNode, endNode, match.Matcher.Direction);

			output = match.Input;
			return resumeNode;
		}
	}
}
