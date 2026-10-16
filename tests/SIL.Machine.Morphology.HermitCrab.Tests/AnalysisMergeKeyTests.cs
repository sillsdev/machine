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

    private static bool KeysEqual(Word x, Word y) =>
        new AnalysisStratumRule.AnalysisMergeKey(x).Equals(new AnalysisStratumRule.AnalysisMergeKey(y));

    private Word NewTestWord()
    {
        return new Word(Entries["32"].PrimaryAllomorph, FeatureStruct.New().Value) { Stratum = Morphophonemic };
    }
}
