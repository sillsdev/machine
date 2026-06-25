using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// How costly a flagged rule is for parsing.
    /// </summary>
    public enum GrammarAdvisorySeverity
    {
        /// <summary>Finite-state-able; informational only.</summary>
        Info,

        /// <summary>Stays finite-state but inflates the combinatorial search fan-out.</summary>
        Cost,

        /// <summary>Breaks finite-state compilation — forces the slow combinatorial search.</summary>
        Escape,
    }

    /// <summary>
    /// One advisory about a single grammar rule: what makes it expensive, and how to keep
    /// (or get) it back on the fast finite-state path.
    /// </summary>
    public sealed class GrammarAdvisory
    {
        public GrammarAdvisory(
            string rule,
            string stratum,
            string kind,
            GrammarAdvisorySeverity severity,
            string issue,
            string advice,
            bool? probeable = null,
            bool? regular = null
        )
        {
            Rule = rule;
            Stratum = stratum;
            Kind = kind;
            Severity = severity;
            Issue = issue;
            Advice = advice;
            Probeable = probeable;
            Regular = regular;
        }

        /// <summary>Name of the offending rule.</summary>
        public string Rule { get; }

        /// <summary>Name of the stratum the rule lives in (rules can appear in more than one).</summary>
        public string Stratum { get; }

        /// <summary>Rule kind (affix / phonological / compounding).</summary>
        public string Kind { get; }

        public GrammarAdvisorySeverity Severity { get; }

        /// <summary>One sentence: what is expensive and why.</summary>
        public string Issue { get; }

        /// <summary>"Constrain it like this" and/or "try this instead".</summary>
        public string Advice { get; }

        /// <summary>
        /// For an <see cref="GrammarAdvisorySeverity.Escape"/>: whether a per-word un-application
        /// probe (strip the affix / de-reduplicate, then re-parse the residue with the FST) is
        /// <em>sound</em> for this rule. True = "clean": no phonological rule at or after its
        /// stratum can rewrite the affixed span, so the affix surfaces literally and stripping it
        /// recovers the stem exactly — the slow path collapses to a cheap local guess+verify.
        /// False = "opaque": a later rule may alter the span, so literal stripping can miss an
        /// analysis and the search backstop is required. Null = not an insertion escape / N/A.
        /// </summary>
        public bool? Probeable { get; }

        /// <summary>
        /// For an <see cref="GrammarAdvisorySeverity.Escape"/>: whether the construct denotes a
        /// <em>regular relation</em> (an FST exists for it in principle). True = regular — it could
        /// be reclaimed onto the fast path once the FST compiler exists (state-encode a spreading
        /// feature, bounded-fold a finite copy, …); by Kaplan &amp; Kay (1994) every standard
        /// rewrite rule is regular regardless of how long its environment is. False = genuinely
        /// non-regular (unbounded copy) or unconfirmable. Null = N/A.
        ///
        /// IMPORTANT: this is a <em>reclaim path</em>, NOT a cost downgrade. A <c>Regular</c>
        /// escape is still <c>Escape</c> severity because it is slow in <em>today's</em> engine —
        /// the FST compiler that would make it fast is not built yet. Severity tells the truth
        /// about today; <c>Regular</c> tells you whether the slowness is fixable by compilation.
        /// </summary>
        public bool? Regular { get; }
    }

    /// <summary>
    /// The result of <see cref="GrammarFstAdvisor.Analyze(Language)"/>: the per-rule advisories
    /// plus an overall tier verdict.
    /// </summary>
    public sealed class GrammarFstReport
    {
        public GrammarFstReport(
            IReadOnlyList<GrammarAdvisory> advisories,
            int affixRulesExamined,
            int phonologicalRulesExamined,
            int compoundingRulesExamined
        )
        {
            Advisories = advisories;
            AffixRulesExamined = affixRulesExamined;
            PhonologicalRulesExamined = phonologicalRulesExamined;
            CompoundingRulesExamined = compoundingRulesExamined;
            EscapeCount = advisories.Count(a => a.Severity == GrammarAdvisorySeverity.Escape);
            CostCount = advisories.Count(a => a.Severity == GrammarAdvisorySeverity.Cost);
            InfoCount = advisories.Count(a => a.Severity == GrammarAdvisorySeverity.Info);
            ProbeableEscapeCount = advisories.Count(a =>
                a.Severity == GrammarAdvisorySeverity.Escape && a.Probeable == true
            );
            OpaqueEscapeCount = advisories.Count(a =>
                a.Severity == GrammarAdvisorySeverity.Escape && a.Probeable == false
            );
            RegularEscapeCount = advisories.Count(a =>
                a.Severity == GrammarAdvisorySeverity.Escape && a.Regular == true
            );
            NonRegularEscapeCount = advisories.Count(a =>
                a.Severity == GrammarAdvisorySeverity.Escape && a.Regular != true
            );
        }

        public IReadOnlyList<GrammarAdvisory> Advisories { get; }

        /// <summary>Affix-process rules inspected (those without an advisory are clean/FST-able).</summary>
        public int AffixRulesExamined { get; }

        /// <summary>Phonological rules (rewrite + metathesis) inspected.</summary>
        public int PhonologicalRulesExamined { get; }

        /// <summary>Compounding rules inspected.</summary>
        public int CompoundingRulesExamined { get; }

        /// <summary>Number of rules that break finite-state compilation.</summary>
        public int EscapeCount { get; }

        /// <summary>Number of rules that inflate the search but stay finite-state.</summary>
        public int CostCount { get; }

        public int InfoCount { get; }

        /// <summary>Escapes for which the per-word un-application probe is sound (clean).</summary>
        public int ProbeableEscapeCount { get; }

        /// <summary>Escapes that may interact with a later rule, so the search backstop is needed.</summary>
        public int OpaqueEscapeCount { get; }

        /// <summary>
        /// Escapes that are regular (an FST could reclaim them once the compiler exists). They are
        /// still slow in today's engine — this is a reclaim path, not a cost downgrade.
        /// </summary>
        public int RegularEscapeCount { get; }

        /// <summary>Escapes that are genuinely non-regular or unconfirmable (no FST in principle).</summary>
        public int NonRegularEscapeCount { get; }

        /// <summary>
        /// Static tier candidate. The static report cannot compute the corpus-weighted fallback
        /// rate, so for a few escapes it reports the <em>candidate</em>; the FST pipeline's corpus
        /// pass confirms whether Tier 2 is worth it vs. Tier 3.
        /// </summary>
        public string Tier =>
            EscapeCount == 0
                ? "Tier 1 candidate — fully FST-able"
                : ProbeableEscapeCount == EscapeCount
                    ? "Tier 2⁺ candidate — every escape is probe-able (surface-invariant): a per-word "
                        + "un-application probe WOULD recover the fast path once the probe runtime exists; "
                        + "all escapes are slow in today's engine"
                    : EscapeCount <= 3
                        ? "Tier 2 candidate — hybrid (opaque/non-probe-able escapes fall back to search); confirm with corpus fallback rate"
                        : "Tier 3 — pervasive escapes, search engine only";

        /// <summary>The rules that break FST compilation (the warnings that flip the tier).</summary>
        public IEnumerable<GrammarAdvisory> Escapes =>
            Advisories.Where(a => a.Severity == GrammarAdvisorySeverity.Escape);

        /// <summary>A readable dump of the report.</summary>
        public string Format()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Tier);
            sb.AppendLine(
                $"  examined {AffixRulesExamined} affix, {PhonologicalRulesExamined} phonological, "
                    + $"{CompoundingRulesExamined} compounding rule(s)"
            );
            sb.AppendLine(
                $"  {EscapeCount} escape(s) ({ProbeableEscapeCount} probe-able, {OpaqueEscapeCount} opaque), "
                    + $"{CostCount} cost(s), {InfoCount} info — {Advisories.Count} rule advisories"
            );
            if (EscapeCount > 0)
            {
                sb.AppendLine(
                    $"  reclaim path: {RegularEscapeCount} of {EscapeCount} escape(s) are FST-reclaimable "
                        + "(regular) once the FST compiler exists; ALL "
                        + $"{EscapeCount} are slow in today's engine. {NonRegularEscapeCount} are genuinely "
                        + "non-regular (per-word probe or search only)."
                );
            }
            foreach (
                GrammarAdvisory a in Advisories
                    .OrderByDescending(a => a.Severity)
                    .ThenBy(a => a.Rule, System.StringComparer.Ordinal)
            )
            {
                string probe =
                    a.Probeable == true ? " [probe-able]"
                    : a.Probeable == false ? " [opaque]"
                    : "";
                string regular =
                    a.Regular == true ? " [regular: FST-reclaimable, slow today]"
                    : a.Regular == false ? " [non-regular]"
                    : "";
                sb.AppendLine();
                sb.AppendLine($"[{a.Severity}]{probe}{regular} {a.Rule} ({a.Kind}, stratum '{a.Stratum}')");
                sb.AppendLine($"  issue : {a.Issue}");
                if (a.Advice.Length > 0)
                    sb.AppendLine($"  advice: {a.Advice}");
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Static grammar linter for the FST acceleration work (see <c>fst.md</c> / <c>HERMITCRAB_FST_PLAN.md</c>).
    /// It walks a compiled <see cref="Language"/> and flags, per rule, what makes parsing expensive
    /// or blocks finite-state compilation, with an actionable write-up (why it's costly, how to
    /// constrain it, what to try instead) and an overall tier verdict.
    ///
    /// This is pure static analysis of the object model — no parsing, no corpus needed — so it can
    /// run at grammar-authoring time or in CI: a new <see cref="GrammarAdvisorySeverity.Escape"/>
    /// that flips the tier is the "one new rule blew up the grammar" warning.
    /// </summary>
    public static class GrammarFstAdvisor
    {
        /// <summary>
        /// Analyze every rule in <paramref name="language"/>.
        /// </summary>
        /// <param name="language">A compiled grammar.</param>
        /// <param name="manyAllomorphsThreshold">
        /// Above this allomorph count a rule earns a <see cref="GrammarAdvisorySeverity.Cost"/> note.
        /// </param>
        public static GrammarFstReport Analyze(Language language, int manyAllomorphsThreshold = 8)
        {
            var advisories = new List<GrammarAdvisory>();
            int affixExamined = 0;
            int phonExamined = 0;
            int compoundExamined = 0;

            // For the clean/opaque (probe-ability) test: an insertion escape in stratum i is sound
            // to un-apply by stripping iff no phonological rule at stratum i or later could rewrite
            // the affixed span. Precompute the count of phonological rules at or after each stratum.
            IList<Stratum> strata = language.Strata;
            var phonAtOrAfter = new int[strata.Count + 1];
            for (int i = strata.Count - 1; i >= 0; i--)
                phonAtOrAfter[i] = phonAtOrAfter[i + 1] + strata[i].PhonologicalRules.Count;

            for (int s = 0; s < strata.Count; s++)
            {
                Stratum stratum = strata[s];
                bool surfaceInvariant = phonAtOrAfter[s] == 0;
                foreach (IMorphologicalRule mrule in stratum.MorphologicalRules)
                {
                    switch (mrule)
                    {
                        case AffixProcessRule affix:
                            affixExamined++;
                            AnalyzeAffix(affix, stratum.Name, surfaceInvariant, advisories, manyAllomorphsThreshold);
                            break;
                        case CompoundingRule compound:
                            compoundExamined++;
                            advisories.Add(
                                new GrammarAdvisory(
                                    compound.Name,
                                    stratum.Name,
                                    "compounding",
                                    GrammarAdvisorySeverity.Info,
                                    "Compounding rule; bounded by MaxStemCount, so it stays finite-state.",
                                    "Keep MaxStemCount as low as the language needs; unbounded compounding is not finite-state."
                                )
                            );
                            break;
                    }
                }

                foreach (IPhonologicalRule prule in stratum.PhonologicalRules)
                {
                    phonExamined++;
                    AnalyzePhonological(prule, stratum.Name, advisories);
                }
            }
            return new GrammarFstReport(advisories, affixExamined, phonExamined, compoundExamined);
        }

        private static void AnalyzeAffix(
            AffixProcessRule rule,
            string stratum,
            bool surfaceInvariant,
            List<GrammarAdvisory> advisories,
            int manyAllomorphsThreshold
        )
        {
            // An insertion escape is "probe-able" (a per-word strip-and-reparse un-application is
            // sound) only when nothing downstream can rewrite the affixed span — i.e. no
            // phonological rule applies at or after this rule's stratum.
            string probeNote = surfaceInvariant
                ? " This escape is PROBE-ABLE: no phonological rule applies after it, so the affix "
                    + "surfaces literally — a per-word probe that strips the candidate affix and re-parses "
                    + "the residue with the FST recovers the analysis without the search engine."
                : " This escape is OPAQUE: a phonological rule applies after it and may rewrite the "
                    + "affixed span, so a literal strip-and-reparse probe can miss an analysis; the search "
                    + "backstop is required.";

            foreach (AffixProcessAllomorph allomorph in rule.Allomorphs)
            {
                // Reduplication: the same input part is copied two or more times. Copying an
                // unbounded span is not regular, so the rule is not finite-state.
                IGrouping<string, CopyFromInput> duplicated = allomorph
                    .Rhs.OfType<CopyFromInput>()
                    .GroupBy(c => c.PartName)
                    .FirstOrDefault(g => g.Count() >= 2);
                if (duplicated != null)
                {
                    // Boundedness of the copied part decides regularity: a fixed-size reduplicant
                    // (CV/CVC) is a finite copy → regular (reclaimable by bounded fold); copying an
                    // unbounded part (the whole stem) is the one genuinely non-regular operation
                    // ({ww} is not regular). Unresolved part → treat as non-regular (warn).
                    bool bounded = IsPartBounded(allomorph, duplicated.Key);
                    string regularNote = bounded
                        ? " REGULAR (bounded reduplicant = finite copy): an FST could reclaim it by "
                            + "bounded-folding the copy — once the FST compiler exists. It is still slow in "
                            + "today's engine."
                        : " GENUINELY NON-REGULAR (unbounded copy — {ww} is not a regular relation): no FST "
                            + "exists for it; only the per-word strip-and-reparse probe (when surface-invariant) "
                            + "or the search engine. Slow today.";
                    advisories.Add(
                        new GrammarAdvisory(
                            rule.Name,
                            stratum,
                            "affix",
                            GrammarAdvisorySeverity.Escape,
                            $"Reduplication: part '{duplicated.Key}' is copied {duplicated.Count()}×, so the "
                                + "parser falls back to the slow combinatorial search for any word this rule "
                                + "could apply to.",
                            "If the reduplicant is a fixed size (e.g. one CV syllable), bound the copied part's "
                                + "length so it becomes finite-state. If only a handful of forms reduplicate, list "
                                + "them as lexical entries instead. Otherwise this rule keeps the whole grammar in "
                                + "the hybrid/search tier."
                                + probeNote
                                + regularNote,
                            surfaceInvariant,
                            bounded
                        )
                    );
                }
                else if (HasInfixedCopy(allomorph.Rhs))
                {
                    // Infixation: a non-copy action (inserted material) sits BETWEEN two copies of
                    // the stem (copy…insert…copy), so the stem is split at an internal position.
                    // Contiguous copies with inserts only at the ends (copy/copy/insert,
                    // insert/copy/copy, insert/copy/copy/insert) are ordinary prefix / suffix /
                    // circumfix over a split stem — finite-state, NOT flagged.
                    advisories.Add(
                        new GrammarAdvisory(
                            rule.Name,
                            stratum,
                            "affix",
                            GrammarAdvisorySeverity.Escape,
                            "Infixation: material is inserted between two copies of the stem, splitting it at "
                                + "an internal position.",
                            "If the infix position is fixed (a known slot), encode it as a bounded split so it "
                                + "stays finite-state. A variable, content-determined split blocks FST compilation."
                                + probeNote
                                + " REGULAR (the split is described by a regular pattern): an FST could reclaim it "
                                + "by bounded-folding the split, or the per-word probe handles it — once those exist. "
                                + "It is still slow in today's engine.",
                            surfaceInvariant,
                            regular: true
                        )
                    );
                }

                if (allomorph.Rhs.OfType<ModifyFromInput>().Any())
                {
                    advisories.Add(
                        new GrammarAdvisory(
                            rule.Name,
                            stratum,
                            "affix",
                            GrammarAdvisorySeverity.Info,
                            "Process modification (ModifyFromInput) rewrites stem segments; finite-state only if "
                                + "the change is local and bounded.",
                            "A feature change in a fixed context is fine; a non-local or agreement-driven change "
                                + "blocks FST — consider a bounded reformulation."
                        )
                    );
                }
            }

            if (rule.Allomorphs.Count > manyAllomorphsThreshold)
            {
                advisories.Add(
                    new GrammarAdvisory(
                        rule.Name,
                        stratum,
                        "affix",
                        GrammarAdvisorySeverity.Cost,
                        $"{rule.Allomorphs.Count} allomorphs; each one multiplies the un-application branching "
                            + "during analysis.",
                        "Consolidate allomorphs via environment conditioning where the language allows it."
                    )
                );
            }
        }

        /// <summary>
        /// True when a non-copy action (inserted material) appears strictly between the first and
        /// last <see cref="CopyFromInput"/> in <paramref name="rhs"/> — i.e. copy…insert…copy, the
        /// signature of infixation. Contiguous copies (inserts only at the ends) return false.
        /// </summary>
        private static bool HasInfixedCopy(IList<MorphologicalOutputAction> rhs)
        {
            int first = -1;
            int last = -1;
            for (int i = 0; i < rhs.Count; i++)
            {
                if (rhs[i] is CopyFromInput)
                {
                    if (first < 0)
                        first = i;
                    last = i;
                }
            }
            if (first < 0 || last == first)
                return false;
            for (int i = first + 1; i < last; i++)
            {
                if (!(rhs[i] is CopyFromInput))
                    return true;
            }
            return false;
        }

        private static void AnalyzePhonological(
            IPhonologicalRule prule,
            string stratum,
            List<GrammarAdvisory> advisories
        )
        {
            switch (prule)
            {
                case RewriteRule rewrite:
                    AnalyzeRewrite(rewrite, stratum, advisories);
                    break;
                case MetathesisRule metathesis:
                    advisories.Add(
                        new GrammarAdvisory(
                            metathesis.Name,
                            stratum,
                            "phonological",
                            GrammarAdvisorySeverity.Info,
                            "Metathesis (segment reordering); finite-state over a bounded span.",
                            "Keep the reordered span bounded; unbounded metathesis blocks FST."
                        )
                    );
                    break;
            }
        }

        private static void AnalyzeRewrite(RewriteRule rule, string stratum, List<GrammarAdvisory> advisories)
        {
            bool unboundedEnvironment = rule.Subrules.Any(sr =>
                HasUnboundedQuantifier(sr.LeftEnvironment) || HasUnboundedQuantifier(sr.RightEnvironment)
            );

            if (unboundedEnvironment)
            {
                // Kaplan & Kay (1994): a context-sensitive rewrite rule with regular φ/ψ/λ/ρ,
                // applied directionally, denotes a REGULAR relation no matter how long the
                // environment is — so an unbounded environment does not make the rule non-regular.
                // It is regular iff the rule's own Lhs/Rhs are bounded (only the environment is
                // unbounded); if the Lhs/Rhs are themselves unbounded we cannot confirm it.
                bool rewriteBounded =
                    !HasUnboundedQuantifier(rule.Lhs) && rule.Subrules.All(sr => !HasUnboundedQuantifier(sr.Rhs));
                advisories.Add(
                    new GrammarAdvisory(
                        rule.Name,
                        stratum,
                        "phonological",
                        GrammarAdvisorySeverity.Escape,
                        "Unbounded rule environment: the left/right context matches an arbitrary-length span, so "
                            + "today's engine un-applies it at many positions — slow, and the composed automaton "
                            + "gains states.",
                        "Replace the '+'/'*' context with the fixed window the rule actually needs (usually 1–2 "
                            + "segments)."
                            + (
                                rewriteBounded
                                    ? " REGULAR (Kaplan & Kay 1994: a directional rewrite rule is a regular "
                                        + "relation however long its environment): the long-distance dependency "
                                        + "(e.g. vowel harmony / spreading) can be state-encoded into the FST — once "
                                        + "the compiler exists. It is still slow in today's engine."
                                    : " The rule's own LHS/RHS is unbounded, so regularity cannot be confirmed — "
                                        + "treat as non-regular."
                            ),
                        regular: rewriteBounded
                    )
                );
            }
            else
            {
                advisories.Add(
                    new GrammarAdvisory(
                        rule.Name,
                        stratum,
                        "phonological",
                        GrammarAdvisorySeverity.Info,
                        "Rewrite rule with a bounded environment: finite-state. It adds states to the composed "
                            + "transducer.",
                        "Keep the environment as tight as the language requires."
                    )
                );
            }

            // Deletion: the LHS is longer than every subrule's RHS. During analysis the parser must
            // guess where the deleted segments were and re-insert them (× DeletionReapplications),
            // which multiplies the search.
            int lhsSegments = CountConstraints(rule.Lhs);
            if (lhsSegments > 0 && rule.Subrules.All(sr => CountConstraints(sr.Rhs) < lhsSegments))
            {
                advisories.Add(
                    new GrammarAdvisory(
                        rule.Name,
                        stratum,
                        "phonological",
                        GrammarAdvisorySeverity.Cost,
                        "Deletion rule (LHS longer than RHS): during analysis the parser guesses where the "
                            + "deleted segments were and re-inserts them (× DeletionReapplications), multiplying "
                            + "the search.",
                        "Keep DeletionReapplications as low as the language needs; a bounded deletion context is "
                            + "still finite-state."
                    )
                );
            }
        }

        /// <summary>
        /// Whether the copied part named <paramref name="partName"/> is length-bounded — i.e. its
        /// defining <see cref="AffixProcessAllomorph.Lhs"/> pattern has no unbounded quantifier.
        /// Bounded ⇒ a finite copy ⇒ regular. Unresolved part ⇒ false (conservative: warn).
        /// </summary>
        private static bool IsPartBounded(AffixProcessAllomorph allomorph, string partName)
        {
            Pattern<Word, ShapeNode> part = allomorph.Lhs.FirstOrDefault(p => p.Name == partName);
            if (part == null)
                return false;
            return !HasUnboundedQuantifier(part);
        }

        private static bool HasUnboundedQuantifier(Pattern<Word, ShapeNode> pattern)
        {
            if (pattern == null)
                return false;
            return pattern
                .GetNodesDepthFirst()
                .OfType<Quantifier<Word, ShapeNode>>()
                .Any(q => q.MaxOccur == Quantifier<Word, ShapeNode>.Infinite);
        }

        private static int CountConstraints(Pattern<Word, ShapeNode> pattern)
        {
            if (pattern == null)
                return 0;
            return pattern.GetNodesDepthFirst().OfType<Constraint<Word, ShapeNode>>().Count();
        }
    }
}
