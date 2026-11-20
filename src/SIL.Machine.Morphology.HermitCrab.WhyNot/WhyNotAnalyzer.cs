using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab.WhyNot;

public enum WhyNotOutcome
{
    Accepted,
    Rejected,
    Unknown,
}

public record ExpectedMorph(string MorphemeId, int? AllomorphIndex, string AllomorphId = null);

public record Failure(
    string Kind,
    string Plain,
    int? Position,
    string HcReason,
    string Provenance,
    IReadOnlyList<MorphRef> Morphs,
    MorphRef EnvironmentAllomorph,
    StemName StemName,
    IReadOnlyList<FeatureMismatch> Features
);

public record DerivationResult(IReadOnlyList<MorphRef> Morphs, Failure Failure, int Rank);

public record WhyNotResult(
    string Word,
    IReadOnlyList<ExpectedMorph> Expect,
    WhyNotOutcome Outcome,
    IReadOnlyList<MorphRef> Analysis,
    IReadOnlyList<DerivationResult> Results,
    string StopReason
);

/// <summary>
/// Runs a normal parse with a capturing trace manager, then reports the first validity failure of each derivation
/// whose morphs are exactly the expected ones. HermitCrab short-circuits validity, so only one failing check per
/// derivation is visible. Failures raised before a word is fully synthesized (rule gates) are not attributed.
/// </summary>
public static class WhyNotAnalyzer
{
    public static WhyNotResult Analyze(Language language, string word, IReadOnlyList<ExpectedMorph> expected) =>
        new WhyNotSession(language).Analyze(word, expected);

    internal static WhyNotResult Analyze(
        Morpher morpher,
        CapturingTraceManager trace,
        string word,
        IReadOnlyList<ExpectedMorph> expected
    )
    {
        List<Word> accepted = morpher.ParseWord(word).ToList();

        foreach (Word w in accepted)
        {
            IReadOnlyList<MorphRef> morphs = CapturingTraceManager.MorphsOf(w);
            if (Matches(morphs, expected))
            {
                return new WhyNotResult(
                    word,
                    expected,
                    WhyNotOutcome.Accepted,
                    morphs,
                    Array.Empty<DerivationResult>(),
                    null
                );
            }
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var results = new List<DerivationResult>();
        foreach (FailedEvent failed in trace.Failures.Where(f => Matches(f.Morphs, expected)))
        {
            Failure failure = Classify(failed);
            string key = string.Join("|", failed.Morphs) + "#" + failure.HcReason + "#" + failure.Plain;
            if (seen.Add(key))
                results.Add(new DerivationResult(failed.Morphs, failure, Rank(failed)));
        }

        if (results.Count == 0)
        {
            return new WhyNotResult(
                word,
                expected,
                WhyNotOutcome.Unknown,
                null,
                results,
                "no_derivation_reached_validation"
            );
        }

        results = results
            .OrderByDescending(r => r.Rank)
            .ThenBy(r => string.Join(",", r.Morphs.Select(m => m.MorphemeId)), StringComparer.Ordinal)
            .ToList();
        return new WhyNotResult(word, expected, WhyNotOutcome.Rejected, null, results, null);
    }

    public static bool Matches(IReadOnlyList<MorphRef> morphs, IReadOnlyList<ExpectedMorph> expected)
    {
        if (morphs.Count != expected.Count)
            return false;
        for (int i = 0; i < morphs.Count; i++)
        {
            if (!string.Equals(morphs[i].MorphemeId, expected[i].MorphemeId, StringComparison.Ordinal))
                return false;
            if (expected[i].AllomorphIndex is int index && index != morphs[i].AllomorphIndex)
                return false;
        }
        return true;
    }

    // Higher means more checks survived, in the order HermitCrab runs them.
    private static int Rank(FailedEvent failed)
    {
        int stage = failed.Reason switch
        {
            FailureReason.PartialParse => 1,
            FailureReason.ObligatorySyntacticFeatures => 2,
            FailureReason.BoundRoot
            or FailureReason.RequiredStemName
            or FailureReason.ExcludedStemName
            or FailureReason.RequiredSyntacticFeatureStruct => 3,
            FailureReason.AllomorphCoOccurrenceRules => 4,
            FailureReason.MorphemeCoOccurrenceRules => 5,
            FailureReason.Environments => 6,
            FailureReason.DisjunctiveAllomorph => 7,
            FailureReason.SurfaceFormMismatch => 8,
            _ => 0,
        };
        int position = PositionOf(failed) ?? 0;
        return stage * 1000 + position;
    }

    private static int? PositionOf(FailedEvent failed)
    {
        if (failed.Allomorph == null)
            return null;
        int index = failed.Word.AllomorphsInMorphOrder.ToList().IndexOf(failed.Allomorph);
        return index < 0 ? (int?)null : index;
    }

    private static MorphRef RefOf(Allomorph allomorph) =>
        new MorphRef(MorphemeIds.Of(allomorph.Morpheme), allomorph.Index, allomorph.Morpheme.Gloss);

    private static string Label(MorphRef morph) =>
        morph.AllomorphIndex < 0
            ? $"{morph.Gloss ?? morph.MorphemeId} ({morph.MorphemeId})"
            : $"{morph.Gloss ?? morph.MorphemeId} ({morph.MorphemeId}/{morph.AllomorphIndex})";

    private static Failure Classify(FailedEvent failed)
    {
        Allomorph allomorph = failed.Allomorph;
        int? position = PositionOf(failed);
        MorphRef culprit = allomorph == null ? null : RefOf(allomorph);
        IReadOnlyList<MorphRef> culpritList = culprit == null ? Array.Empty<MorphRef>() : new[] { culprit };
        string reason = failed.Reason.ToString();
        IReadOnlyList<FeatureMismatch> none = Array.Empty<FeatureMismatch>();

        switch (failed.Reason)
        {
            case FailureReason.AllomorphCoOccurrenceRules:
            case FailureReason.MorphemeCoOccurrenceRules:
            {
                (ConstraintType type, MorphCoOccurrenceAdjacency adjacency, List<MorphRef> others, List<bool> present) =
                    CoOccurrence(failed);
                // Prefer the partner that makes the rule fail (present for Exclude, absent for Require). A non-Anywhere
                // adjacency can fail with the opposite partner state, so fall back to any partner of the rule.
                List<MorphRef> partners = others
                    .Where((o, i) => present[i] == (type == ConstraintType.Exclude))
                    .ToList();
                if (partners.Count == 0)
                    partners = others;
                string verb = type == ConstraintType.Exclude ? "excludes" : "requires";
                string plain =
                    $"{Label(culprit)} {verb} {string.Join(" or ", others.Select(Label))} ({adjacency}) in this word.";
                return new Failure(
                    "adhoc_prohibition",
                    plain,
                    position,
                    reason,
                    "hc_reported",
                    new[] { culprit, partners[0] },
                    null,
                    null,
                    none
                );
            }
            case FailureReason.BoundRoot:
                return new Failure(
                    "stem_name",
                    $"{Label(culprit)} is a bound root and cannot stand alone.",
                    position,
                    reason,
                    "hc_reported",
                    culpritList,
                    null,
                    null,
                    none
                );
            case FailureReason.RequiredStemName:
            {
                var stemName = (StemName)failed.FailureObject;
                IReadOnlyList<FeatureMismatch> diff = stemName
                    .Regions.Select(r => FeatureStructDiff.Diff(r, failed.Word.SyntacticFeatureStruct))
                    .OrderBy(d => d.Count)
                    .First();
                return new Failure(
                    "stem_name",
                    $"{Label(culprit)} requires stem name {stemName}, but the word's features match none of its regions.",
                    position,
                    reason,
                    "hc_reported",
                    culpritList,
                    null,
                    stemName,
                    diff
                );
            }
            case FailureReason.ExcludedStemName:
            {
                var stemName = (StemName)failed.FailureObject;
                return new Failure(
                    "stem_name",
                    $"Another allomorph of the same entry has stem name {stemName}, which fits the word and excludes {Label(culprit)}.",
                    position,
                    reason,
                    "hc_reported",
                    culpritList,
                    null,
                    stemName,
                    none
                );
            }
            case FailureReason.RequiredSyntacticFeatureStruct:
            {
                var required = (FeatureStruct)failed.FailureObject;
                return new Failure(
                    "feature_clash",
                    $"{Label(culprit)} requires syntactic features that the word does not have.",
                    position,
                    reason,
                    "hc_reported",
                    culpritList,
                    null,
                    null,
                    FeatureStructDiff.Diff(required, failed.Word.SyntacticFeatureStruct)
                );
            }
            case FailureReason.ObligatorySyntacticFeatures:
            {
                var feature = (Feature)failed.FailureObject;
                failed.Word.SyntacticFeatureStruct.TryGetValue(feature, out FeatureValue actual);
                return new Failure(
                    "feature_clash",
                    $"The word must specify obligatory feature {feature.ID}.",
                    null,
                    reason,
                    "hc_reported",
                    Array.Empty<MorphRef>(),
                    null,
                    null,
                    new[] { new FeatureMismatch(feature.ID, "specified", actual?.ToString()) }
                );
            }
            case FailureReason.Environments:
                return new Failure(
                    "environment_unmet",
                    $"{Label(culprit)} occurs in none of its {((ICollection<AllomorphEnvironment>)failed.FailureObject).Count} allowed environment(s).",
                    position,
                    reason,
                    "hc_reported",
                    culpritList,
                    culprit,
                    null,
                    none
                );
            case FailureReason.SurfaceFormMismatch:
                return Other(
                    "surface_mismatch",
                    "The synthesized surface form differs from the word.",
                    failed,
                    position
                );
            // RequiredMprFeatures and ExcludedMprFeatures never arrive as Failed events: HermitCrab reports them
            // through MorphologicalRuleNotApplied, which carries no completed derivation, so they stay other_gate.
            default:
                return Other("other_gate", $"The word failed the {failed.Reason} check.", failed, position);
        }
    }

    private static Failure Other(string kind, string plain, FailedEvent failed, int? position)
    {
        MorphRef culprit = failed.Allomorph == null ? null : RefOf(failed.Allomorph);
        return new Failure(
            kind,
            plain,
            position,
            failed.Reason.ToString(),
            "hc_reported",
            culprit == null ? Array.Empty<MorphRef>() : new[] { culprit },
            null,
            null,
            Array.Empty<FeatureMismatch>()
        );
    }

    private static (ConstraintType, MorphCoOccurrenceAdjacency, List<MorphRef>, List<bool>) CoOccurrence(
        FailedEvent failed
    )
    {
        IReadOnlyList<Allomorph> inWord = failed.Word.AllomorphsInMorphOrder.ToList();
        var others = new List<MorphRef>();
        var present = new List<bool>();
        switch (failed.FailureObject)
        {
            case AllomorphCoOccurrenceRule rule:
                foreach (Allomorph other in rule.Others)
                {
                    others.Add(RefOf(other));
                    present.Add(inWord.Contains(other));
                }
                return (rule.Type, rule.Adjacency, others, present);
            case MorphemeCoOccurrenceRule rule:
                foreach (Morpheme other in rule.Others)
                {
                    Allomorph inWordAllomorph = inWord.FirstOrDefault(a => a.Morpheme == other);
                    others.Add(
                        inWordAllomorph != null
                            ? RefOf(inWordAllomorph)
                            : new MorphRef(MorphemeIds.Of(other), -1, other.Gloss)
                    );
                    present.Add(inWordAllomorph != null);
                }
                return (rule.Type, rule.Adjacency, others, present);
            default:
                throw new InvalidOperationException("Unexpected co-occurrence failure object.");
        }
    }
}
