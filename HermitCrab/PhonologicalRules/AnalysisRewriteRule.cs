using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class AnalysisRewriteRule : IRule<Word, ShapeNode>
	{
		private enum ReapplyType
		{
			Normal,
			Deletion,
			SelfOpaquing
		}

		private readonly Morpher _morpher;
		private readonly RewriteRule _rule;
		private readonly List<Tuple<ReapplyType, BacktrackingPatternRule>> _rules;  

		public AnalysisRewriteRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, RewriteRule rule)
		{
			_morpher = morpher;
			_rule = rule;

			_rules = new List<Tuple<ReapplyType, BacktrackingPatternRule>>();
			foreach (RewriteSubrule sr in _rule.Subrules)
			{
				AnalysisRewriteRuleSpec ruleSpec = null;
				ApplicationMode mode = ApplicationMode.Iterative;
				ReapplyType reapplyType = ReapplyType.Normal;
				if (_rule.Lhs.Children.Count == sr.Rhs.Children.Count)
				{
					ruleSpec = new FeatureAnalysisRewriteRuleSpec(rule.Lhs, sr);
					if (_rule.ApplicationMode == ApplicationMode.Simultaneous)
					{
						foreach (Constraint<Word, ShapeNode> constraint in sr.Rhs.Children)
						{
							if (constraint.Type() == HCFeatureSystem.Segment)
							{
								if (!IsUnifiable(constraint, sr.LeftEnvironment) || !IsUnifiable(constraint, sr.RightEnvironment))
								{
									reapplyType = ReapplyType.SelfOpaquing;
									break;
								}
							}
						}
					}
				}
				else if (_rule.Lhs.Children.Count > sr.Rhs.Children.Count)
				{
					ruleSpec = new NarrowAnalysisRewriteRuleSpec(_rule.Lhs, sr);
					mode = ApplicationMode.Simultaneous;
					reapplyType = ReapplyType.Deletion;
				}
				else if (_rule.Lhs.Children.Count == 0)
				{
					ruleSpec = new EpenthesisAnalysisRewriteRuleSpec(sr);
					if (_rule.ApplicationMode == ApplicationMode.Simultaneous)
						reapplyType = ReapplyType.SelfOpaquing;
				}
				Debug.Assert(ruleSpec != null);

				var patternRule = new BacktrackingPatternRule(spanFactory, ruleSpec, mode,
					new MatcherSettings<ShapeNode>
						{
							Direction = rule.Direction == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight,
							Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Anchor)
						});
				_rules.Add(Tuple.Create(reapplyType, patternRule));
			}
		}

		private static bool IsUnifiable(Constraint<Word, ShapeNode> constraint, Pattern<Word, ShapeNode> env)
		{
			foreach (Constraint<Word, ShapeNode> curConstraint in env.GetNodesDepthFirst().OfType<Constraint<Word, ShapeNode>>())
			{
				if (curConstraint.Type() == HCFeatureSystem.Segment && !curConstraint.FeatureStruct.IsUnifiable(constraint.FeatureStruct))
				{
					return false;
				}
			}

			return true;
		}

		public bool IsApplicable(Word input)
		{
			return true;
		}

		public IEnumerable<Word> Apply(Word input)
		{
			Trace trace = null;
			if (_morpher.TraceRules.Contains(_rule))
			{
				trace = new Trace(TraceType.PhonologicalRuleAnalysis, _rule) { Input = input.DeepClone() };
				input.CurrentTrace.Children.Add(trace);
			}

			bool applied = false;
			foreach (Tuple<ReapplyType, BacktrackingPatternRule> sr in _rules)
			{
				switch (sr.Item1)
				{
					case ReapplyType.Normal:
						{
							if (sr.Item2.Apply(input).Any())
								applied = true;
						}
						break;

					case ReapplyType.Deletion:
						{
							int i = 0;
							Word data = sr.Item2.Apply(input).SingleOrDefault();
							while (data != null)
							{
								applied = true;
								i++;
								if (i > _morpher.DeletionReapplications)
									break;
								data = sr.Item2.Apply(data).SingleOrDefault();
							}
						}
						break;

					case ReapplyType.SelfOpaquing:
						{
							Word data = sr.Item2.Apply(input).SingleOrDefault();
							while (data != null)
							{
								applied = true;
								data = sr.Item2.Apply(data).SingleOrDefault();
							}
						}
						break;
				}
			}

			if (trace != null)
				trace.Output = input.DeepClone();

			if (applied)
				return input.ToEnumerable();
			return Enumerable.Empty<Word>();
		}
	}
}
