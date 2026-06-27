using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Template-based analysis with build-time category gating (HERMITCRAB_FST_PLAN.md §6/§10): a
/// suffixing affix template attaches only to roots whose category matches, and the token-
/// accumulating walk reproduces the search engine's analyses — including NOT over-generating the
/// template onto a wrong-category root.
/// </summary>
public class FstTemplateAnalyzerTests : HermitCrabTestBase
{
    private AffixProcessRule Suffix(string name, string gloss, string seg)
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var rule = new AffixProcessRule { Name = name, Gloss = gloss };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, seg) },
            }
        );
        return rule;
    }

    private AffixProcessRule Prefix(string name, string gloss, string seg)
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var rule = new AffixProcessRule { Name = name, Gloss = gloss };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new InsertSegments(Table3, seg), new CopyFromInput("1") },
            }
        );
        return rule;
    }

    [Test]
    public void Analyze_SlotAffixWrongCategory_PrunedNotOvergenerated()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        AffixProcessRule ok = Suffix("ok_suffix", "OK", "d"); // no category requirement → applies to V
        var wrong = new AffixProcessRule
        {
            Name = "n_only_suffix",
            Gloss = "NS",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
        };
        wrong.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "z") },
            }
        );
        var verbTemplate = new AffixTemplate
        {
            Name = "verb",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        verbTemplate.Slots.Add(new AffixTemplateSlot(ok) { Optional = true });
        verbTemplate.Slots.Add(new AffixTemplateSlot(wrong) { Optional = true });
        Morphophonemic.AffixTemplates.Add(verbTemplate);

        var search = new Morpher(TraceManager, Language);
        var fst = new FstTemplateAnalyzer(Language);

        // sag is V. "sagd" uses the OK suffix (valid); "sagz" would use the N-only suffix on a V
        // root — the build-time category gate prunes it, so the FST must NOT over-generate it.
        string[] corpus = { "sag", "sagd", "sagz" };
        AnalysisComparison comparison = FstVerification.Compare(search, fst, corpus);
        Assert.That(comparison.IsComplete, Is.True, comparison.Format());

        Morphophonemic.AffixTemplates.Remove(verbTemplate);
    }

    [Test]
    public void Build_ReduplicationSlot_DegradesGracefully_DoesNotThrow()
    {
        // A reduplication slot is non-regular and unbuildable. The proposer must SKIP it (degrade),
        // not throw and abort the whole build — and flag the grammar as not fully covered.
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var redup = new AffixProcessRule { Name = "redup", Gloss = "RED" };
        redup.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new CopyFromInput("1") }, // copy the stem twice = reduplication
            }
        );
        var t = new AffixTemplate
        {
            Name = "redup_tmpl",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        t.Slots.Add(new AffixTemplateSlot(redup) { Optional = true });
        Morphophonemic.AffixTemplates.Add(t);

        FstTemplateAnalyzer? fst = null;
        Assert.DoesNotThrow(() => fst = new FstTemplateAnalyzer(Language), "an unbuildable slot must degrade, not throw");
        Assert.That(fst!.CoversAllConstructs, Is.False, "reduplication slot → grammar not fully covered (won't certify)");
        Assert.That(fst!.AnalyzeWord("sag"), Is.Not.Empty, "the rest of the grammar still analyzes");

        Morphophonemic.AffixTemplates.Remove(t);
    }

    [Test]
    public void Analyze_ZeroSegmentSuffix_IsEmitted_NotDropped()
    {
        // A true zero-segment affix (CopyFromInput only, no InsertSegments) must still emit its
        // morpheme token (it adds no segments). Previously it threw / was silently dropped.
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var zero = new AffixProcessRule { Name = "zero_sfx", Gloss = "Z" };
        zero.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1") }, // copy stem, insert nothing = zero affix
            }
        );
        var t = new AffixTemplate
        {
            Name = "zero_tmpl",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        t.Slots.Add(new AffixTemplateSlot(zero) { Optional = true });
        Morphophonemic.AffixTemplates.Add(t);

        var search = new Morpher(TraceManager, Language);
        var fst = new FstTemplateAnalyzer(Language);
        Assert.That(fst.CoversAllConstructs, Is.True, "a zero-segment affix is buildable, not a skipped construct");
        // Whatever the engine yields for "sag" (bare root and/or root+Z), the FST must match it —
        // i.e. it must not drop the zero-suffixed analysis.
        AnalysisComparison comparison = FstVerification.Compare(search, fst, new[] { "sag" });
        Assert.That(comparison.IsComplete, Is.True, comparison.Format());

        Morphophonemic.AffixTemplates.Remove(t);
    }

    [Test]
    public void Analyze_PrefixAndSuffixTemplate_MatchesSearch()
    {
        // A verb template with a prefix slot (di-) and a suffix slot (-d), restricted to V roots.
        AffixProcessRule di = Prefix("di_prefix", "PST", "di");
        AffixProcessRule ed = Suffix("ed_suffix", "PERF", "d");
        var verbTemplate = new AffixTemplate
        {
            Name = "verb",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        verbTemplate.Slots.Add(new AffixTemplateSlot(di) { Optional = true });
        verbTemplate.Slots.Add(new AffixTemplateSlot(ed) { Optional = true });
        Morphophonemic.AffixTemplates.Add(verbTemplate);

        var search = new Morpher(TraceManager, Language);
        var fst = new FstTemplateAnalyzer(Language);

        // sag (V, Morphophonemic): bare, prefixed (disag), suffixed (sagd), both (disagd).
        string[] corpus = { "sag", "disag", "sagd", "disagd", "gab", "digab" };
        AnalysisComparison comparison = FstVerification.Compare(search, fst, corpus);
        Assert.That(comparison.IsComplete, Is.True, comparison.Format());

        Morphophonemic.AffixTemplates.Remove(verbTemplate);
    }

    [Test]
    public void Analyze_SuffixTemplateWithCategoryGate_MatchesSearch()
    {
        // A verb template, restricted to V roots, with two optional suffix slots.
        AffixProcessRule ed = Suffix("ed_suffix", "PAST", "d");
        AffixProcessRule wit = Suffix("evidential", "WIT", "v");
        var verbTemplate = new AffixTemplate
        {
            Name = "verb",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        verbTemplate.Slots.Add(new AffixTemplateSlot(ed) { Optional = true });
        verbTemplate.Slots.Add(new AffixTemplateSlot(wit) { Optional = true });
        Morphophonemic.AffixTemplates.Add(verbTemplate);

        var search = new Morpher(TraceManager, Language);
        var fst = new FstTemplateAnalyzer(Language);

        // Same-stratum (Morphophonemic) roots so only the category gate is in play: sag (32, V)
        // takes the template; gab (11, A) must NOT. "sagdv" exercises both slots; "gabd" must yield
        // no analysis in either engine (the gate blocks the verb template on the A root).
        string[] corpus = { "sag", "sagd", "sagdv", "gab", "gabd" };
        AnalysisComparison comparison = FstVerification.Compare(search, fst, corpus);
        Assert.That(comparison.IsComplete, Is.True, comparison.Format());

        Morphophonemic.AffixTemplates.Remove(verbTemplate);
    }
}
