using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class AnalysisRewriteRule : RuleCascade<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly RewriteRule _rule;

		public AnalysisRewriteRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, RewriteRule rule)
			: base(CreateRules(spanFactory, rule))
		{
			_morpher = morpher;
			_rule = rule;
		}

		private static IEnumerable<IRule<Word, ShapeNode>> CreateRules(SpanFactory<ShapeNode> spanFactory, RewriteRule rule)
		{
			foreach (RewriteSubrule sr in rule.Subrules)
			{
				AnalysisRewriteRuleSpec ruleSpec = null;
				if (rule.Lhs.Children.Count == sr.Rhs.Children.Count)
					ruleSpec = new FeatureAnalysisRewriteRuleSpec(rule.Lhs, sr);
				else if (rule.Lhs.Children.Count > sr.Rhs.Children.Count)
					ruleSpec = new NarrowAnalysisRewriteRuleSpec(rule.Lhs, sr);
				else if (rule.Lhs.Children.Count == 0)
					ruleSpec = new EpenthesisAnalysisRewriteRuleSpec(sr);
				Debug.Assert(ruleSpec != null);

				var patternRule = new BacktrackingPatternRule(spanFactory, ruleSpec, ruleSpec.ApplicationMode,
					new MatcherSettings<ShapeNode>
						{
							Direction = rule.Direction == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight,
							Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Anchor)
						});
				yield return patternRule;
			}
		}

		public override IEnumerable<Word> Apply(Word input)
		{
			Trace trace = null;
			if (_morpher.GetTraceRule(_rule))
			{
				trace = new Trace(TraceType.PhonologicalRuleAnalysis, _rule) { Input = input.DeepClone() };
				input.CurrentTrace.Children.Add(trace);
			}

			IEnumerable<Word> output = base.Apply(input);

			if (trace != null)
				trace.Output = input.DeepClone();

			return output;
		}

		protected override IEnumerable<Word> ApplyRule(IRule<Word, ShapeNode> rule, int index, Word input)
		{
			var rewriteRule = (PatternRule<Word, ShapeNode>) rule;
			switch (((AnalysisRewriteRuleSpec) rewriteRule.RuleSpec).GetAnalysisReapplyType(_rule.ApplicationMode))
			{
				case AnalysisReapplyType.Normal:
					{
						Word output = rewriteRule.Apply(input).SingleOrDefault();
						if (output != null)
							yield return output;
					}
					break;

				case AnalysisReapplyType.Deletion:
					{
						Word output = null;
						int i = 0;
						Word data = rewriteRule.Apply(input).SingleOrDefault();
						while (data != null)
						{
							output = data;
							i++;
							if (i > _rule.DelReapplications)
								break;
							data = rewriteRule.Apply(data).SingleOrDefault();
						}
						if (output != null)
							yield return output;
					}
					break;

				case AnalysisReapplyType.SelfOpaquing:
					{
						Word output = null;
						Word data = rewriteRule.Apply(input).SingleOrDefault();
						while (data != null)
						{
							output = data;
							data = rewriteRule.Apply(data).SingleOrDefault();
						}
						if (output != null)
							yield return output;
					}
					break;
			}
		}
	}
}
