using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class FeatureAnalysisRewriteRuleSpec : AnalysisRewriteRuleSpec
	{
		private readonly RewriteSubrule _subrule;
		private readonly Pattern<Word, ShapeNode> _analysisRhs;

		public FeatureAnalysisRewriteRuleSpec(Pattern<Word, ShapeNode> lhs, RewriteSubrule subrule)
		{
			_subrule = subrule;

			var rhsAntiFSs = new List<FeatureStruct>();
			foreach (Constraint<Word, ShapeNode> constraint in _subrule.Rhs.Children.OfType<Constraint<Word, ShapeNode>>().Where(c => c.Type() == HCFeatureSystem.Segment))
				rhsAntiFSs.Add(constraint.FeatureStruct.AntiFeatureStruct());

			Pattern.Acceptable = match => IsUnapplicationNonvacuous(match, rhsAntiFSs);

			_analysisRhs = new Pattern<Word, ShapeNode>();
			AddEnvironment("leftEnv", _subrule.LeftEnvironment);
			int i = 0;
			foreach (Tuple<PatternNode<Word, ShapeNode>, PatternNode<Word, ShapeNode>> tuple in lhs.Children.Zip(_subrule.Rhs.Children))
			{
				var lhsConstraint = (Constraint<Word, ShapeNode>) tuple.Item1;
				var rhsConstraint = (Constraint<Word, ShapeNode>) tuple.Item2;

				if (lhsConstraint.Type() == HCFeatureSystem.Segment && rhsConstraint.Type() == HCFeatureSystem.Segment)
				{
					var targetConstraint = lhsConstraint.DeepClone();
					targetConstraint.FeatureStruct.PriorityUnion(rhsConstraint.FeatureStruct);
					targetConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
					Pattern.Children.Add(new Group<Word, ShapeNode>("target" + i) {Children = {targetConstraint}});

					FeatureStruct fs = rhsConstraint.FeatureStruct.AntiFeatureStruct();
					fs.Subtract(lhsConstraint.FeatureStruct.AntiFeatureStruct());
					fs.AddValue(HCFeatureSystem.Type, HCFeatureSystem.Segment);
					_analysisRhs.Children.Add(new Constraint<Word, ShapeNode>(fs));

					i++;
				}
			}
			AddEnvironment("rightEnv", _subrule.RightEnvironment);
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
				ShapeNode node = match.GroupCaptures["target" + i].Span.GetStart(match.Matcher.Direction);
				if (!node.Annotation.FeatureStruct.IsUnifiable(fs, match.VariableBindings))
					return true;
				i++;
			}

			return false;
		}

		private static bool IsUnifiable(Constraint<Word, ShapeNode> constraint, Pattern<Word, ShapeNode> env)
		{
			foreach (Constraint<Word, ShapeNode> curConstraint in env.GetNodesDepthFirst().OfType<Constraint<Word, ShapeNode>>())
			{
				if (curConstraint.Type() == HCFeatureSystem.Segment
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
				foreach (Constraint<Word, ShapeNode> constraint in _subrule.Rhs.Children)
				{
					if (constraint.Type() == HCFeatureSystem.Segment)
					{
						if (!IsUnifiable(constraint, _subrule.LeftEnvironment) || !IsUnifiable(constraint, _subrule.RightEnvironment))
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
				ShapeNode node = match.GroupCaptures["target" + i].Span.GetStart(match.Matcher.Direction);
				if (endNode == null || node.CompareTo(endNode, match.Matcher.Direction) > 0)
					endNode = node;
				FeatureStruct fs = node.Annotation.FeatureStruct.DeepClone();
				fs.PriorityUnion(constraint.FeatureStruct);
				node.Annotation.FeatureStruct.Union(fs, match.VariableBindings);
				node.SetDirty(true);
				i++;
			}

			output = match.Input;
			return match.Span.GetStart(match.Matcher.Direction).GetNext(match.Matcher.Direction);
		}
	}
}
