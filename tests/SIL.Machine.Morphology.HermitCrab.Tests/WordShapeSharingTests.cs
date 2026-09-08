using NUnit.Framework;
using SIL.Machine.Annotations;

namespace SIL.Machine.Morphology.HermitCrab;

// Covers the public deep-clone contract and the internal copy-on-write Shape sharing used by engine clones.
[TestFixture]
public class WordShapeSharingTests : HermitCrabTestBase
{
    [Test]
    public void Clone_OfFrozenWord_DeepCopiesShapeToMutableInstance()
    {
        Word word = BuildWord("bad");
        word.Freeze();

        Word clone = word.Clone();

        Assert.That(ReferenceEquals(clone.Shape, word.Shape), Is.False);
        Assert.That(clone.Shape.IsFrozen, Is.False);
        Assert.DoesNotThrow(() => clone.Shape.Add(Character(Table1, "b")));
    }

    [Test]
    public void Clone_OfUnfrozenWord_DoesNotShareShapeReference()
    {
        Word word = BuildWord("bad");
        Assert.That(word.IsFrozen, Is.False);

        Word clone = word.Clone();

        Assert.That(ReferenceEquals(clone.Shape, word.Shape), Is.False);
    }

    [Test]
    public void EnsureOwnShape_OnSharedClone_YieldsDistinctUnfrozenShapeWithSameContent()
    {
        Word word = BuildWord("bad");
        word.Freeze();
        Word clone = word.CloneForEngine();
        Assert.That(ReferenceEquals(clone.Shape, word.Shape), Is.True, "precondition: shape must start shared");

        clone.EnsureOwnShape();

        Assert.That(ReferenceEquals(clone.Shape, word.Shape), Is.False);
        Assert.That(clone.Shape.IsFrozen, Is.False);
        Assert.That(clone.Shape.Count, Is.EqualTo(word.Shape.Count));
    }

    [Test]
    public void EnsureOwnShape_CalledTwice_OnlyClonesOnce()
    {
        Word word = BuildWord("bad");
        word.Freeze();
        Word clone = word.CloneForEngine();

        clone.EnsureOwnShape();
        Shape ownShape = clone.Shape;
        clone.EnsureOwnShape();

        Assert.That(ReferenceEquals(clone.Shape, ownShape), Is.True);
    }

    [Test]
    public void MutatingSharedClone_WithoutEnsureOwnShape_Throws()
    {
        Word word = BuildWord("bad");
        word.Freeze();
        Word clone = word.CloneForEngine();

        Assert.Throws<InvalidOperationException>(() => clone.Shape.Add(Character(Table1, "b")));
    }

    [Test]
    public void ResetShape_YieldsEmptyUnfrozenShape_AndRebuiltWordMatchesWordBuiltDirectly()
    {
        Word word = BuildWord("bad");
        word.Freeze();
        Word clone = word.CloneForEngine();

        clone.ResetShape();

        Assert.That(clone.Shape.Count, Is.EqualTo(0));
        Assert.That(clone.Shape.IsFrozen, Is.False);

        // Rebuild the cleared shape from the frozen original, exactly like GenerateShape/CopyFromInput do.
        word.Shape.CopyTo(clone.Shape);
        clone.Freeze();

        Word directlyBuilt = BuildWord("bad");
        directlyBuilt.Freeze();

        Assert.That(clone.ValueEquals(directlyBuilt), Is.True);
        Assert.That(clone.GetFrozenHashCode(), Is.EqualTo(directlyBuilt.GetFrozenHashCode()));
    }

    private Word BuildWord(string form)
    {
        return new Word(Allophonic, Table1.Segment(form));
    }
}
