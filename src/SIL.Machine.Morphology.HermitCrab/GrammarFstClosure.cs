using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// The closure verdict for one non-regular escape: whether any FST-able rule could apply before
    /// it and thus <em>feed</em> it. A <see cref="Closed"/> escape cannot be reached by an FST-able
    /// derivation, so the FST's "no path" is a proof for words showing no escape signature.
    /// </summary>
    public sealed class EscapeClosure
    {
        public EscapeClosure(string rule, string stratum, bool closed, string reason)
        {
            Rule = rule;
            Stratum = stratum;
            Closed = closed;
            Reason = reason;
        }

        public string Rule { get; }
        public string Stratum { get; }
        public bool Closed { get; }
        public string Reason { get; }
    }

    /// <summary>The result of the static feeding-closure pass.</summary>
    public sealed class ClosureReport
    {
        public ClosureReport(IReadOnlyList<EscapeClosure> escapes)
        {
            Escapes = escapes;
        }

        public IReadOnlyList<EscapeClosure> Escapes { get; }

        /// <summary>
        /// True iff every non-regular escape is closed (vacuously true when there are none). When
        /// true, an FST built over the FST-able fragment is closed: its "no path" is a proof, not a
        /// guess — subject to the per-word surface check and the corpus parity gate (§9.5).
        /// </summary>
        public bool FstClosed => Escapes.All(e => e.Closed);

        public string Format()
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                FstClosed
                    ? "FST-CLOSED — no escape can be fed by an FST-able step; FST silence is a proof"
                    : "NOT closed — some escapes may be fed; those words need the search backstop"
            );
            foreach (EscapeClosure e in Escapes)
            {
                sb.AppendLine($"  [{(e.Closed ? "closed" : "fed")}] {e.Rule} (stratum '{e.Stratum}'): {e.Reason}");
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Static feeding-closure pre-filter (HERMITCRAB_FST_PLAN.md §9.1b / §9.5). For each non-regular
    /// escape (reduplication / infixation) it decides — by <em>stratal precedence</em> — whether any
    /// FST-able rule could apply before it and so create its trigger (Kiparsky feeding). An escape
    /// that nothing FST-able precedes is CLOSED: no FST-able derivation can feed it, so the FST
    /// (which excludes it) is complete for any word that shows no escape signature, and its silence
    /// is a proof.
    ///
    /// This is the conservative, SOUND pre-filter: it never falsely reports "closed" (any FST-able
    /// rule at or before the escape's stratum — which, under unordered application, could precede
    /// it — is treated as a potential feeder). The precise refinement that reclaims the
    /// over-flagged cases is the regular-emptiness test <c>range(F) ∩ trigger(E) = ∅</c> via
    /// <c>Fst.Intersect</c>; the empirical backstop is the corpus set-parity gate (FstVerification).
    /// </summary>
    public static class GrammarFstClosure
    {
        /// <summary>
        /// Run the feeding-closure pass. <paramref name="boundedReduplication"/> is an explicit caller
        /// assertion that the grammar's reduplication (and infixation) is <b>bounded for a fixed
        /// lexicon</b> — bounded copy count over finitely many stems — so the reduplicated/infixed
        /// language is <b>finite, hence regular</b> (compile-replace / Beesley–Karttunen), and a sibling
        /// generator (<see cref="ForwardSynthesisProposer"/>/<see cref="ReduplicationProposer"/>/
        /// <see cref="InfixProposer"/>) precompiles it. Under that assertion those constructs are no
        /// longer non-regular escapes, so a grammar whose only escapes are reduplication/infix becomes
        /// <see cref="ClosureReport.FstClosed"/>. It is opt-in (default off) and unsound if reduplication
        /// is genuinely unbounded/productive over novel stems; the parity gate + verify still backstop.
        /// </summary>
        public static ClosureReport Analyze(Language language, bool boundedReduplication = false)
        {
            IList<Stratum> strata = language.Strata;
            int count = strata.Count;

            // FST-able "feeders" per stratum: every morphological rule that is NOT a non-regular
            // escape (concatenative affixes, compounding) plus every phonological rule. With
            // boundedReduplication, reduplication/infix are FST-able (finite over a fixed lexicon), so
            // they count as feeders rather than escapes.
            var feedersPerStratum = new int[count];
            var escapes = new List<KeyValuePair<int, string>>();
            for (int i = 0; i < count; i++)
            {
                Stratum stratum = strata[i];
                foreach (IMorphologicalRule mrule in stratum.MorphologicalRules)
                {
                    if (IsEscape(mrule) && !boundedReduplication)
                    {
                        escapes.Add(new KeyValuePair<int, string>(i, ((AffixProcessRule)mrule).Name));
                    }
                    else
                    {
                        feedersPerStratum[i]++;
                    }
                }
                feedersPerStratum[i] += stratum.PhonologicalRules.Count;
            }

            var results = new List<EscapeClosure>();
            foreach (KeyValuePair<int, string> escape in escapes)
            {
                int index = escape.Key;
                int feedersBefore = 0;
                for (int j = 0; j < index; j++)
                {
                    feedersBefore += feedersPerStratum[j];
                }
                // Same-stratum feeders could precede the escape under unordered application, so they
                // count too (conservative).
                bool closed = feedersBefore == 0 && feedersPerStratum[index] == 0;
                string reason = closed
                    ? $"no FST-able rule applies at or before stratum '{strata[index].Name}' — nothing can feed it"
                    : $"FST-able rule(s) at or before stratum '{strata[index].Name}' could feed it "
                        + "(stratal pre-filter; refine with range∩trigger)";
                results.Add(new EscapeClosure(escape.Value, strata[index].Name, closed, reason));
            }
            return new ClosureReport(results);
        }

        private static bool IsEscape(IMorphologicalRule mrule)
        {
            if (!(mrule is AffixProcessRule affix))
            {
                return false;
            }
            foreach (AffixProcessAllomorph allomorph in affix.Allomorphs)
            {
                MorphOp op = MorphTokenCodec.ClassifyOp(allomorph, false);
                if (op == MorphOp.Reduplication || op == MorphOp.Infix)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
