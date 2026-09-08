using NUnit.Framework;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab;

// Covers copy-on-write SyntacticFeatureStruct sharing on Word's internal engine clone path. Public Clone
// retains its historical deep-copy/mutable behavior; internal clones of engine-stamped Words may share a
// frozen struct and only pay for a real clone at mutation sites. See Word.cs for the design.
//
// Unlike Shape (which Word.FreezeImpl always freezes as part of freezing the Word), SyntacticFeatureStruct
// is deliberately never frozen by Word.FreezeImpl -- analysis rules mutate it in place on Words that are
// already frozen (see AnalysisAffixTemplateRule.Apply, AnalysisAffixProcessRule.Apply,
// AnalysisCompoundingRule.Apply). So the sharing decision is keyed on
// word.SyntacticFeatureStruct.IsFrozen, not word.IsFrozen; in production this is set by
// AnalysisStateKey.PinAndKey, which freezes it explicitly before using it as a memo key. These tests
// freeze the FeatureStruct directly to isolate that condition from Word's own freeze state.
[TestFixture]
[NonParallelizable]
public class WordSyntacticFsSharingTests : HermitCrabTestBase
{
    private bool _savedDefault;

    [SetUp]
    public void SetUp()
    {
        // Save and restore rather than forcing true so this composes with the suite-level environment
        // override in ShareSyntacticFsSetUpFixture.
        _savedDefault = Morpher.DefaultShareSyntacticFeatureStructs;
    }

    [TearDown]
    public void TearDown()
    {
        Morpher.DefaultShareSyntacticFeatureStructs = _savedDefault;
    }

    [Test]
    public void Clone_OfWordWithFrozenSyntacticFS_DeepCopiesToMutableStruct()
    {
        Word word = BuildWord("bad");
        word.SyntacticFeatureStruct.Freeze();

        Word clone = word.Clone();

        Assert.That(ReferenceEquals(clone.SyntacticFeatureStruct, word.SyntacticFeatureStruct), Is.False);
        Assert.That(clone.SyntacticFeatureStruct.IsFrozen, Is.False);
        Assert.DoesNotThrow(clone.SyntacticFeatureStruct.Clear);
    }

    [Test]
    public void Clone_OfWordWithUnfrozenSyntacticFS_DoesNotShareReference()
    {
        Word word = BuildWord("bad");
        Assert.That(word.SyntacticFeatureStruct.IsFrozen, Is.False);

        Word clone = word.Clone();

        Assert.That(ReferenceEquals(clone.SyntacticFeatureStruct, word.SyntacticFeatureStruct), Is.False);
    }

    [Test]
    public void CloneForEngine_WithSharingDisabled_DoesNotShareEvenWhenFrozen()
    {
        Word word = BuildWord("bad");
        word.ShareSyntacticFeatureStructs = false;
        word.SyntacticFeatureStruct.Freeze();

        Word clone = word.CloneForEngine();

        Assert.That(ReferenceEquals(clone.SyntacticFeatureStruct, word.SyntacticFeatureStruct), Is.False);
        Assert.That(clone.SyntacticFeatureStruct.ValueEquals(word.SyntacticFeatureStruct), Is.True);
    }

    [Test]
    public void CloneForEngine_OfEnabledWordWithFrozenSyntacticFS_SharesReference()
    {
        Word word = BuildWord("bad");
        word.ShareSyntacticFeatureStructs = true;
        word.SyntacticFeatureStruct.Freeze();

        Word clone = word.CloneForEngine();

        Assert.That(ReferenceEquals(clone.SyntacticFeatureStruct, word.SyntacticFeatureStruct), Is.True);
        Assert.That(clone.ShareSyntacticFeatureStructs, Is.True);
    }

    [Test]
    public void EnsureOwnSyntacticFeatureStruct_OnSharedClone_YieldsDistinctUnfrozenStructWithSameContent()
    {
        Word word = BuildWord("bad");
        word.ShareSyntacticFeatureStructs = true;
        word.SyntacticFeatureStruct.Freeze();
        Word clone = word.CloneForEngine();
        Assert.That(
            ReferenceEquals(clone.SyntacticFeatureStruct, word.SyntacticFeatureStruct),
            Is.True,
            "precondition: syntactic FS must start shared"
        );

        clone.EnsureOwnSyntacticFeatureStruct();

        Assert.That(ReferenceEquals(clone.SyntacticFeatureStruct, word.SyntacticFeatureStruct), Is.False);
        Assert.That(clone.SyntacticFeatureStruct.IsFrozen, Is.False);
        Assert.That(clone.SyntacticFeatureStruct.ValueEquals(word.SyntacticFeatureStruct), Is.True);
    }

    [Test]
    public void EnsureOwnSyntacticFeatureStruct_CalledTwice_OnlyClonesOnce()
    {
        Word word = BuildWord("bad");
        word.ShareSyntacticFeatureStructs = true;
        word.SyntacticFeatureStruct.Freeze();
        Word clone = word.CloneForEngine();

        clone.EnsureOwnSyntacticFeatureStruct();
        FeatureStruct ownFs = clone.SyntacticFeatureStruct;
        clone.EnsureOwnSyntacticFeatureStruct();

        Assert.That(ReferenceEquals(clone.SyntacticFeatureStruct, ownFs), Is.True);
    }

    [Test]
    public void EnsureOwnSyntacticFeatureStruct_OnUnsharedClone_IsANoOp()
    {
        Word word = BuildWord("bad");
        Word clone = word.Clone();
        FeatureStruct ownFs = clone.SyntacticFeatureStruct;
        Assert.That(ReferenceEquals(ownFs, word.SyntacticFeatureStruct), Is.False, "precondition: not shared");

        clone.EnsureOwnSyntacticFeatureStruct();

        Assert.That(ReferenceEquals(clone.SyntacticFeatureStruct, ownFs), Is.True);
    }

    [Test]
    public void MutatingSharedClone_WithoutEnsureOwnSyntacticFeatureStruct_Throws()
    {
        Word word = BuildWord("bad");
        word.ShareSyntacticFeatureStructs = true;
        word.SyntacticFeatureStruct.Freeze();
        Word clone = word.CloneForEngine();

        Assert.Throws<System.InvalidOperationException>(clone.SyntacticFeatureStruct.Clear);
    }

    [Test]
    public void MutatingSharedClone_AfterEnsureOwnSyntacticFeatureStruct_DoesNotThrowAndDoesNotAffectSource()
    {
        Word word = BuildWord("bad");
        word.ShareSyntacticFeatureStructs = true;
        word.SyntacticFeatureStruct.Add(FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("foo+").Value);
        word.SyntacticFeatureStruct.Freeze();
        Word clone = word.CloneForEngine();

        clone.EnsureOwnSyntacticFeatureStruct();
        Assert.DoesNotThrow(clone.SyntacticFeatureStruct.Clear);

        Assert.That(clone.SyntacticFeatureStruct.IsEmpty, Is.True);
        Assert.That(word.SyntacticFeatureStruct.IsEmpty, Is.False, "source must be unaffected by clone's mutation");
    }

    [Test]
    public void AssigningNewSyntacticFeatureStruct_ClearsSharingFlagAndAllowsDirectMutation()
    {
        Word word = BuildWord("bad");
        word.ShareSyntacticFeatureStructs = true;
        word.SyntacticFeatureStruct.Freeze();
        Word clone = word.CloneForEngine();
        Assert.That(
            ReferenceEquals(clone.SyntacticFeatureStruct, word.SyntacticFeatureStruct),
            Is.True,
            "precondition: syntactic FS must start shared"
        );

        var replacement = new FeatureStruct();
        clone.SyntacticFeatureStruct = replacement;

        // No throw: the setter must have cleared the sharing flag, so this is treated as an owned struct.
        Assert.DoesNotThrow(() => clone.SyntacticFeatureStruct.Clear());
        // EnsureOwnSyntacticFeatureStruct must be a no-op now (nothing shared left to clone).
        clone.EnsureOwnSyntacticFeatureStruct();
        Assert.That(ReferenceEquals(clone.SyntacticFeatureStruct, replacement), Is.True);
    }

    [Test]
    public void CloneForEngine_ClonesNestedNonHeadsThroughInternalPath()
    {
        Word nonHead = BuildWord("dad");
        nonHead.ShareSyntacticFeatureStructs = true;
        nonHead.SyntacticFeatureStruct.Freeze();
        Word head = BuildWord("bad");
        head.ShareSyntacticFeatureStructs = true;
        head.NonHeadUnapplied(nonHead);

        Word clone = head.CloneForEngine();

        Assert.That(clone.NonHeads[0], Is.Not.SameAs(nonHead));
        Assert.That(ReferenceEquals(clone.NonHeads[0].SyntacticFeatureStruct, nonHead.SyntacticFeatureStruct), Is.True);
    }

    [Test]
    public void CloneNonHeadsForReplay_UsesInternalClonePath()
    {
        Word nonHead = BuildWord("dad");
        nonHead.ShareSyntacticFeatureStructs = true;
        nonHead.SyntacticFeatureStruct.Freeze();
        Word word = BuildWord("bad");
        word.NonHeadUnapplied(nonHead);

        Word clone = word.CloneNonHeadsForReplay().Single();

        Assert.That(clone, Is.Not.SameAs(nonHead));
        Assert.That(ReferenceEquals(clone.SyntacticFeatureStruct, nonHead.SyntacticFeatureStruct), Is.True);
    }

    [Test]
    public void MorphersCaptureDefaultAndRemainIndependent()
    {
        Morpher.DefaultShareSyntacticFeatureStructs = true;
        var enabled = new Morpher(TraceManager, Language);
        var independentlyEnabled = new Morpher(TraceManager, Language);

        enabled.ShareSyntacticFeatureStructs = false;
        Morpher.DefaultShareSyntacticFeatureStructs = false;
        var disabledByDefault = new Morpher(TraceManager, Language);

        Assert.That(enabled.ShareSyntacticFeatureStructs, Is.False);
        Assert.That(independentlyEnabled.ShareSyntacticFeatureStructs, Is.True);
        Assert.That(disabledByDefault.ShareSyntacticFeatureStructs, Is.False);
    }

    private Word BuildWord(string form)
    {
        return new Word(Allophonic, Table1.Segment(form));
    }
}
