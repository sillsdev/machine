using NUnit.Framework;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

// The memo's primitives in isolation, independent of any cascade wiring: AnalysisStateKey's
// order-invariance and field sensitivity, its frozen-word requirement, and ReplayOnto's graft.
[TestFixture]
public class AnalysisStateKeyTests : HermitCrabTestBase
{
    [Test]
    public void Equals_IsInvariantOverUnapplicationOrder_ForEqualMultisets()
    {
        var ruleA = new AffixProcessRule { Name = "ruleA" };
        var ruleB = new AffixProcessRule { Name = "ruleB" };

        // Same multiset {ruleA: 2, ruleB: 1} reached in two different orders. wordY touches ruleB first,
        // so the backing dictionaries also differ in insertion order, not just in a repeated rule's
        // position.
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
    public void Constructor_Throws_WhenWordIsNotFrozen()
    {
        Word unfrozen = NewTestWord();

        Assert.That(() => new AnalysisStateKey(unfrozen), Throws.ArgumentException);
    }

    [Test]
    public void ReplayOnto_SharesHoistedQueryPrefix_AcrossOneHitsReplays()
    {
        Word queryNonHead = NewTestWord("32");
        queryNonHead.Freeze();
        Word query = NewTestWord("32");
        query.NonHeadUnapplied(queryNonHead);
        query.Freeze();

        Word storedNonHead = NewTestWord("32");
        storedNonHead.Freeze();
        Word subtreeNonHead = NewTestWord("33");
        subtreeNonHead.Freeze();
        Word memoized = NewTestWord("32");
        memoized.NonHeadUnapplied(storedNonHead);
        memoized.NonHeadUnapplied(subtreeNonHead);
        memoized.Freeze();

        List<Word> hoisted = query.CloneNonHeadsForReplay();
        Word first = memoized.ReplayOnto(query, 0, 1, hoisted);
        Word second = memoized.ReplayOnto(query, 0, 1, hoisted);

        // Query's 1 non-head prefix plus the stored subtree's 1 non-head suffix.
        Assert.That(first.NonHeadCount, Is.EqualTo(2));
        Assert.That(first.CurrentNonHead.RootAllomorph, Is.SameAs(subtreeNonHead.RootAllomorph));
        // Both replays share the one hoisted clone, and it is a clone rather than the query's own instance.
        Assert.That(first.NonHeads[0], Is.SameAs(second.NonHeads[0]));
        Assert.That(first.NonHeads[0], Is.SameAs(hoisted[0]));
        Assert.That(first.NonHeads[0], Is.Not.SameAs(queryNonHead));
    }

    [Test]
    public void ReplayOnto_GraftsQueryPrefixOntoStoredSuffix_ForMruleTrail()
    {
        var ruleA = new AffixProcessRule { Name = "ruleA" };
        var ruleB = new AffixProcessRule { Name = "ruleB" };
        var ruleC = new AffixProcessRule { Name = "ruleC" };

        // Trail [ruleA, ruleB] at the moment of the write: ruleA is the length-1 prefix, ruleB the
        // subtree-local suffix that must survive the graft.
        Word memoized = NewTestWord();
        memoized.MorphologicalRuleUnapplied(ruleA);
        memoized.MorphologicalRuleUnapplied(ruleB);
        memoized.Freeze();

        // The same key reached with a different prefix, [ruleC].
        Word query = NewTestWord();
        query.MorphologicalRuleUnapplied(ruleC);
        query.Freeze();

        Word replayed = memoized.ReplayOnto(query, mruleTrailPrefixLength: 1, nonHeadPrefixLength: 0);

        Assert.That(replayed.MorphologicalRules, Is.EqualTo(new IMorphologicalRule[] { ruleC, ruleB }));
    }

    [Test]
    public void ReplayOnto_GraftsQueryPrefixOntoStoredSuffix_ForNonHeads()
    {
        // Distinct lexical entries (32 vs 33) so that a graft keeping the wrong non-head, or reversing the
        // GetRange window, is distinguishable by RootAllomorph identity rather than only by count.
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
