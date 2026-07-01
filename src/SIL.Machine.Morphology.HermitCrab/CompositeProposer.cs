using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Morphology;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Unions the candidate sets of several proposers into one (FST_FULL_PLAN.md). The FST proposer
    /// (<see cref="FstTemplateAnalyzer"/>) covers the regular bulk; sibling generators
    /// (<see cref="ReduplicationProposer"/>, <see cref="InfixProposer"/>) contribute candidates for
    /// constructs the FST skips. Every candidate still flows through the single
    /// <see cref="VerifiedFstAnalyzer"/> verify gate, so the composite is sound by the same argument as
    /// each part — a generator that over-generates has its junk pruned, one that under-generates only
    /// loses acceleration (the parity gate falls those words back to the engine).
    ///
    /// Candidates are <b>deduped by signature</b> before they leave the composite: when two generators
    /// (or a generator and the FST) propose the same morpheme set, verify would otherwise confirm it
    /// twice and emit a duplicate analysis. The signature is order-sensitive morpheme identity + root
    /// index, mirroring <see cref="FstReplay"/>'s match semantics.
    ///
    /// <see cref="CoversAllConstructs"/> aggregates coverage at the MorphOp level: the FST's uncovered
    /// ops minus the ops the sibling generators cover. It is the cheap build-time signal certification
    /// pairs with the empirical parity gate; op-level optimism here is safe because parity is the real
    /// arbiter (a generator that covers an op only partially fails parity and the grammar is not
    /// certified).
    /// </summary>
    public class CompositeProposer : IMorphologicalAnalyzer
    {
        private readonly IReadOnlyList<IMorphologicalAnalyzer> _proposers;
        private readonly bool _coversAllConstructs;

        public CompositeProposer(FstTemplateAnalyzer fst, params IConstructProposer[] generators)
        {
            var proposers = new List<IMorphologicalAnalyzer> { fst };
            var covered = new HashSet<MorphOp>();
            foreach (IConstructProposer generator in generators)
            {
                proposers.Add(generator);
                foreach (MorphOp op in generator.CoveredOps)
                {
                    covered.Add(op);
                }
            }
            _proposers = proposers;
            _coversAllConstructs = fst.UncoveredOps.All(covered.Contains);
        }

        /// <summary>The standard production proposer: the FST plus the reduplication and infix
        /// generators and the phonology-composition proposer (Point 4, all bounded phonology including
        /// cross-boundary). For a grammar without a given construct the corresponding generator is inert
        /// (it holds no rules and yields nothing — the phonology proposer short-circuits when the grammar
        /// has no phonological rules), so this adds near-zero overhead and does not change behavior; that
        /// is why the factories wire it unconditionally rather than as an opt-in.
        ///
        /// <paramref name="forwardSynthesis"/> (opt-in, default off) adds the
        /// <see cref="ForwardSynthesisProposer"/> — a build-time root × affix-combo synthesis precompile
        /// that covers boundary-conditioned morphophonemics (e.g. Indonesian meN- nasal substitution) the
        /// inverse-based phonology proposer cannot. It is opt-in because its build cost grows with
        /// lexicon × affix permutations: appropriate for bounded-affixation grammars / fixed corpora, not
        /// for heavily-inflecting templatic systems. <paramref name="maxAffixes"/> bounds the combo
        /// depth.</summary>
        public static CompositeProposer ForLanguage(
            Language language,
            FstTemplateAnalyzer fst,
            bool forwardSynthesis = false,
            int maxAffixes = 2
        )
        {
            var generators = new List<IConstructProposer>
            {
                new ReduplicationProposer(language, fst),
                new InfixProposer(language, fst),
                new ComposedPhonologyProposer(language, fst),
            };
            if (forwardSynthesis)
            {
                generators.Insert(
                    0,
                    new ForwardSynthesisProposer(language, new Morpher(new TraceManager(), language), maxAffixes)
                );
            }
            return new CompositeProposer(fst, generators.ToArray());
        }

        /// <summary>True iff every construct the FST proposer skipped is claimed by a sibling generator.
        /// Paired with the empirical parity gate for certification (see class remarks).</summary>
        public bool CoversAllConstructs => _coversAllConstructs;

        public IEnumerable<WordAnalysis> AnalyzeWord(string word)
        {
            var ids = new Dictionary<IMorpheme, int>();
            var seen = new HashSet<string>();
            foreach (IMorphologicalAnalyzer proposer in _proposers)
            {
                foreach (WordAnalysis candidate in proposer.AnalyzeWord(word))
                {
                    if (seen.Add(Signature(candidate, ids)))
                    {
                        yield return candidate;
                    }
                }
            }
        }

        /// <summary>Order-sensitive morpheme-identity signature (same scheme as <see cref="FstReplay"/>).</summary>
        private static string Signature(WordAnalysis analysis, Dictionary<IMorpheme, int> ids)
        {
            return string.Join("+", analysis.Morphemes.Select(m => Id(m, ids))) + ":" + analysis.RootMorphemeIndex;
        }

        private static int Id(IMorpheme morpheme, Dictionary<IMorpheme, int> ids)
        {
            if (!ids.TryGetValue(morpheme, out int id))
            {
                id = ids.Count;
                ids[morpheme] = id;
            }
            return id;
        }
    }

    /// <summary>A candidate generator for a specific non-FST construct (reduplication, infixation). It
    /// proposes a sound superset for that construct and declares which <see cref="MorphOp"/>s it covers
    /// so the composite can aggregate the build-time coverage signal.</summary>
    public interface IConstructProposer : IMorphologicalAnalyzer
    {
        IReadOnlyCollection<MorphOp> CoveredOps { get; }
    }
}
