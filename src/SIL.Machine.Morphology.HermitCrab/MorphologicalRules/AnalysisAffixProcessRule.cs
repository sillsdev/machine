using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public class AnalysisAffixProcessRule : IRule<Word, ShapeNode>
    {
        // Mirrors the MatcherSettings<ShapeNode> built below (UseDefaults is never set there, so it takes
        // the MatcherSettings default of false). The prefilter must use the exact unification semantics
        // the matcher itself uses -- see TraversalMethodBase.Advance/CheckArc, which calls
        // arc.Input.Matches(annotation.FeatureStruct, fst.UseUnification, useDefaults, varBindings), i.e.
        // annotationFeatureStruct.IsUnifiable(constraintFeatureStruct, useDefaults, varBindings).
        private const bool UseDefaults = false;

        private readonly Morpher _morpher;
        private readonly AffixProcessRule _rule;
        private readonly List<PatternRule<Word, ShapeNode>> _rules;
        private readonly List<AnalysisAffixProcessAllomorphRuleSpec> _ruleSpecs;

        public AnalysisAffixProcessRule(Morpher morpher, AffixProcessRule rule)
        {
            _morpher = morpher;
            _rule = rule;

            _rules = new List<PatternRule<Word, ShapeNode>>();
            _ruleSpecs = new List<AnalysisAffixProcessAllomorphRuleSpec>();
            foreach (AffixProcessAllomorph allo in rule.Allomorphs)
            {
                var spec = new AnalysisAffixProcessAllomorphRuleSpec(allo);
                _ruleSpecs.Add(spec);
                _rules.Add(
                    new MultiplePatternRule<Word, ShapeNode>(
                        spec,
                        new MatcherSettings<ShapeNode>
                        {
                            Filter = ann => ann.Type() == HCFeatureSystem.Segment,
                            MatchingMethod = MatchingMethod.Unification,
                            AnchoredToStart = true,
                            AnchoredToEnd = true,
                            AllSubmatches = true,
                        }
                    )
                );
            }
        }

        /// <summary>
        /// The shape's first Segment-type node (skipping Boundary-type nodes, exactly as the matcher's
        /// Filter does), or null if the shape has none.
        /// </summary>
        private static ShapeNode FindFirstSegmentNode(Shape shape)
        {
            for (ShapeNode node = shape.First; node != null && node != shape.End; node = node.Next)
            {
                if (node.Type() == HCFeatureSystem.Segment)
                    return node;
            }
            return null;
        }

        /// <summary>
        /// The shape's last Segment-type node (skipping Boundary-type nodes), or null if the shape has
        /// none.
        /// </summary>
        private static ShapeNode FindLastSegmentNode(Shape shape)
        {
            for (ShapeNode node = shape.Last; node != null && node != shape.Begin; node = node.Prev)
            {
                if (node.Type() == HCFeatureSystem.Segment)
                    return node;
            }
            return null;
        }

        /// <summary>
        /// True unless <paramref name="edgeConstraint"/> and <paramref name="edgeNode"/> are both present
        /// and provably cannot unify. Null on either side (no fixed edge constraint on the pattern, or no
        /// segment node on that side of the shape) always passes -- let the full matcher decide. An
        /// Optional edge node also always passes, since the matcher's traversal may skip an optional
        /// annotation entirely (see TraversalMethodBase's optional-annotation handling), so a leading or
        /// trailing optional segment is not a guaranteed edge.
        /// </summary>
        private static bool EdgePasses(FeatureStruct edgeConstraint, ShapeNode edgeNode)
        {
            if (edgeConstraint == null || edgeNode == null || edgeNode.Annotation.Optional)
                return true;
            return edgeNode.Annotation.FeatureStruct.IsUnifiable(edgeConstraint, UseDefaults, null);
        }

        public IEnumerable<Word> Apply(Word input)
        {
            if (!_morpher.RuleSelector(_rule))
                return Enumerable.Empty<Word>();

            if (
                input.GetUnapplicationCount(_rule) >= _rule.MaxApplicationCount
                || !_rule.OutSyntacticFeatureStruct.IsUnifiable(input.SyntacticFeatureStruct)
            )
            {
                return Enumerable.Empty<Word>();
            }

            ShapeNode firstSegment = null;
            ShapeNode lastSegment = null;
            if (_morpher.EdgePrefilterEnabled)
            {
                firstSegment = FindFirstSegmentNode(input.Shape);
                lastSegment = FindLastSegmentNode(input.Shape);
            }

            var output = new List<Word>();
            for (int i = 0; i < _rules.Count; i++)
            {
                if (_morpher.EdgePrefilterEnabled)
                {
                    AnalysisAffixProcessAllomorphRuleSpec spec = _ruleSpecs[i];
                    if (
                        !EdgePasses(spec.LeftEdgeConstraint, firstSegment)
                        || !EdgePasses(spec.RightEdgeConstraint, lastSegment)
                    )
                    {
                        if (_morpher.TraceManager.IsTracing)
                            _morpher.TraceManager.MorphologicalRuleNotUnapplied(_rule, i, input);
                        continue;
                    }
                }

                bool unapplied = false;
                foreach (Word outWord in _rules[i].Apply(input).RemoveDuplicates())
                {
                    if (!_rule.RequiredSyntacticFeatureStruct.IsEmpty)
                    {
                        outWord.EnsureOwnSyntacticFeatureStruct();
                        outWord.SyntacticFeatureStruct.Add(_rule.RequiredSyntacticFeatureStruct);
                    }
                    else if (_rule.OutSyntacticFeatureStruct.IsEmpty)
                    {
                        outWord.EnsureOwnSyntacticFeatureStruct();
                        outWord.SyntacticFeatureStruct.Clear();
                    }
                    outWord.MorphologicalRuleUnapplied(_rule);
                    outWord.Freeze();
                    if (_morpher.TraceManager.IsTracing)
                        _morpher.TraceManager.MorphologicalRuleUnapplied(_rule, i, input, outWord);
                    output.Add(outWord);
                    unapplied = true;
                }

                if (_morpher.TraceManager.IsTracing && !unapplied)
                    _morpher.TraceManager.MorphologicalRuleNotUnapplied(_rule, i, input);
            }
            return output;
        }
    }
}
