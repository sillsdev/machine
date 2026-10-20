using NUnit.Framework;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

// AnalysisMergeKey is internal to AnalysisStratumRule; these tests exercise the merge key
// AnalysisStratumRule.Apply actually uses, not a proxy for it.
[TestFixture]
public class AnalysisMergeKeyTests : HermitCrabTestBase
{
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

        Assert.That(KeysEqual(wordX, wordY), Is.False);
    }

    [Test]
    public void Equals_False_WhenUnapplicationCountsDiffer()
    {
        var ruleA = new AffixProcessRule { Name = "ruleA" };

        Word wordX = NewTestWord();
        wordX.MorphologicalRuleUnapplied(ruleA);
        wordX.Freeze();

        Word wordY = NewTestWord();
        wordY.MorphologicalRuleUnapplied(ruleA);
        wordY.MorphologicalRuleUnapplied(ruleA);
        wordY.Freeze();

        Assert.That(KeysEqual(wordX, wordY), Is.False);
    }

    [Test]
    public void Equals_IsInvariantOverUnapplicationOrder_ForEqualMultisets()
    {
        var ruleA = new AffixProcessRule { Name = "ruleA" };
        var ruleB = new AffixProcessRule { Name = "ruleB" };

        // Same multiset {ruleA: 2, ruleB: 1}; wordY touches ruleB first, so the backing dictionaries also
        // differ in insertion order, not just in a repeated rule's position.
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

        var keyX = new AnalysisStratumRule.AnalysisMergeKey(wordX);
        var keyY = new AnalysisStratumRule.AnalysisMergeKey(wordY);

        Assert.That(keyX.GetHashCode(), Is.EqualTo(keyY.GetHashCode()));
        Assert.That(keyX.Equals(keyY), Is.True);
    }

    [Test]
    public void Equals_False_WhenFinalTemplateStateDiffers()
    {
        Word wordX = NewTestWord();
        wordX.FinalTemplateState = FinalTemplateState.None;
        wordX.Freeze();

        Word wordY = NewTestWord();
        wordY.FinalTemplateState = FinalTemplateState.NonTemplate;
        wordY.Freeze();

        Assert.That(KeysEqual(wordX, wordY), Is.False);
    }

    private static bool KeysEqual(Word x, Word y) =>
        new AnalysisStratumRule.AnalysisMergeKey(x).Equals(new AnalysisStratumRule.AnalysisMergeKey(y));

    private Word NewTestWord()
    {
        return new Word(Entries["32"].PrimaryAllomorph, FeatureStruct.New().Value) { Stratum = Morphophonemic };
    }
}
