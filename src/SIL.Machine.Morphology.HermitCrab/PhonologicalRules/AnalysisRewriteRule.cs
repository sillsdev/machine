using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
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
        private readonly List<Tuple<ReapplyType, PhonologicalPatternRule>> _rules;

        public AnalysisRewriteRule(Morpher morpher, RewriteRule rule)
        {
            _morpher = morpher;
            _rule = rule;

            var settings = new MatcherSettings<ShapeNode>
            {
                Direction = rule.Direction == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight,
                Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Anchor),
                MatchingMethod = MatchingMethod.Unification,
                UseDefaults = true,
                // during analysis shape nodes can have features that are underspecified, so this must be non-deterministic
                Nondeterministic = true
            };

            _rules = new List<Tuple<ReapplyType, PhonologicalPatternRule>>();
            foreach (RewriteSubrule sr in _rule.Subrules)
            {
                IPhonologicalPatternRuleSpec ruleSpec = null;
                RewriteApplicationMode mode = RewriteApplicationMode.Iterative;
                ReapplyType reapplyType = ReapplyType.Normal;
                if (_rule.Lhs.Children.Count == sr.Rhs.Children.Count)
                {
                    ruleSpec = new FeatureAnalysisRewriteRuleSpec(settings, rule.Lhs, sr);
                    if (_rule.ApplicationMode == RewriteApplicationMode.Simultaneous)
                    {
                        foreach (
                            Constraint<Word, ShapeNode> constraint in sr.Rhs.Children.Cast<
                                Constraint<Word, ShapeNode>
                            >()
                        )
                        {
                            if (constraint.Type() == HCFeatureSystem.Segment)
                            {
                                if (
                                    !IsUnifiable(constraint, sr.LeftEnvironment)
                                    || !IsUnifiable(constraint, sr.RightEnvironment)
                                )
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
                    ruleSpec = new NarrowAnalysisRewriteRuleSpec(settings, _rule.Lhs, sr);
                    mode = RewriteApplicationMode.Simultaneous;
                    reapplyType = ReapplyType.Deletion;
                }
                else if (_rule.Lhs.Children.Count == 0)
                {
                    ruleSpec = new EpenthesisAnalysisRewriteRuleSpec(settings, sr);
                    if (_rule.ApplicationMode == RewriteApplicationMode.Simultaneous)
                        reapplyType = ReapplyType.SelfOpaquing;
                }
                Debug.Assert(ruleSpec != null);

                PhonologicalPatternRule patternRule = null;
                switch (mode)
                {
                    case RewriteApplicationMode.Iterative:
                        patternRule = new IterativePhonologicalPatternRule(ruleSpec, settings);
                        break;

                    case RewriteApplicationMode.Simultaneous:
                        patternRule = new SimultaneousPhonologicalPatternRule(ruleSpec, settings);
                        break;
                }

                _rules.Add(Tuple.Create(reapplyType, patternRule));
            }
        }

        private static bool IsUnifiable(Constraint<Word, ShapeNode> constraint, Pattern<Word, ShapeNode> env)
        {
            foreach (
                Constraint<Word, ShapeNode> curConstraint in env.GetNodesDepthFirst()
                    .OfType<Constraint<Word, ShapeNode>>()
            )
            {
                if (
                    curConstraint.Type() == HCFeatureSystem.Segment
                    && !curConstraint.FeatureStruct.IsUnifiable(constraint.FeatureStruct)
                )
                {
                    return false;
                }
            }

            return true;
        }

        public IEnumerable<Word> Apply(Word input)
        {
            if (!_morpher.RuleSelector(_rule))
                return Enumerable.Empty<Word>();

            bool applied = false;
            for (int i = 0; i < _rules.Count; i++)
            {
                Word origInput = null;
                if (_morpher.TraceManager.IsTracing)
                    origInput = input.Clone();

                Tuple<ReapplyType, PhonologicalPatternRule> sr = _rules[i];
                bool srApplied = false;
                switch (sr.Item1)
                {
                    case ReapplyType.Normal:

                        {
                            if (sr.Item2.Apply(input).Any())
                                srApplied = true;
                        }
                        break;

                    case ReapplyType.Deletion:

                        {
                            int j = 0;
                            Word data = sr.Item2.Apply(input).SingleOrDefault();
                            while (data != null)
                            {
                                srApplied = true;
                                j++;
                                if (j > _morpher.DeletionReapplications)
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
                                srApplied = true;
                                data = sr.Item2.Apply(data).SingleOrDefault();
                            }
                        }
                        break;
                }

                if (srApplied)
                {
                    if (_morpher.TraceManager.IsTracing)
                        _morpher.TraceManager.PhonologicalRuleUnapplied(_rule, i, origInput, input);
                    applied = true;
                }
                else if (_morpher.TraceManager.IsTracing)
                    _morpher.TraceManager.PhonologicalRuleNotUnapplied(_rule, i, input);
            }

            if (applied)
                return input.ToEnumerable();
            return Enumerable.Empty<Word>();
        }
    }
}
