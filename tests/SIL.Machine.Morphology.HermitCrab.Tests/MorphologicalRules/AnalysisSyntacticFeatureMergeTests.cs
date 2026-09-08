using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

// Adversarial characterisation of sillsdev/machine PR #494 ("Change Add to PriorityUnion" in HermitCrab
// analysis) and a stronger "Exact" variant (see AnalysisSyntacticFeatureMerge for the three modes).
//
// The question for every case: does a mode DROP a parse that synthesis would accept? Analysis
// over-generation is harmless (synthesis re-verifies every candidate from the true root forward, in the
// forward direction, independent of whatever the analysis-side bookkeeping FS says); analysis
// under-generation loses parses. Comments on each test say, per mode, what is expected and why, before
// reporting what was actually observed.
internal class AnalysisSyntacticFeatureMergeTests : HermitCrabTestBase
{
    private AnalysisSyntacticFeatureMergeMode _savedMode;

    [SetUp]
    public void SaveMode()
    {
        _savedMode = AnalysisSyntacticFeatureMerge.Mode;
    }

    [TearDown]
    public void RestoreMode()
    {
        AnalysisSyntacticFeatureMerge.Mode = _savedMode;
        AnalysisSyntacticFeatureMerge.ResetCounters();
    }

    // ---------------------------------------------------------------------
    // helpers
    // ---------------------------------------------------------------------

    private Word MakeWord(string shape, FeatureStruct syntacticFS)
    {
        var word = new Word(Morphophonemic, Morphophonemic.CharacterDefinitionTable.Segment(shape));
        word.SyntacticFeatureStruct = syntacticFS;
        word.Freeze();
        return word;
    }

    // An affix rule whose allomorph never changes the shape (CopyFromInput only). Used for the
    // direct-instantiation tests, where only the FS bookkeeping matters and phonology would just add noise.
    private static AffixProcessRule MakeIdentityRule(
        string name,
        FeatureStruct required,
        FeatureStruct outFs,
        int maxApplicationCount = 1
    )
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var rule = new AffixProcessRule
        {
            Name = name,
            Gloss = name,
            RequiredSyntacticFeatureStruct = required,
            OutSyntacticFeatureStruct = outFs,
            MaxApplicationCount = maxApplicationCount,
        };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1") },
            }
        );
        return rule;
    }

    // An affix rule that appends `insert` (in Table3) so that decompositions of different depth produce
    // distinct surface strings -- needed for end-to-end ParseWord/AssertMorphsEqual tests, where an
    // identity rule would be indistinguishable from "no rule applied".
    private AffixProcessRule MakeSuffixRule(string name, FeatureStruct required, FeatureStruct outFs, string insert)
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var rule = new AffixProcessRule
        {
            Name = name,
            Gloss = name,
            RequiredSyntacticFeatureStruct = required,
            OutSyntacticFeatureStruct = outFs,
        };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, insert) },
            }
        );
        return rule;
    }

    private static void AssertFSEqual(FeatureStruct actual, FeatureStruct expected, string because)
    {
        Assert.That(actual, Is.EqualTo(expected).Using(FreezableEqualityComparer<FeatureStruct>.Default), because);
    }

    private FeatureStruct Pos(params string[] symbols) => FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol(symbols).Value;

    private static readonly FeatureStruct Empty = FeatureStruct.New().Value;

    // =======================================================================================================
    // Task 1: Maxwell's PR scenario -- three category-changing derivational rules.
    // =======================================================================================================

    // Direct assertion on AnalysisAffixProcessRule output (the unit test ddaspit asked for).
    //
    // ruleA2B: Required=N, Out=V.  ruleB2A: Required=V, Out=N.  ruleThird: Required=N, Out=V (same shape as
    // ruleA2B -- deliberately: it asks "could THIRD have been the last rule applied here", which is only
    // possible if the current word's POS is unifiable with V).
    //
    // Starting from a surface word with POS=N and un-applying B2A then A2B:
    //   Add:           POS ends up {N,V} (union -- Add never narrows, it only accumulates).
    //   PriorityUnion: POS ends up N (required overwrites, narrowing every step).
    //   Exact:         POS ends up N (RemovePaths+Unify narrows the same way here, since this is a pure
    //                  top-level scalar POS flip with no sub-structure to preserve).
    //
    // Then trying ruleThird (Out=V) against that result:
    //   Add:           {N,V} is unifiable with V -> CanUnapply succeeds -> rule is attempted (not rejected).
    //   PriorityUnion: N is NOT unifiable with V -> CanUnapply fails -> CheckRejects increments.
    //   Exact:         same as PriorityUnion here (checkFs = PU(N,V) = V, and input is N) -> rejected.
    [TestCase(AnalysisSyntacticFeatureMergeMode.Add)]
    [TestCase(AnalysisSyntacticFeatureMergeMode.PriorityUnion)]
    [TestCase(AnalysisSyntacticFeatureMergeMode.Exact)]
    public void CategoryChangeChain_AccumulatedPosAndThirdRuleGate_MatchModeSemantics(AnalysisSyntacticFeatureMergeMode mode)
    {
        AnalysisSyntacticFeatureMerge.Mode = mode;
        AnalysisSyntacticFeatureMerge.ResetCounters();

        FeatureStruct posA = Pos("N");
        FeatureStruct posB = Pos("V");

        AffixProcessRule ruleA2B = MakeIdentityRule("a2b", posA, posB);
        AffixProcessRule ruleB2A = MakeIdentityRule("b2a", posB, posA);
        AffixProcessRule ruleThird = MakeIdentityRule("third", posA, posB);

        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        var analysisB2A = new AnalysisAffixProcessRule(morpher, ruleB2A);
        var analysisA2B = new AnalysisAffixProcessRule(morpher, ruleA2B);
        var analysisThird = new AnalysisAffixProcessRule(morpher, ruleThird);

        // Synthesis chain being un-applied: root(A) --a2b--> B --b2a--> A == surface.
        Word surface = MakeWord("sag", posA);
        Word afterB2A = analysisB2A.Apply(surface).Single();
        Word afterA2B = analysisA2B.Apply(afterB2A).Single();

        FeatureStruct expectedPos = mode == AnalysisSyntacticFeatureMergeMode.Add ? Pos("N", "V") : posA;
        AssertFSEqual(
            afterA2B.SyntacticFeatureStruct,
            expectedPos,
            $"{mode}: after two un-applications the accumulated POS should be {(mode == AnalysisSyntacticFeatureMergeMode.Add ? "{N,V}" : "N")}."
        );

        long rejectsBefore = AnalysisSyntacticFeatureMerge.CheckRejects;
        List<Word> thirdResult = analysisThird.Apply(afterA2B).ToList();
        long rejectsAfter = AnalysisSyntacticFeatureMerge.CheckRejects;

        if (mode == AnalysisSyntacticFeatureMergeMode.Add)
        {
            Assert.That(
                rejectsAfter,
                Is.EqualTo(rejectsBefore),
                "Add: the ambiguous {N,V} POS is unifiable with Out=V, so the third rule is attempted, not rejected."
            );
            Assert.That(thirdResult, Is.Not.Empty, "Add: the third rule's pattern matches trivially once attempted.");
        }
        else
        {
            Assert.That(
                rejectsAfter,
                Is.EqualTo(rejectsBefore + 1),
                $"{mode}: POS has narrowed to N, which is not unifiable with Out=V, so the third rule is rejected before pattern matching."
            );
            Assert.That(thirdResult, Is.Empty, $"{mode}: CanUnapply gate rejected the rule, so there is no output.");
        }
    }

    // End-to-end: does the FULL four-morph decomposition (root a2b b2a third) exist as a parse of the
    // surface form, and is it found identically in all three modes?
    //
    // This is the headline finding: root+a2b+b2a+third is a LEGITIMATE decomposition (re-synthesis from the
    // true root succeeds in every mode -- root(N) -a2b-> V -b2a-> N -third-> V is a perfectly valid forward
    // chain), but PriorityUnion and Exact's analysis-side bookkeeping narrows the accumulated POS to N right
    // after un-applying b2a/a2b, so CanUnapply rejects "third" (Out=V) before it is ever tried -- the
    // decomposition is never generated as a candidate, so it is never offered to Synthesize() for
    // re-verification. Add's looser bookkeeping keeps POS={N,V}, so "third" is tried, and Synthesize()
    // confirms it against the true root chain. Prediction: Add finds it; PriorityUnion and Exact do not.
    [Test]
    public void CategoryChangeChain_EndToEnd_PriorityUnionAndExactLoseAFourthMorphParseThatAddFinds()
    {
        FeatureStruct posA = Pos("N");
        FeatureStruct posB = Pos("V");
        AddEntry("flipRoot", posA, Morphophonemic, "zim");

        var results = new Dictionary<AnalysisSyntacticFeatureMergeMode, List<Word>>();
        foreach (AnalysisSyntacticFeatureMergeMode mode in new[]
        {
            AnalysisSyntacticFeatureMergeMode.Add,
            AnalysisSyntacticFeatureMergeMode.PriorityUnion,
            AnalysisSyntacticFeatureMergeMode.Exact,
        })
        {
            Morphophonemic.MorphologicalRules.Clear();
            var a2b = MakeIdentityRule("a2b", posA, posB);
            var b2a = MakeIdentityRule("b2a", posB, posA);
            var third = MakeIdentityRule("third", posA, posB);
            Morphophonemic.MorphologicalRules.Add(a2b);
            Morphophonemic.MorphologicalRules.Add(b2a);
            Morphophonemic.MorphologicalRules.Add(third);

            AnalysisSyntacticFeatureMerge.Mode = mode;
            AnalysisSyntacticFeatureMerge.ResetCounters();
            var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
            results[mode] = morpher.ParseWord("zim").ToList();
        }

        static bool FoundFourMorphParse(List<Word> words) =>
            words.Any(w => w.AllomorphsInMorphOrder.Select(m => m.Morpheme.Gloss).SequenceEqual(new[] { "flipRoot", "a2b", "b2a", "third" }));

        Assert.That(
            FoundFourMorphParse(results[AnalysisSyntacticFeatureMergeMode.Add]),
            Is.True,
            "Add should find the 4-morph decomposition: its accumulated {N,V} POS lets CanUnapply try 'third', and re-synthesis from the true root confirms it."
        );
        // Record what actually happens for PriorityUnion/Exact -- this is the load-bearing check.
        Console.WriteLine(
            $"PriorityUnion found 4-morph parse: {FoundFourMorphParse(results[AnalysisSyntacticFeatureMergeMode.PriorityUnion])}"
        );
        Console.WriteLine(
            $"Exact found 4-morph parse: {FoundFourMorphParse(results[AnalysisSyntacticFeatureMergeMode.Exact])}"
        );
        Assert.That(
            FoundFourMorphParse(results[AnalysisSyntacticFeatureMergeMode.PriorityUnion]),
            Is.False,
            "PriorityUnion: POS narrows to N after un-applying a2b, which is not unifiable with third's Out=V, so CanUnapply rejects 'third' and this valid decomposition is never generated."
        );
        Assert.That(
            FoundFourMorphParse(results[AnalysisSyntacticFeatureMergeMode.Exact]),
            Is.False,
            "Exact: checkFs for 'third' is PU(N,V)=V, and the accumulated POS is N (same narrowing as PriorityUnion in this pure top-level-scalar case), so it is rejected too -- Exact is not a strict superset of Add's coverage."
        );
    }

    // =======================================================================================================
    // Task 2: override-loss case -- does a mode retain a rule's Out after it should have been superseded?
    // =======================================================================================================

    // root: V (bare).  inner: Required=V, Out=Head:[tense:pres].  outer: Required=V, Out=Head:[tense:past].
    // outermost: Required=Head:[tense:past], Out=empty.  All identity except for the FS bookkeeping; wired
    // through a mandatory 3-slot AffixTemplate so the un-application order is deterministic (outermost,
    // then outer, then inner -- reverse of synthesis) instead of an Unordered-stratum combinatorial search.
    //
    // Prediction (see AnalysisSyntacticFeatureMerge.cs comments and MEMORY): un-applying "outer" leaves
    // tense:past on the word under Add AND PriorityUnion, because neither mode removes the feature paths
    // that Out contributed -- only Exact's RemovePaths does. Un-applying "inner" then checks
    // Out=tense:pres against a word that (wrongly) still says tense:past -> rejected for both Add and
    // PriorityUnion. This is a PRE-EXISTING MASTER BUG (present in Add), not something PR #494 introduced.
    [Test]
    public void OverrideLoss_TenseFlipFlop_AddAndPriorityUnionLoseTheParse_ExactFindsIt()
    {
        FeatureStruct bareV = Pos("V");
        FeatureStruct tensePres = FeatureStruct.New(Language.SyntacticFeatureSystem).Feature(Head).EqualTo(head => head.Feature("tense").EqualTo("pres")).Value;
        FeatureStruct tensePast = FeatureStruct.New(Language.SyntacticFeatureSystem).Feature(Head).EqualTo(head => head.Feature("tense").EqualTo("past")).Value;

        AddEntry("olRoot", bareV, Morphophonemic, "zud");

        var results = new Dictionary<AnalysisSyntacticFeatureMergeMode, List<Word>>();
        foreach (AnalysisSyntacticFeatureMergeMode mode in new[]
        {
            AnalysisSyntacticFeatureMergeMode.Add,
            AnalysisSyntacticFeatureMergeMode.PriorityUnion,
            AnalysisSyntacticFeatureMergeMode.Exact,
        })
        {
            Morphophonemic.AffixTemplates.Clear();
            var inner = MakeSuffixRule("olInner", bareV, tensePres, "i");
            var outer = MakeSuffixRule("olOuter", bareV, tensePast, "u");
            var outermost = MakeSuffixRule("olOutermost", tensePast, Empty, "a");
            var template = new AffixTemplate { Name = "olTemplate" };
            template.Slots.Add(new AffixTemplateSlot(inner));
            template.Slots.Add(new AffixTemplateSlot(outer));
            template.Slots.Add(new AffixTemplateSlot(outermost));
            Morphophonemic.AffixTemplates.Add(template);

            AnalysisSyntacticFeatureMerge.Mode = mode;
            AnalysisSyntacticFeatureMerge.ResetCounters();
            var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
            results[mode] = morpher.ParseWord("zudiua").ToList();
        }

        static bool FoundChain(List<Word> words) =>
            words.Any(w => w.AllomorphsInMorphOrder.Select(m => m.Morpheme.Gloss).SequenceEqual(new[] { "olRoot", "olInner", "olOuter", "olOutermost" }));

        Console.WriteLine($"Add found chain: {FoundChain(results[AnalysisSyntacticFeatureMergeMode.Add])}");
        Console.WriteLine($"PriorityUnion found chain: {FoundChain(results[AnalysisSyntacticFeatureMergeMode.PriorityUnion])}");
        Console.WriteLine($"Exact found chain: {FoundChain(results[AnalysisSyntacticFeatureMergeMode.Exact])}");

        Assert.That(
            FoundChain(results[AnalysisSyntacticFeatureMergeMode.Add]),
            Is.False,
            "Add: leftover tense:past (never cleared) makes 'inner's Out=tense:pres unifiable check fail -- PRE-EXISTING MASTER BUG."
        );
        Assert.That(
            FoundChain(results[AnalysisSyntacticFeatureMergeMode.PriorityUnion]),
            Is.False,
            "PriorityUnion: same leftover-tense bug as Add -- PR #494 does not fix this."
        );
        Assert.That(
            FoundChain(results[AnalysisSyntacticFeatureMergeMode.Exact]),
            Is.True,
            "Exact: RemovePaths(fs, outFs) strips tense:past before folding in 'outer's Required, so 'inner' correctly finds an untensed word to attach to."
        );
    }

    // =======================================================================================================
    // Task 3: smaller mechanical sub-cases.
    // =======================================================================================================

    // 3a. Disjunctive Required {N,V} folded onto an accumulated single POS=N (Out left empty so CanUnapply
    // is trivially satisfied and only the merge itself is under test).
    //   Add:           Add({N,V}) onto N unions to {N,V}.
    //   PriorityUnion: PriorityUnion({N,V}) onto N overwrites wholesale with {N,V} (same result as Add here).
    //   Exact:         RemovePaths is a no-op (Out empty); Unify({N,V}, N) narrows to N (intersection).
    [TestCase(AnalysisSyntacticFeatureMergeMode.Add)]
    [TestCase(AnalysisSyntacticFeatureMergeMode.PriorityUnion)]
    [TestCase(AnalysisSyntacticFeatureMergeMode.Exact)]
    public void DisjunctiveRequired_MeetingSinglePos_NarrowsOnlyUnderExact(AnalysisSyntacticFeatureMergeMode mode)
    {
        AnalysisSyntacticFeatureMerge.Mode = mode;
        AnalysisSyntacticFeatureMerge.ResetCounters();

        FeatureStruct posN = Pos("N");
        FeatureStruct posNV = Pos("N", "V");
        AffixProcessRule rule = MakeIdentityRule("disjReq", posNV, Empty);
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        var analysisRule = new AnalysisAffixProcessRule(morpher, rule);

        Word result = analysisRule.Apply(MakeWord("zog", posN)).Single();

        FeatureStruct expected = mode == AnalysisSyntacticFeatureMergeMode.Exact ? posN : posNV;
        AssertFSEqual(
            result.SyntacticFeatureStruct,
            expected,
            $"{mode}: disjunctive Required folded onto POS=N should give {(mode == AnalysisSyntacticFeatureMergeMode.Exact ? "N (narrowed)" : "{N,V} (widened)")}."
        );
    }

    // 3b. Non-overlapping nested head features (required head:[num:pl] meeting accumulated head:[tense:past])
    // should merge identically in all three modes -- there is nothing to override or remove.
    [TestCase(AnalysisSyntacticFeatureMergeMode.Add)]
    [TestCase(AnalysisSyntacticFeatureMergeMode.PriorityUnion)]
    [TestCase(AnalysisSyntacticFeatureMergeMode.Exact)]
    public void NonOverlappingNestedHeadFeatures_MergeIdenticallyInAllModes(AnalysisSyntacticFeatureMergeMode mode)
    {
        AnalysisSyntacticFeatureMerge.Mode = mode;
        AnalysisSyntacticFeatureMerge.ResetCounters();

        FeatureStruct tensePast = FeatureStruct.New(Language.SyntacticFeatureSystem).Feature(Head).EqualTo(head => head.Feature("tense").EqualTo("past")).Value;
        FeatureStruct numPl = FeatureStruct.New(Language.SyntacticFeatureSystem).Feature(Head).EqualTo(head => head.Feature("num").EqualTo("pl")).Value;
        FeatureStruct expected = FeatureStruct
            .New(Language.SyntacticFeatureSystem)
            .Feature(Head)
            .EqualTo(head => head.Feature("tense").EqualTo("past").Feature("num").EqualTo("pl"))
            .Value;

        AffixProcessRule rule = MakeIdentityRule("nested", numPl, Empty);
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        var analysisRule = new AnalysisAffixProcessRule(morpher, rule);

        Word result = analysisRule.Apply(MakeWord("zog", tensePast)).Single();

        AssertFSEqual(result.SyntacticFeatureStruct, expected, $"{mode}: non-overlapping nested features should merge the same way in every mode.");
    }

    // 3c. The Clear() branch: an empty-Required/empty-Out rule following a rule that established POS=N.
    //   Add / PriorityUnion: both explicitly Clear() the whole FS when required and out are both empty.
    //   Exact: never Clear()s (RemovePaths(fs, empty)=no-op, and Unify is skipped when required.IsEmpty),
    //          so the previously-established POS=N survives.
    [TestCase(AnalysisSyntacticFeatureMergeMode.Add)]
    [TestCase(AnalysisSyntacticFeatureMergeMode.PriorityUnion)]
    [TestCase(AnalysisSyntacticFeatureMergeMode.Exact)]
    public void EmptyRequiredAndOut_ClearsFS_ExceptUnderExact(AnalysisSyntacticFeatureMergeMode mode)
    {
        AnalysisSyntacticFeatureMerge.Mode = mode;
        AnalysisSyntacticFeatureMerge.ResetCounters();

        FeatureStruct posN = Pos("N");
        AffixProcessRule ruleX = MakeIdentityRule("clearX", posN, Empty);
        AffixProcessRule ruleY = MakeIdentityRule("clearY", Empty, Empty);
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        var analysisX = new AnalysisAffixProcessRule(morpher, ruleX);
        var analysisY = new AnalysisAffixProcessRule(morpher, ruleY);

        Word afterX = analysisX.Apply(MakeWord("zog", posN)).Single();
        AssertFSEqual(afterX.SyntacticFeatureStruct, posN, $"{mode}: sanity check -- ruleX alone should fold in POS=N.");

        Word afterY = analysisY.Apply(afterX).Single();

        if (mode == AnalysisSyntacticFeatureMergeMode.Exact)
        {
            AssertFSEqual(
                afterY.SyntacticFeatureStruct,
                posN,
                "Exact never Clear()s: an empty-Required/empty-Out rule should leave the accumulated FS untouched."
            );
        }
        else
        {
            AssertFSEqual(
                afterY.SyntacticFeatureStruct,
                Empty,
                $"{mode}: an empty-Required/empty-Out rule explicitly Clear()s the accumulated FS, discarding POS=N."
            );
        }
    }

    // 3d. A rule applied twice (MaxApplicationCount=2): Required=Head:[num:pl], Out=Head:[num:sg].
    // Un-applying it once takes num:sg -> (Add: {sg,pl}; PriorityUnion/Exact: pl). Trying to un-apply it a
    // SECOND time in a row asks "could this same rule also have produced the word one step up?", which
    // requires Out=sg to be unifiable with the current accumulated value.
    //   Add:           {sg,pl} is unifiable with sg -> second application is attempted.
    //   PriorityUnion: pl is NOT unifiable with sg -> second application rejected.
    //   Exact:         checkFs = PU(pl,sg) = sg (Out overwrites); pl is NOT unifiable with sg -> rejected.
    // Note: a chain that un-applies the same Required=pl/Out=sg rule twice in a row is not something real
    // synthesis could have produced either (synthesizing it twice requires num=pl again immediately after
    // num=sg was just set, which the rule's own Required forbids) -- so Add's extra attempt here is exactly
    // the "harmless over-generation, caught at re-synthesis" case, unlike Task 1's flip-flop.
    [TestCase(AnalysisSyntacticFeatureMergeMode.Add)]
    [TestCase(AnalysisSyntacticFeatureMergeMode.PriorityUnion)]
    [TestCase(AnalysisSyntacticFeatureMergeMode.Exact)]
    public void SameRuleAppliedTwice_SecondApplicationGatedDifferentlyByMode(AnalysisSyntacticFeatureMergeMode mode)
    {
        AnalysisSyntacticFeatureMerge.Mode = mode;
        AnalysisSyntacticFeatureMerge.ResetCounters();

        FeatureStruct numPl = FeatureStruct.New(Language.SyntacticFeatureSystem).Feature(Head).EqualTo(head => head.Feature("num").EqualTo("pl")).Value;
        FeatureStruct numSg = FeatureStruct.New(Language.SyntacticFeatureSystem).Feature(Head).EqualTo(head => head.Feature("num").EqualTo("sg")).Value;
        AffixProcessRule ruleZ = MakeIdentityRule("twiceZ", numPl, numSg, maxApplicationCount: 2);
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        var analysisZ = new AnalysisAffixProcessRule(morpher, ruleZ);

        Word afterFirst = analysisZ.Apply(MakeWord("zog", numSg)).Single();
        List<Word> afterSecond = analysisZ.Apply(afterFirst).ToList();

        if (mode == AnalysisSyntacticFeatureMergeMode.Add)
        {
            Assert.That(afterSecond, Is.Not.Empty, "Add: {sg,pl} is unifiable with Out=sg, so a second un-application is attempted.");
        }
        else
        {
            Assert.That(afterSecond, Is.Empty, $"{mode}: the narrowed accumulated value (pl) is not unifiable with Out=sg, so the second un-application is rejected.");
        }
    }

    // =======================================================================================================
    // Task 4: guessed roots.
    // =======================================================================================================

    // Reuses the Task 2 tense flip-flop, but with NO dictionary entry for the root -- only a lexical
    // pattern ("[Seg]+", any nonempty shape) with a fixed SyntacticFeatureStruct=V (bare). guessRoot:true
    // forces Morpher.LexicalGuess.
    //
    // IMPORTANT finding (see Morpher.LexicalGuess, lines ~483-509): when the lexical pattern's owning
    // LexEntry is non-null (always true for patterns discovered via stratum.Entries, which is the only way
    // Morpher populates _lexicalPatterns), the guessed LexEntry's SyntacticFeatureStruct is unconditionally
    // OVERWRITTEN with the pattern LexEntry's own fixed FS (`lexEntry.SyntacticFeatureStruct =
    // patternEntry.SyntacticFeatureStruct`), not left as `input.SyntacticFeatureStruct` (which line ~490
    // sets first but which then gets clobbered). So the analysis-accumulated FS built up by
    // Add/PriorityUnion/Exact NEVER reaches the guessed root -- contrary to what one might assume, the
    // merge mode cannot bias which category a guessed root receives. What it CAN still do is change which
    // analysisWord candidates exist to guess from at all -- which is exactly Task 2's bug, so we expect the
    // same Add/PriorityUnion-lose, Exact-finds split here, for the SAME underlying reason as Task 2, not
    // because of anything specific to guessing.
    [Test]
    public void GuessedRoot_OverrideLossBugAlsoBreaksGuessing_ButNotViaTheGuessedEntrysFS()
    {
        FeatureStruct bareV = Pos("V");
        FeatureStruct tensePres = FeatureStruct.New(Language.SyntacticFeatureSystem).Feature(Head).EqualTo(head => head.Feature("tense").EqualTo("pres")).Value;
        FeatureStruct tensePast = FeatureStruct.New(Language.SyntacticFeatureSystem).Feature(Head).EqualTo(head => head.Feature("tense").EqualTo("past")).Value;

        // No concrete root entry -- only a lexical pattern, so LexicalLookup finds nothing and ParseWord
        // must fall back to LexicalGuess.
        Table3.AddNaturalClass(new NaturalClass(FeatureStruct.New().Value) { Name = "Seg" });
        var patternEntry = new LexEntry
        {
            Id = "guessPattern",
            Gloss = "guessPattern",
            SyntacticFeatureStruct = bareV,
        };
        patternEntry.Allomorphs.Add(new RootAllomorph(new Segments(Table3, "[Seg]+", true)));
        Morphophonemic.Entries.Add(patternEntry);

        var results = new Dictionary<AnalysisSyntacticFeatureMergeMode, List<Word>>();
        foreach (AnalysisSyntacticFeatureMergeMode mode in new[]
        {
            AnalysisSyntacticFeatureMergeMode.Add,
            AnalysisSyntacticFeatureMergeMode.PriorityUnion,
            AnalysisSyntacticFeatureMergeMode.Exact,
        })
        {
            Morphophonemic.AffixTemplates.Clear();
            var inner = MakeSuffixRule("guInner", bareV, tensePres, "i");
            var outer = MakeSuffixRule("guOuter", bareV, tensePast, "u");
            var outermost = MakeSuffixRule("guOutermost", tensePast, Empty, "a");
            var template = new AffixTemplate { Name = "guTemplate" };
            template.Slots.Add(new AffixTemplateSlot(inner));
            template.Slots.Add(new AffixTemplateSlot(outer));
            template.Slots.Add(new AffixTemplateSlot(outermost));
            Morphophonemic.AffixTemplates.Add(template);

            AnalysisSyntacticFeatureMerge.Mode = mode;
            AnalysisSyntacticFeatureMerge.ResetCounters();
            var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
            results[mode] = morpher.ParseWord("zudiua", out _, guessRoot: true).ToList();
        }

        Console.WriteLine($"Add guess results: {results[AnalysisSyntacticFeatureMergeMode.Add].Count}");
        Console.WriteLine($"PriorityUnion guess results: {results[AnalysisSyntacticFeatureMergeMode.PriorityUnion].Count}");
        Console.WriteLine($"Exact guess results: {results[AnalysisSyntacticFeatureMergeMode.Exact].Count}");

        Assert.That(
            results[AnalysisSyntacticFeatureMergeMode.Add],
            Is.Empty,
            "Add: same leftover-tense bug as Task 2 prevents the correct decomposition from ever being generated, so there is nothing to guess a root for."
        );
        Assert.That(
            results[AnalysisSyntacticFeatureMergeMode.PriorityUnion],
            Is.Empty,
            "PriorityUnion: same bug as Add."
        );
        Assert.That(
            results[AnalysisSyntacticFeatureMergeMode.Exact],
            Is.Not.Empty,
            "Exact: correctly reconstructs the decomposition, so LexicalGuess gets a chance to guess a root and re-synthesis succeeds."
        );
        // The guessed root's own POS is fixed to bare V (from patternEntry) in all modes: it never sees the
        // analysis-accumulated {N,V}/N-style ambiguity from Task 1, confirming the finding above.
        AssertSyntacticFeatureStructsEqual(
            results[AnalysisSyntacticFeatureMergeMode.Exact],
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Feature(Head).EqualTo(head => head.Feature("tense").EqualTo("past")).Value
        );
    }

    // =======================================================================================================
    // Task 5: compounding.
    // =======================================================================================================

    // Baseline: a derivational rule changes the head's category, then a CompoundingRule gates on that via
    // HeadRequiredSyntacticFeatureStruct. No flip-flop / leftover-Out setup here, so no mode-specific
    // narrowing pressure is expected -- this is a parity confirmation, not a divergence-seeking case.
    [Test]
    public void Compounding_AfterDerivationalRule_ParityAcrossModes()
    {
        FeatureStruct posN = Pos("N");
        FeatureStruct posV = Pos("V");
        AddEntry("cpHead", posN, Morphophonemic, "zag");
        AddEntry("cpNonHead", posN, Morphophonemic, "bim");

        var results = new Dictionary<AnalysisSyntacticFeatureMergeMode, List<Word>>();
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        foreach (AnalysisSyntacticFeatureMergeMode mode in new[]
        {
            AnalysisSyntacticFeatureMergeMode.Add,
            AnalysisSyntacticFeatureMergeMode.PriorityUnion,
            AnalysisSyntacticFeatureMergeMode.Exact,
        })
        {
            Morphophonemic.MorphologicalRules.Clear();
            var der = MakeSuffixRule("cpDer", posN, posV, "u");
            Morphophonemic.MorphologicalRules.Add(der);

            var comp = new CompoundingRule
            {
                Name = "cpComp",
                HeadRequiredSyntacticFeatureStruct = posV,
                OutSyntacticFeatureStruct = posN,
            };
            comp.Subrules.Add(
                new CompoundingSubrule
                {
                    HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
                    NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
                    Rhs = { new CopyFromInput("head"), new InsertSegments(Table3, "+"), new CopyFromInput("nonHead") },
                }
            );
            Morphophonemic.MorphologicalRules.Add(comp);

            AnalysisSyntacticFeatureMerge.Mode = mode;
            AnalysisSyntacticFeatureMerge.ResetCounters();
            var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1) { MaxStemCount = 3 };
            results[mode] = morpher.ParseWord("zagubim").ToList();
        }

        static List<string> AsGlosses(List<Word> words) =>
            words.Select(w => string.Join(" ", w.AllomorphsInMorphOrder.Select(m => m.Morpheme.Gloss))).OrderBy(s => s).ToList();

        List<string> addGlosses = AsGlosses(results[AnalysisSyntacticFeatureMergeMode.Add]);
        List<string> puGlosses = AsGlosses(results[AnalysisSyntacticFeatureMergeMode.PriorityUnion]);
        List<string> exactGlosses = AsGlosses(results[AnalysisSyntacticFeatureMergeMode.Exact]);

        Console.WriteLine($"Add: {string.Join(" | ", addGlosses)}");
        Console.WriteLine($"PriorityUnion: {string.Join(" | ", puGlosses)}");
        Console.WriteLine($"Exact: {string.Join(" | ", exactGlosses)}");

        Assert.That(addGlosses, Is.Not.Empty, "sanity: the derivation+compound chain should parse at all.");
        Assert.That(puGlosses, Is.EqualTo(addGlosses), "PriorityUnion should match Add for this simple (non-flip-flop) compounding case.");
        Assert.That(exactGlosses, Is.EqualTo(addGlosses), "Exact should match Add for this simple (non-flip-flop) compounding case.");
    }

    // Divergence-seeking variant of the above: an outerCheck rule on a LATER stratum (Allophonic, processed
    // before Morphophonemic during analysis) re-requires the compound's Out=N and re-emits V, mirroring
    // Task 1's flip-flop but with a CompoundingRule as the middle link. Using separate strata (rather than
    // one Unordered stratum) forces a deterministic analysis order and avoids a combinatorial search.
    [Test]
    public void Compounding_FlipFlopWithOuterCheck_MirrorsTask1Finding()
    {
        FeatureStruct posN = Pos("N");
        FeatureStruct posV = Pos("V");
        AddEntry("cp2Head", posN, Morphophonemic, "zag");
        AddEntry("cp2NonHead", posN, Morphophonemic, "bim");

        var results = new Dictionary<AnalysisSyntacticFeatureMergeMode, List<Word>>();
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        foreach (AnalysisSyntacticFeatureMergeMode mode in new[]
        {
            AnalysisSyntacticFeatureMergeMode.Add,
            AnalysisSyntacticFeatureMergeMode.PriorityUnion,
            AnalysisSyntacticFeatureMergeMode.Exact,
        })
        {
            Morphophonemic.MorphologicalRules.Clear();
            Allophonic.MorphologicalRules.Clear();

            var der = MakeSuffixRule("cp2Der", posN, posV, "u");
            Morphophonemic.MorphologicalRules.Add(der);

            var comp = new CompoundingRule
            {
                Name = "cp2Comp",
                HeadRequiredSyntacticFeatureStruct = posV,
                OutSyntacticFeatureStruct = posN,
            };
            comp.Subrules.Add(
                new CompoundingSubrule
                {
                    HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
                    NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
                    Rhs = { new CopyFromInput("head"), new InsertSegments(Table3, "+"), new CopyFromInput("nonHead") },
                }
            );
            Morphophonemic.MorphologicalRules.Add(comp);

            // outerCheck: Required=N (must match comp's Out=N), Out=V. Lives on Allophonic (uses Table1,
            // which has 'i'), analyzed before Morphophonemic, so it always un-applies first.
            var outerCheck = new AffixProcessRule
            {
                Name = "cp2OuterCheck",
                Gloss = "cp2OuterCheck",
                RequiredSyntacticFeatureStruct = posN,
                OutSyntacticFeatureStruct = posV,
            };
            outerCheck.Allomorphs.Add(
                new AffixProcessAllomorph
                {
                    Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                    Rhs = { new CopyFromInput("1"), new InsertSegments(Table1, "i") },
                }
            );
            Allophonic.MorphologicalRules.Add(outerCheck);

            AnalysisSyntacticFeatureMerge.Mode = mode;
            AnalysisSyntacticFeatureMerge.ResetCounters();
            var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1) { MaxStemCount = 3 };
            results[mode] = morpher.ParseWord("zagubimi").ToList();
        }

        static bool FoundChain(List<Word> words) =>
            words.Any(w => w.AllomorphsInMorphOrder.Select(m => m.Morpheme.Gloss).OrderBy(g => g)
                .SequenceEqual(new[] { "cp2Der", "cp2Head", "cp2NonHead", "cp2OuterCheck" }.OrderBy(g => g)));

        Console.WriteLine($"Add found chain: {FoundChain(results[AnalysisSyntacticFeatureMergeMode.Add])}");
        Console.WriteLine($"PriorityUnion found chain: {FoundChain(results[AnalysisSyntacticFeatureMergeMode.PriorityUnion])}");
        Console.WriteLine($"Exact found chain: {FoundChain(results[AnalysisSyntacticFeatureMergeMode.Exact])}");

        // Recorded, not asserted a priori beyond Add (which must find its own construction): the point of
        // this test is to characterise, not to prescribe, what actually happens for the other two modes
        // when a CompoundingRule sits in the middle of a flip-flop chain.
        Assert.That(FoundChain(results[AnalysisSyntacticFeatureMergeMode.Add]), Is.True, "sanity: Add should find the chain it was designed to expose.");
    }
}
