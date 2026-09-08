using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public class AnalysisAffixProcessAllomorphRuleSpec : AnalysisMorphologicalTransformRuleSpec
    {
        private readonly AffixProcessAllomorph _allomorph;

        public AnalysisAffixProcessAllomorphRuleSpec(AffixProcessAllomorph allomorph)
            : base(allomorph.Lhs, allomorph.Rhs)
        {
            _allomorph = allomorph;
            Pattern.Freeze();

            // Edge-segment prefilter support (see AnalysisAffixProcessRule.Apply): both anchors mean a
            // top-level leading/trailing Constraint can only ever match the shape's first/last
            // Segment-type node, so its FeatureStruct is captured here once at compile time. Null when the
            // first/last top-level child is anything else (a Group, Quantifier, or Alternation) -- those
            // give no fixed edge condition, so the matcher must decide.
            LeftEdgeConstraint = EdgeConstraint(Pattern.Children.First);
            RightEdgeConstraint = EdgeConstraint(Pattern.Children.Last);
        }

        /// <summary>
        /// The FeatureStruct of <paramref name="node"/> when it is a bare top-level Constraint. Null for
        /// any other node kind (including null itself, i.e. an empty pattern).
        /// </summary>
        private static FeatureStruct EdgeConstraint(PatternNode<Word, ShapeNode> node)
        {
            return (node as Constraint<Word, ShapeNode>)?.FeatureStruct;
        }

        /// <summary>
        /// The FeatureStruct that a shape's first Segment-type node must unify with for this allomorph to
        /// have any chance of matching, or null if the pattern's first top-level child is not a bare
        /// Constraint.
        /// </summary>
        internal FeatureStruct LeftEdgeConstraint { get; }

        /// <summary>
        /// The FeatureStruct that a shape's last Segment-type node must unify with for this allomorph to
        /// have any chance of matching, or null if the pattern's last top-level child is not a bare
        /// Constraint.
        /// </summary>
        internal FeatureStruct RightEdgeConstraint { get; }

        public override Word ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match)
        {
            Word output = match.Input.CloneForEngine();
            output.ResetShape();
            GenerateShape(_allomorph.Lhs, output.Shape, match);
            return output;
        }
    }
}
