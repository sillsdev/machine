using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Forward phonology for the surface-allomorph precompile (FST_FULL_PLAN.md, Point 1, C-internal
    /// tier). Applies the grammar's <b>synthesis</b> phonological rules to a morpheme's underlying
    /// segment string <i>in isolation</i> (word-edge context) and returns the distinct surface
    /// realizations. Reuses HC's own compiled synthesis rules — no reimplemented phonology — exactly the
    /// rules <see cref="SynthesisStratumRule"/> runs.
    ///
    /// Tier scope: catches edge-conditioned and morpheme-internal alternations (an affix that devoices
    /// word-finally, a root-internal change). Cross-boundary, stem-conditioned alternations are <b>not</b>
    /// seen by this tier (the neighbor context is absent); those surfaces are simply not precompiled, so
    /// the word rides the engine via the parity gate — never a wrong answer, only less acceleration.
    /// </summary>
    internal sealed class SurfacePhonology
    {
        private readonly CharacterDefinitionTable _table;
        private readonly Stratum _surfaceStratum;
        private readonly List<LinearRuleCascade<Word, ShapeNode>> _strataPrules;

        public SurfacePhonology(Language language, Morpher morpher)
        {
            _table = language.SurfaceStratum.CharacterDefinitionTable;
            _surfaceStratum = language.SurfaceStratum;
            _strataPrules = new List<LinearRuleCascade<Word, ShapeNode>>();
            foreach (Stratum stratum in language.Strata)
            {
                _strataPrules.Add(
                    new LinearRuleCascade<Word, ShapeNode>(
                        stratum.PhonologicalRules.Select(p => p.CompileSynthesisRule(morpher))
                    )
                );
            }
        }

        /// <summary>The distinct surface realizations of <paramref name="underlying"/> in isolation
        /// (always includes the underlying form itself, so the 0-phonology path is unchanged).</summary>
        public IReadOnlyCollection<string> Variants(string underlying)
        {
            Shape shape;
            try
            {
                shape = _table.Segment(underlying);
            }
            catch (InvalidShapeException)
            {
                return new[] { underlying };
            }
            var word = new Word(_surfaceStratum, shape);
            foreach (LinearRuleCascade<Word, ShapeNode> cascade in _strataPrules)
            {
                word = cascade.Apply(word).DefaultIfEmpty(word).First();
            }
            string surface = word.Shape.ToString(_table, false);
            return underlying == surface ? new[] { underlying } : new[] { underlying, surface };
        }
    }
}
