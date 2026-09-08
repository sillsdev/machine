using NUnit.Framework;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Annotations;

// Covers Shape.CopyTo's node-to-node mapping, which used to be built with a second GetNodes(...).Zip(...)
// .ToDictionary(...) pass over both shapes; it is now built inline, in the same loop that clones the nodes.
// These tests exercise both the leaf annotation case (a per-node annotation) and the spanning/subsuming case (an
// annotation with children), since CopyAnnotations handles those two shapes of the mapping differently.
[TestFixture]
public class ShapeCopyToTests
{
    private static Shape NewShape()
    {
        return new Shape(begin => new ShapeNode(new FeatureStruct()));
    }

    private static FeatureStruct MakeFeatureStruct(string value)
    {
        var fs = new FeatureStruct();
        fs.AddValue(new StringFeature("s"), value);
        return fs;
    }

    [Test]
    public void CopyTo_CopiesLeafAnnotationsAndValueEqualsSource()
    {
        Shape src = NewShape();
        src.Add(MakeFeatureStruct("a"));
        src.Add(MakeFeatureStruct("b"));
        src.Add(MakeFeatureStruct("c"));

        Shape dest = NewShape();
        Range<ShapeNode> destRange = src.CopyTo(dest);

        Assert.That(dest.Count, Is.EqualTo(3));
        Assert.That(destRange.Start, Is.Not.Null);
        Assert.That(destRange.Start.List, Is.SameAs(dest));
        Assert.That(destRange.End.List, Is.SameAs(dest));

        // Every copied node is a distinct instance owned by dest, with equal (but not shared) FeatureStructs.
        ShapeNode[] srcNodes = src.ToArray();
        ShapeNode[] destNodes = dest.ToArray();
        for (int i = 0; i < srcNodes.Length; i++)
        {
            Assert.That(destNodes[i], Is.Not.SameAs(srcNodes[i]));
            Assert.That(destNodes[i].List, Is.SameAs(dest));
            Assert.That(
                destNodes[i].Annotation.FeatureStruct.ValueEquals(srcNodes[i].Annotation.FeatureStruct),
                Is.True
            );
        }
    }

    [Test]
    public void CopyTo_CopiesSpanningAnnotationWithCorrectlyMappedChildren()
    {
        Shape src = NewShape();
        ShapeNode n1 = src.Add(MakeFeatureStruct("a"));
        ShapeNode n2 = src.Add(MakeFeatureStruct("b"));
        src.Add(MakeFeatureStruct("c"));

        FeatureStruct spanFs = MakeFeatureStruct("span");
        // Subsumes (by default) the n1/n2 leaf annotations as children of the new spanning annotation.
        src.Annotations.Add(n1, n2, spanFs);

        Shape dest = NewShape();
        src.CopyTo(dest);

        Assert.That(dest.ValueEquals(src), Is.True);

        // The spanning annotation itself: a distinct, dest-owned clone spanning dest-owned nodes (proving the
        // node mapping was actually applied, not left pointing at the source shape's nodes).
        Annotation<ShapeNode> destSpan = dest.Annotations.Single(a => !a.IsLeaf);
        Assert.That(destSpan.FeatureStruct, Is.Not.SameAs(spanFs));
        Assert.That(destSpan.FeatureStruct.ValueEquals(spanFs), Is.True);
        Assert.That(destSpan.Range.Start.List, Is.SameAs(dest));
        Assert.That(destSpan.Range.End.List, Is.SameAs(dest));
        Assert.That(destSpan.Range.Start, Is.Not.SameAs(n1));
        Assert.That(destSpan.Range.End, Is.Not.SameAs(n2));

        Assert.That(destSpan.Children.Count, Is.EqualTo(2));
        Assert.That(destSpan.Children.First.Range.Start.List, Is.SameAs(dest));
        Assert.That(destSpan.Children.Last.Range.Start.List, Is.SameAs(dest));
        Assert.That(destSpan.Children.First.FeatureStruct.ValueEquals(n1.Annotation.FeatureStruct), Is.True);
        Assert.That(destSpan.Children.Last.FeatureStruct.ValueEquals(n2.Annotation.FeatureStruct), Is.True);
    }

    [Test]
    public void CopyTo_EmptyShape_ReturnsNullRangeAndDoesNotThrow()
    {
        Shape src = NewShape();
        Shape dest = NewShape();

        Range<ShapeNode> destRange = src.CopyTo(dest);

        Assert.That(destRange, Is.EqualTo(Range<ShapeNode>.Null));
        Assert.That(dest.Count, Is.EqualTo(0));
    }
}
