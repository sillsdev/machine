using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Morphology;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Point 4 (C-exact phonology) by <b>composition with HC's phonology inverse</b>
    /// (FST_FULL_PLAN.md). Un-applies the grammar's phonological rules to the surface — reusing each
    /// stratum's <see cref="IPhonologicalRule.CompileAnalysisRule"/>, exactly the rules
    /// <see cref="AnalysisStratumRule"/> runs (surface stratum first, rules reversed within a stratum) —
    /// to recover the underlying form, then walks the underlying-arc morphotactic FST on it
    /// (<see cref="FstTemplateAnalyzer.AnalyzeShape"/>). That is literally phonology⁻¹ ∘ morphotactics.
    ///
    /// Because the inverse is applied to the <i>assembled</i> surface, this covers ALL bounded phonology
    /// — including the cross-boundary, stem-conditioned alternations the per-morpheme precompile (Point 1)
    /// cannot see — completing the phonology story. The un-applied shape carries under-specified nodes
    /// (analysis is non-deterministic), which the unification walk matches against every compatible arc;
    /// verify prunes the spurious ones, so it stays a sound superset. Complete for bounded (non-cyclic)
    /// phonology; an unbounded self-feeding cycle is not a regular relation and simply will not certify.
    ///
    /// <b>Thread-safe.</b> The inverse cascade is compiled once against a private <see cref="Morpher"/>
    /// with its own <see cref="TraceManager"/> (not the factory's), and each <see cref="AnalyzeWord"/>
    /// applies it to a fresh local <see cref="Word"/> — no per-call mutation of shared state — so the
    /// composite stays safe on the parallel path <see cref="CompleteHybridMorpher"/> advertises.
    /// </summary>
    public class ComposedPhonologyProposer : IConstructProposer
    {
        private static readonly MorphOp[] _ops = new MorphOp[0];
        private readonly FstTemplateAnalyzer _fst;
        private readonly Stratum _surfaceStratum;
        private readonly CharacterDefinitionTable _table;
        private readonly LinearRuleCascade<Word, ShapeNode> _inverse;
        private readonly bool _hasPhonology;

        public ComposedPhonologyProposer(Language language, FstTemplateAnalyzer fst)
        {
            _fst = fst;
            _surfaceStratum = language.SurfaceStratum;
            _table = language.SurfaceStratum.CharacterDefinitionTable;
            // Compile against a private Morpher with its own TraceManager — the analysis rules read
            // _morpher.TraceManager (and the morpher's selectors), so this proposer must not share the
            // factory's morpher (mirrors MorpherPool giving each rented morpher its own TraceManager).
            var morpher = new Morpher(new TraceManager(), language);
            // Inverse order mirrors AnalysisLanguageRule/AnalysisStratumRule: strata surface→inner, and
            // within each stratum the synthesis rules are un-applied in reverse application order.
            var rules = new List<IRule<Word, ShapeNode>>();
            foreach (Stratum stratum in language.Strata.Reverse())
            {
                foreach (IPhonologicalRule prule in stratum.PhonologicalRules.Reverse())
                {
                    rules.Add(prule.CompileAnalysisRule(morpher));
                }
            }
            _hasPhonology = rules.Count > 0;
            _inverse = new LinearRuleCascade<Word, ShapeNode>(rules);
        }

        /// <summary>Phonology completeness is not a per-construct MorphOp, so this covers none; its value
        /// is validated empirically by the parity gate.</summary>
        public IReadOnlyCollection<MorphOp> CoveredOps => _ops;

        public IEnumerable<WordAnalysis> AnalyzeWord(string word)
        {
            if (!_hasPhonology)
            {
                yield break; // no phonology ⇒ the bare FST proposer already covers everything
            }
            Shape shape;
            try
            {
                shape = _table.Segment(word);
            }
            catch (InvalidShapeException)
            {
                yield break;
            }
            // Un-apply phonology in place (the cascade mutates the word's shape, as AnalysisStratumRule
            // relies on); the resulting under-specified shape is the underlying form to walk.
            var inverseWord = new Word(_surfaceStratum, shape);
            _inverse.Apply(inverseWord).ToList();
            foreach (WordAnalysis candidate in _fst.AnalyzeShape(inverseWord.Shape))
            {
                yield return candidate;
            }
        }
    }
}
