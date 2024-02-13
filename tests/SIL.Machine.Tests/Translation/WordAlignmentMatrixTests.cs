using NUnit.Framework;

namespace SIL.Machine.Translation;

[TestFixture]
public class WordAlignmentMatrixTests
{
    [Test]
    public void IntersectWith()
    {
        (WordAlignmentMatrix x, WordAlignmentMatrix y) = CreateMatrices();

        x.IntersectWith(y);

        var expected = new WordAlignmentMatrix(7, 9)
        {
            [0, 0] = true,
            [2, 1] = true,
            [3, 4] = true
        };
        Assert.That(x.ValueEquals(expected), Is.True);
    }

    [Test]
    public void UnionWith()
    {
        (WordAlignmentMatrix x, WordAlignmentMatrix y) = CreateMatrices();

        x.UnionWith(y);

        var expected = new WordAlignmentMatrix(7, 9)
        {
            [0, 0] = true,
            [1, 1] = true,
            [1, 5] = true,
            [2, 1] = true,
            [3, 2] = true,
            [3, 3] = true,
            [3, 4] = true,
            [4, 5] = true,
            [4, 6] = true,
            [5, 3] = true,
            [6, 8] = true
        };
        Assert.That(x.ValueEquals(expected), Is.True);
    }

    [Test]
    public void SymmetrizeWith()
    {
        (WordAlignmentMatrix x, WordAlignmentMatrix y) = CreateMatrices();

        x.SymmetrizeWith(y);

        var expected = new WordAlignmentMatrix(7, 9)
        {
            [0, 0] = true,
            [1, 1] = true,
            [2, 1] = true,
            [3, 2] = true,
            [3, 3] = true,
            [3, 4] = true,
            [4, 5] = true,
            [4, 6] = true,
            [6, 8] = true
        };
        Assert.That(x.ValueEquals(expected), Is.True);
    }

    [Test]
    public void GrowSymmetrizeWith()
    {
        (WordAlignmentMatrix x, WordAlignmentMatrix y) = CreateMatrices();

        x.GrowSymmetrizeWith(y);

        var expected = new WordAlignmentMatrix(7, 9)
        {
            [0, 0] = true,
            [1, 1] = true,
            [2, 1] = true,
            [3, 2] = true,
            [3, 3] = true,
            [3, 4] = true
        };
        Assert.That(x.ValueEquals(expected), Is.True);
    }

    [Test]
    public void GrowDiagSymmetrizeWith()
    {
        (WordAlignmentMatrix x, WordAlignmentMatrix y) = CreateMatrices();

        x.GrowDiagSymmetrizeWith(y);

        var expected = new WordAlignmentMatrix(7, 9)
        {
            [0, 0] = true,
            [1, 1] = true,
            [2, 1] = true,
            [3, 2] = true,
            [3, 3] = true,
            [3, 4] = true,
            [4, 5] = true,
            [4, 6] = true
        };
        Assert.That(x.ValueEquals(expected), Is.True);
    }

    [Test]
    public void GrowDiagFinalSymmetrizeWith()
    {
        (WordAlignmentMatrix x, WordAlignmentMatrix y) = CreateMatrices();

        x.GrowDiagFinalSymmetrizeWith(y);

        var expected = new WordAlignmentMatrix(7, 9)
        {
            [0, 0] = true,
            [1, 1] = true,
            [2, 1] = true,
            [3, 2] = true,
            [3, 3] = true,
            [3, 4] = true,
            [4, 5] = true,
            [4, 6] = true,
            [5, 3] = true,
            [6, 8] = true
        };
        Assert.That(x.ValueEquals(expected), Is.True);
    }

    [Test]
    public void GrowDiagFinalAndSymmetrizeWith()
    {
        (WordAlignmentMatrix x, WordAlignmentMatrix y) = CreateMatrices();

        x.GrowDiagFinalAndSymmetrizeWith(y);

        var expected = new WordAlignmentMatrix(7, 9)
        {
            [0, 0] = true,
            [1, 1] = true,
            [2, 1] = true,
            [3, 2] = true,
            [3, 3] = true,
            [3, 4] = true,
            [4, 5] = true,
            [4, 6] = true,
            [6, 8] = true
        };
        Assert.That(x.ValueEquals(expected), Is.True);
    }

    [Test]
    public void Resize_Grow()
    {
        var matrix = new WordAlignmentMatrix(3, 3)
        {
            [0, 0] = true,
            [1, 1] = true,
            [2, 2] = true
        };

        matrix.Resize(4, 4);
        var expected = new WordAlignmentMatrix(4, 4)
        {
            [0, 0] = true,
            [1, 1] = true,
            [2, 2] = true
        };
        Assert.That(matrix.ValueEquals(expected), Is.True);
    }

    [Test]
    public void Resize_Shrink()
    {
        var matrix = new WordAlignmentMatrix(3, 3)
        {
            [0, 0] = true,
            [1, 1] = true,
            [2, 2] = true
        };

        matrix.Resize(2, 2);
        var expected = new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true };
        Assert.That(matrix.ValueEquals(expected), Is.True);
    }

    [Test]
    public void Resize_Grow_Shrink()
    {
        var matrix = new WordAlignmentMatrix(3, 3)
        {
            [0, 0] = true,
            [1, 1] = true,
            [2, 2] = true
        };

        matrix.Resize(2, 4);
        var expected = new WordAlignmentMatrix(2, 4) { [0, 0] = true, [1, 1] = true };
        Assert.That(matrix.ValueEquals(expected), Is.True);
    }

    private static (WordAlignmentMatrix, WordAlignmentMatrix) CreateMatrices()
    {
        var x = new WordAlignmentMatrix(7, 9)
        {
            [0, 0] = true,
            [1, 5] = true,
            [2, 1] = true,
            [3, 2] = true,
            [3, 3] = true,
            [3, 4] = true,
            [4, 5] = true,
            [5, 3] = true
        };

        var y = new WordAlignmentMatrix(7, 9)
        {
            [0, 0] = true,
            [1, 1] = true,
            [2, 1] = true,
            [3, 4] = true,
            [4, 6] = true,
            [6, 8] = true
        };

        return (x, y);
    }
}
