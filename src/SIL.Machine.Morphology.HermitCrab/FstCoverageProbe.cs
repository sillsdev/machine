using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// A deliberately bounded, opt-in tool for grammar authoring: run a wordlist through the FST fast
    /// path — <see cref="VerifiedFstAnalyzer"/> over the full composite (<see cref="FstTemplateAnalyzer"/>
    /// plus every sibling generator: <see cref="ReduplicationProposer"/>, <see cref="InfixProposer"/>,
    /// <see cref="ComposedPhonologyProposer"/>, <see cref="LockstepPhonologyProposer"/>) — and report
    /// coverage, or diff coverage between two versions of a grammar. This exists to answer "did my
    /// grammar edit make parsing better or worse?" in milliseconds, as a fast proxy for the real engine
    /// — never as a replacement for it, and never behind any notion of the fast path being "proven"
    /// complete for a grammar.
    ///
    /// <b>Contract (read before trusting the numbers):</b>
    /// <list type="bullet">
    /// <item><b>Sound on positives.</b> A word this reports as parsed was confirmed by HC's own
    /// restricted re-analysis (<see cref="FstReplay"/>) — it is a genuine engine analysis, not FST
    /// over-generation.</item>
    /// <item><b>Known-incomplete on negatives — by design, not by accident.</b> The fast path does not
    /// (yet) model every construct — compounding, clitics, templatic multi-slot infixation, and
    /// phonological rules outside <see cref="PhonologyRuleCompiler"/>'s and
    /// <see cref="ComposedPhonologyProposer"/>'s combined reach are the current gaps (see
    /// FST_FAST_PATH_PLAN.md's KNOWN_GAPS) — so an uncovered word shows up as "unparsed" here even when
    /// the real engine parses it. Do not read "unparsed" as "invalid"; read a coverage-count *change*
    /// between two grammar versions as the signal.</item>
    /// <item><b>Never wired into production analysis.</b> This type is not used by
    /// <see cref="Morpher"/> — it exists solely for a grammar engineer (or a script) to call directly
    /// while iterating on a grammar.</item>
    /// </list>
    /// </summary>
    public sealed class FstCoverageProbe
    {
        private readonly VerifiedFstAnalyzer _analyzer;
        private readonly bool _coversAllConstructs;
        private readonly IReadOnlyCollection<MorphOp> _uncoveredConstructs;
        private readonly int _unsupportedPhonologyRuleCount;

        private FstCoverageProbe(
            VerifiedFstAnalyzer analyzer,
            bool coversAllConstructs,
            IReadOnlyCollection<MorphOp> uncoveredConstructs,
            int unsupportedPhonologyRuleCount
        )
        {
            _analyzer = analyzer;
            _coversAllConstructs = coversAllConstructs;
            _uncoveredConstructs = uncoveredConstructs;
            _unsupportedPhonologyRuleCount = unsupportedPhonologyRuleCount;
        }

        /// <summary>Build the full-composite fast path for one grammar. Cheap enough to call after
        /// every edit: no corpus comparison against the engine, ever. <paramref name="forwardSynthesis"/>
        /// (opt-in, default off) adds <see cref="ForwardSynthesisProposer"/> — see
        /// <see cref="CompositeProposer.ForLanguage"/> for its build-cost tradeoff.</summary>
        public static FstCoverageProbe ForLanguage(Language language, bool forwardSynthesis = false)
        {
            var fst = new FstTemplateAnalyzer(language, new Morpher(new TraceManager(), language));
            var lockstep = new LockstepPhonologyProposer(language, new Morpher(new TraceManager(), language));
            var generators = new List<IConstructProposer>
            {
                new ReduplicationProposer(language, fst),
                new InfixProposer(language, fst),
                new ComposedPhonologyProposer(language, fst),
                lockstep,
            };
            if (forwardSynthesis)
            {
                generators.Insert(0, new ForwardSynthesisProposer(language, new Morpher(new TraceManager(), language)));
            }
            var composite = new CompositeProposer(fst, generators.ToArray());
            var pool = new MorpherPool(() => new Morpher(new TraceManager(), language));
            var analyzer = new VerifiedFstAnalyzer(composite, pool);
            return new FstCoverageProbe(
                analyzer,
                composite.CoversAllConstructs,
                fst.UncoveredOps,
                lockstep.UnsupportedRuleCount
            );
        }

        /// <summary>Run the fast path over <paramref name="words"/> and summarize coverage.</summary>
        public ProbeReport Probe(IEnumerable<string> words)
        {
            var sw = Stopwatch.StartNew();
            int total = 0;
            int parsed = 0;
            int totalAnalyses = 0;
            var unparsed = new List<string>();
            foreach (string word in words)
            {
                total++;
                int count = _analyzer.AnalyzeWord(word).Count();
                if (count > 0)
                {
                    parsed++;
                    totalAnalyses += count;
                }
                else
                {
                    unparsed.Add(word);
                }
            }
            sw.Stop();
            return new ProbeReport(
                total,
                parsed,
                totalAnalyses,
                unparsed,
                _coversAllConstructs,
                _uncoveredConstructs,
                _unsupportedPhonologyRuleCount,
                sw.Elapsed
            );
        }

        /// <summary>Probe <paramref name="before"/> and <paramref name="after"/> over the same corpus and
        /// diff coverage — the direct answer to "did this grammar edit make parsing better or worse?".
        /// Each grammar gets its own fresh fast-path build, so this is exactly two <see cref="Probe"/>
        /// calls plus a set diff; no engine comparison, ever.</summary>
        public static CoverageDiff CompareGrammars(Language before, Language after, IEnumerable<string> words)
        {
            List<string> corpus = words.ToList();
            ProbeReport beforeReport = ForLanguage(before).Probe(corpus);
            ProbeReport afterReport = ForLanguage(after).Probe(corpus);
            var beforeUnparsed = new HashSet<string>(beforeReport.UnparsedWords);
            var afterUnparsed = new HashSet<string>(afterReport.UnparsedWords);
            List<string> gained = beforeUnparsed.Where(w => !afterUnparsed.Contains(w)).OrderBy(w => w).ToList();
            List<string> lost = afterUnparsed.Where(w => !beforeUnparsed.Contains(w)).OrderBy(w => w).ToList();
            return new CoverageDiff(beforeReport, afterReport, gained, lost);
        }
    }

    /// <summary>Coverage summary for one grammar over one corpus — see <see cref="FstCoverageProbe"/> for
    /// what "parsed" does and does not guarantee.</summary>
    public sealed class ProbeReport
    {
        public int TotalWords { get; }
        public int ParsedWords { get; }
        public int TotalAnalyses { get; }
        public IReadOnlyList<string> UnparsedWords { get; }

        /// <summary>True iff every construct the bare FST cannot build (reduplication/infix/etc.) is
        /// claimed by a sibling generator — a build-time coverage diagnostic, not a soundness or
        /// per-word completeness guarantee (see <see cref="CompositeProposer.CoversAllConstructs"/>).</summary>
        public bool CoversAllConstructs { get; }

        /// <summary>The <see cref="MorphOp"/>s no generator in this composite claims to cover — a
        /// grammar using one of these constructs will systematically under-generate on words that need
        /// it. Empty when <see cref="CoversAllConstructs"/> is true.</summary>
        public IReadOnlyCollection<MorphOp> UncoveredConstructs { get; }

        /// <summary>How many (rule, subrule) pairs <see cref="PhonologyRuleCompiler"/> could not fit
        /// into its v1 supported shape (see its class remarks) — a phonology-specific coverage
        /// diagnostic distinct from <see cref="UncoveredConstructs"/>, which only tracks whole
        /// <see cref="MorphOp"/> categories.</summary>
        public int UnsupportedPhonologyRuleCount { get; }

        /// <summary>Wall-clock time for this <see cref="FstCoverageProbe.Probe"/> call.</summary>
        public System.TimeSpan Elapsed { get; }

        public double CoverageRate => TotalWords == 0 ? 0 : (double)ParsedWords / TotalWords;
        public double AverageAnalysesPerParsedWord => ParsedWords == 0 ? 0 : (double)TotalAnalyses / ParsedWords;

        internal ProbeReport(
            int totalWords,
            int parsedWords,
            int totalAnalyses,
            IReadOnlyList<string> unparsedWords,
            bool coversAllConstructs,
            IReadOnlyCollection<MorphOp> uncoveredConstructs,
            int unsupportedPhonologyRuleCount,
            System.TimeSpan elapsed
        )
        {
            TotalWords = totalWords;
            ParsedWords = parsedWords;
            TotalAnalyses = totalAnalyses;
            UnparsedWords = unparsedWords;
            CoversAllConstructs = coversAllConstructs;
            UncoveredConstructs = uncoveredConstructs;
            UnsupportedPhonologyRuleCount = unsupportedPhonologyRuleCount;
            Elapsed = elapsed;
        }

        public override string ToString() =>
            $"{ParsedWords}/{TotalWords} words parsed ({CoverageRate:P1}), "
            + $"{AverageAnalysesPerParsedWord:F2} analyses/parsed word, "
            + $"{Elapsed.TotalMilliseconds:F0} ms"
            + (CoversAllConstructs ? "" : $", uncovered constructs: [{string.Join(",", UncoveredConstructs)}]")
            + (
                UnsupportedPhonologyRuleCount > 0
                    ? $", {UnsupportedPhonologyRuleCount} unsupported phonology rule(s)"
                    : ""
            );
    }

    /// <summary>The coverage delta between two grammar versions over the same corpus. <see cref="Gained"/>
    /// and <see cref="Lost"/> are the words whose fast-path parse status flipped — the direct answer to
    /// "what did this edit change?".</summary>
    public sealed class CoverageDiff
    {
        public ProbeReport Before { get; }
        public ProbeReport After { get; }

        /// <summary>Unparsed under <see cref="Before"/>, parsed under <see cref="After"/>.</summary>
        public IReadOnlyList<string> Gained { get; }

        /// <summary>Parsed under <see cref="Before"/>, unparsed under <see cref="After"/>.</summary>
        public IReadOnlyList<string> Lost { get; }

        internal CoverageDiff(
            ProbeReport before,
            ProbeReport after,
            IReadOnlyList<string> gained,
            IReadOnlyList<string> lost
        )
        {
            Before = before;
            After = after;
            Gained = gained;
            Lost = lost;
        }

        public override string ToString() =>
            $"before: {Before}{System.Environment.NewLine}"
            + $"after:  {After}{System.Environment.NewLine}"
            + $"gained {Gained.Count} word(s), lost {Lost.Count} word(s)";
    }
}
