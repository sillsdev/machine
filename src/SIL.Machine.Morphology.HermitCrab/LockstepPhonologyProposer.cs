using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Phonology coverage via LEVER_2.md's lazy lockstep composition: <see cref="PhonologyRuleCompiler"/>
    /// auto-builds an <see cref="InversePhonology"/> from the grammar's rewrite rules, and
    /// <see cref="FstTemplateAnalyzer.AnalyzeComposed"/> walks it against the underlying-only lexicon in
    /// lockstep — the lexicon constrains every restoration as it happens, so boundary-conditioned rules
    /// (where <see cref="ComposedPhonologyProposer"/>'s boundary-less un-apply over-generates) stay
    /// sound here by construction.
    ///
    /// <b>Narrower than <see cref="ComposedPhonologyProposer"/> today, not a replacement (yet).</b> The
    /// v1 compiler only handles single-segment, right-context-only rules with no true multi-rule
    /// cascade composition (see <see cref="PhonologyRuleCompiler"/> for the exact supported shape) — it
    /// is wired in ADDITIVELY alongside the existing phonology proposer, not instead of it. Retiring
    /// <see cref="ComposedPhonologyProposer"/>/<see cref="ForwardSynthesisProposer"/> is gated on this
    /// compiler demonstrably matching or exceeding their coverage on real grammars (see
    /// FST_FAST_PATH_PLAN.md Phase 3d) — not assumed.
    /// </summary>
    public sealed class LockstepPhonologyProposer : IConstructProposer
    {
        private static readonly MorphOp[] _ops = new MorphOp[0];
        private readonly FstTemplateAnalyzer _underlyingOnlyFst;
        private readonly InversePhonology _pinv;
        private readonly bool _hasArcs;

        public LockstepPhonologyProposer(Language language, Morpher morpher)
        {
            // Lex must be the underlying-only acceptor (default ctor, no surface precompile) — see
            // LEVER_2.md: composing against the surface-precompiled FST would apply phonology twice.
            _underlyingOnlyFst = new FstTemplateAnalyzer(language);
            (InversePhonology pinv, int unsupported) = PhonologyRuleCompiler.Compile(language, morpher);
            _pinv = pinv;
            UnsupportedRuleCount = unsupported;
            // An all-identity Pinv (no rule contributed a restoration/substitution branch) walks
            // exactly like the plain underlying lexicon, so skip it — the bare FST already covers that.
            _hasArcs = HasNonIdentityArcs(pinv);
        }

        /// <summary>How many (rule, subrule) pairs the compiler could not put into this Pinv — a
        /// coverage diagnostic (see <see cref="PhonologyRuleCompiler"/>), not a soundness signal.</summary>
        public int UnsupportedRuleCount { get; }

        /// <summary>Phonology completeness is not a per-construct MorphOp; validated empirically.</summary>
        public IReadOnlyCollection<MorphOp> CoveredOps => _ops;

        public IEnumerable<WordAnalysis> AnalyzeWord(string word)
        {
            if (!_hasArcs)
            {
                return System.Linq.Enumerable.Empty<WordAnalysis>();
            }
            return _underlyingOnlyFst.AnalyzeComposed(word, _pinv);
        }

        private static bool HasNonIdentityArcs(InversePhonology pinv)
        {
            foreach (InversePhonology.Arc arc in pinv.ArcsFrom(pinv.StartState))
            {
                if (arc.IsEpsilonInput || !arc.SurfaceInput.ValueEquals(arc.UnderlyingOutput))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
