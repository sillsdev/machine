using NUnit.Framework;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

// Unit battery for the analysis-cascade memo's primitives (memoization.md Stage 2): AnalysisStateKey's
// order-invariance over the rule-unapplication multiset, its sensitivity to every other field the key
// captures, and Word.ReplayOnto's prefix/suffix graft. Nothing in production code consumes these yet
// (that is Stage 3) -- these tests pin the primitives' own contract in isolation, independent of any
// particular cascade wiring.
[TestFixture]
public class AnalysisStateKeyTests : HermitCrabTestBase
{
    [Test]
    public void Equals_IsInvariantOverUnapplicationOrder_ForEqualMultisets()
    {
        var ruleA = new AffixProcessRule { Name = "ruleA" };
        var ruleB = new AffixProcessRule { Name = "ruleB" };

        // Same multiset {ruleA: 2, ruleB: 1}, built up in two genuinely different orders --
        // wordY touches ruleB first, unlike wordX, so this also exercises different backing
        // dictionary insertion order, not just a different position for a repeated rule.
        Word wordX = NewTestWord();
        wordX.MorphologicalRuleUnapplied(ruleA);
        wordX.MorphologicalRuleUnapplied(ruleB);
        wordX.MorphologicalRuleUnapplied(ruleA);
        wordX.Freeze();

        Word wordY = NewTestWord();
        wordY.MorphologicalRuleUnapplied(ruleB);
        wordY.MorphologicalRuleUnapplied(ruleA);
        wordY.MorphologicalRuleUnapplied(ruleA);
        wordY.Freeze();

        var keyX = new AnalysisStateKey(wordX);
        var keyY = new AnalysisStateKey(wordY);

        Assert.That(keyX.GetHashCode(), Is.EqualTo(keyY.GetHashCode()));
        Assert.That(keyX.Equals(keyY), Is.True);
    }

    [Test]
    public void Equals_False_WhenUnapplicationMultisetsDiffer()
    {
        var ruleA = new AffixProcessRule { Name = "ruleA" };

        Word wordX = NewTestWord();
        wordX.MorphologicalRuleUnapplied(ruleA);
        wordX.Freeze();

        Word wordY = NewTestWord();
        wordY.MorphologicalRuleUnapplied(ruleA);
        wordY.MorphologicalRuleUnapplied(ruleA);
        wordY.Freeze();

        Assert.That(new AnalysisStateKey(wordX).Equals(new AnalysisStateKey(wordY)), Is.False);
    }

    [Test]
    public void Equals_False_WhenNonHeadCountDiffers()
    {
        Word wordX = NewTestWord();
        wordX.Freeze();

        Word wordY = NewTestWord();
        Word nonHead = NewTestWord();
        nonHead.Freeze();
        wordY.NonHeadUnapplied(nonHead);
        wordY.Freeze();

        Assert.That(new AnalysisStateKey(wordX).Equals(new AnalysisStateKey(wordY)), Is.False);
    }

    [Test]
    public void Equals_False_WhenSyntacticFeatureStructDiffers()
    {
        Word wordX = NewTestWord();
        wordX.SyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value;
        wordX.Freeze();

        Word wordY = NewTestWord();
        wordY.SyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value;
        wordY.Freeze();

        Assert.That(new AnalysisStateKey(wordX).Equals(new AnalysisStateKey(wordY)), Is.False);
    }

    [Test]
    public void ReplayOnto_GraftsQueryPrefixOntoStoredSuffix_ForMruleTrail()
    {
        var ruleA = new AffixProcessRule { Name = "ruleA" };
        var ruleB = new AffixProcessRule { Name = "ruleB" };
        var ruleC = new AffixProcessRule { Name = "ruleC" };

        // The memoized node's own trail was [ruleA, ruleB] at the moment its subtree was recorded, with
        // ruleA as the length-1 prefix (whatever led to this state) and ruleB as the subtree-local suffix
        // that must survive the graft.
        Word memoized = NewTestWord();
        memoized.MorphologicalRuleUnapplied(ruleA);
        memoized.MorphologicalRuleUnapplied(ruleB);
        memoized.Freeze();

        // A different arrival at the same AnalysisStateKey, via a different prefix: [ruleC].
        Word query = NewTestWord();
        query.MorphologicalRuleUnapplied(ruleC);
        query.Freeze();

        Word replayed = memoized.ReplayOnto(query, mruleTrailPrefixLength: 1, nonHeadPrefixLength: 0);

        Assert.That(replayed.MorphologicalRules, Is.EqualTo(new IMorphologicalRule[] { ruleC, ruleB }));
    }

    [Test]
    public void ReplayOnto_GraftsQueryPrefixOntoStoredSuffix_ForNonHeads()
    {
        // The memoized node had already unapplied one non-head (its own prefix, at the memo point) before
        // its subtree unapplied a second one -- that second one is the subtree-local suffix to keep. The
        // two non-heads are built from distinct lexical entries (32 vs 33) so a wrong-prefix graft (e.g.
        // one that kept storedNonHead instead of subtreeNonHead, or reversed the GetRange window) is
        // actually distinguishable via RootAllomorph identity, not just NonHeadCount.
        Word storedNonHead = NewTestWord("32");
        storedNonHead.Freeze();
        Word subtreeNonHead = NewTestWord("33");
        subtreeNonHead.Freeze();
        Word memoized = NewTestWord("32");
        memoized.NonHeadUnapplied(storedNonHead);
        memoized.NonHeadUnapplied(subtreeNonHead);
        memoized.Freeze();

        // Query reached the same key with a different (empty) non-head prefix.
        Word query = NewTestWord("32");
        query.Freeze();

        Word replayed = memoized.ReplayOnto(query, mruleTrailPrefixLength: 0, nonHeadPrefixLength: 1);

        // Query's (empty) prefix + the memoized subtree's suffix (subtreeNonHead) = 1 non-head.
        Assert.That(replayed.NonHeadCount, Is.EqualTo(1));
        Assert.That(replayed.CurrentNonHead.RootAllomorph, Is.SameAs(subtreeNonHead.RootAllomorph));
        Assert.That(replayed.CurrentNonHead.RootAllomorph, Is.Not.SameAs(storedNonHead.RootAllomorph));
    }

    private Word NewTestWord(string entryId = "32")
    {
        var word = new Word(Entries[entryId].PrimaryAllomorph, FeatureStruct.New().Value) { Stratum = Morphophonemic };
        return word;
    }
}
