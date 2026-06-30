using NUnit.Framework;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Annotations;

[TestFixture]
public class AnnotationTests
{
    [Test]
    public void Add()
    {
        var annList = new AnnotationList<int>();
        // add without subsumption
        // add to empty list
        var a = new Annotation<int>(Range<int>.Create(49, 50), FeatureStruct.New().Value);
        annList.Add(a, false);
        Assert.That(annList.Count, Is.EqualTo(1));
        Assert.That(annList.First, Is.SameAs(a));
        // add to beginning of list
        a = new Annotation<int>(Range<int>.Create(0, 1), FeatureStruct.New().Value);
        annList.Add(a, false);
        Assert.That(annList.Count, Is.EqualTo(2));
        Assert.That(annList.First, Is.SameAs(a));
        // add to end of list
        a = new Annotation<int>(Range<int>.Create(99, 100), FeatureStruct.New().Value);
        annList.Add(a, false);
        Assert.That(annList.Count, Is.EqualTo(3));
        Assert.That(annList.Last, Is.SameAs(a));
        // add to middle of list
        a = new Annotation<int>(Range<int>.Create(24, 25), FeatureStruct.New().Value);
        annList.Add(a, false);
        Assert.That(annList.Count, Is.EqualTo(4));
        Assert.That(annList.ElementAt(1), Is.SameAs(a));
        // add containing annotation
        a = new Annotation<int>(Range<int>.Create(0, 100), FeatureStruct.New().Value);
        annList.Add(a, false);
        Assert.That(annList.Count, Is.EqualTo(5));
        Assert.That(annList.First(), Is.SameAs(a));
        // add contained annotation
        a = new Annotation<int>(Range<int>.Create(9, 10), FeatureStruct.New().Value);
        annList.Add(a, false);
        Assert.That(annList.Count, Is.EqualTo(6));
        Assert.That(annList.ElementAt(2), Is.SameAs(a));

        annList.Clear();

        // add with subsumption
        // add to empty list
        a = new Annotation<int>(Range<int>.Create(49, 50), FeatureStruct.New().Value);
        annList.Add(a);
        Assert.That(annList.Count, Is.EqualTo(1));
        Assert.That(annList.First, Is.SameAs(a));
        // add to beginning of list
        a = new Annotation<int>(Range<int>.Create(0, 1), FeatureStruct.New().Value);
        annList.Add(a);
        Assert.That(annList.Count, Is.EqualTo(2));
        Assert.That(annList.First, Is.SameAs(a));
        // add to end of list
        a = new Annotation<int>(Range<int>.Create(99, 100), FeatureStruct.New().Value);
        annList.Add(a);
        Assert.That(annList.Count, Is.EqualTo(3));
        Assert.That(annList.Last, Is.SameAs(a));
        // add to middle of list
        a = new Annotation<int>(Range<int>.Create(24, 25), FeatureStruct.New().Value);
        annList.Add(a);
        Assert.That(annList.Count, Is.EqualTo(4));
        Assert.That(annList.ElementAt(1), Is.SameAs(a));
        // add containing annotation
        a = new Annotation<int>(Range<int>.Create(0, 100), FeatureStruct.New().Value);
        annList.Add(a);
        Assert.That(annList.Count, Is.EqualTo(1));
        Assert.That(annList.First(), Is.SameAs(a));
        Assert.That(a.Children.Count, Is.EqualTo(4));
        // add contained annotation
        a = new Annotation<int>(Range<int>.Create(9, 10), FeatureStruct.New().Value);
        annList.Add(a);
        Assert.That(annList.Count, Is.EqualTo(1));
        Assert.That(annList.First.Children.Count, Is.EqualTo(5));
        Assert.That(annList.First.Children.ElementAt(1), Is.SameAs(a));

        annList.Clear();

        annList.Add(0, 1, FeatureStruct.New().Value);
        annList.Add(1, 2, FeatureStruct.New().Value);
        annList.Add(2, 3, FeatureStruct.New().Value);
        annList.Add(3, 4, FeatureStruct.New().Value);
        annList.Add(4, 5, FeatureStruct.New().Value);
        annList.Add(5, 6, FeatureStruct.New().Value);
        Assert.That(annList.Count, Is.EqualTo(6));
        a = new Annotation<int>(Range<int>.Create(1, 5), FeatureStruct.New().Value);
        a.Children.Add(1, 3, FeatureStruct.New().Value);
        a.Children.Add(3, 5, FeatureStruct.New().Value);
        Assert.That(a.Children.Count, Is.EqualTo(2));
        annList.Add(a);
        Assert.That(annList.Count, Is.EqualTo(3));
        Assert.That(annList.ElementAt(1), Is.SameAs(a));
        Assert.That(a.Children.Count, Is.EqualTo(2));
        Assert.That(a.Children.First.Children.Count, Is.EqualTo(2));
        Assert.That(a.Children.Last.Children.Count, Is.EqualTo(2));
    }

    [Test]
    public void Remove()
    {
        var annList = new AnnotationList<int>();
        annList.Add(0, 1, FeatureStruct.New().Value);
        annList.Add(9, 10, FeatureStruct.New().Value);
        annList.Add(24, 25, FeatureStruct.New().Value);
        annList.Add(49, 50, FeatureStruct.New().Value);
        annList.Add(99, 100, FeatureStruct.New().Value);

        annList.Remove(annList.First);
        Assert.That(annList.Count, Is.EqualTo(4));
        Assert.That(annList.First.Range, Is.EqualTo(Range<int>.Create(9, 10)));

        annList.Remove(annList.Last);
        Assert.That(annList.Count, Is.EqualTo(3));
        Assert.That(annList.Last.Range, Is.EqualTo(Range<int>.Create(49, 50)));

        annList.Remove(annList.First.Next);
        Assert.That(annList.Count, Is.EqualTo(2));
        annList.Remove(annList.First);
        Assert.That(annList.Count, Is.EqualTo(1));
        annList.Remove(annList.First);
        Assert.That(annList.Count, Is.EqualTo(0));

        annList.Add(0, 1, FeatureStruct.New().Value);
        annList.Add(9, 10, FeatureStruct.New().Value);
        annList.Add(49, 50, FeatureStruct.New().Value);
        annList.Add(69, 70, FeatureStruct.New().Value);
        annList.Add(99, 100, FeatureStruct.New().Value);
        annList.Add(0, 49, FeatureStruct.New().Value);
        annList.Add(51, 100, FeatureStruct.New().Value);

        annList.Remove(annList.First);
        Assert.That(annList.Count, Is.EqualTo(4));
        annList.Remove(annList.Last, false);
        Assert.That(annList.Count, Is.EqualTo(3));
    }

    [Test]
    public void Find()
    {
        var annList = new AnnotationList<int>();
        annList.Add(1, 2, FeatureStruct.New().Value);
        annList.Add(9, 10, FeatureStruct.New().Value);
        annList.Add(24, 25, FeatureStruct.New().Value);
        annList.Add(49, 50, FeatureStruct.New().Value);
        annList.Add(99, 100, FeatureStruct.New().Value);
        annList.Add(99, 100, FeatureStruct.New().Value);
        annList.Add(new Annotation<int>(Range<int>.Create(20, 70), FeatureStruct.New().Value), false);

        Annotation<int> result;
        Assert.That(annList.Find(0, out result), Is.False);
        Assert.That(result, Is.SameAs(annList.Begin));

        Assert.That(annList.Find(1, out result), Is.True);
        Assert.That(result, Is.SameAs(annList.First));

        Assert.That(annList.Find(100, out result), Is.False);
        Assert.That(result, Is.SameAs(annList.Last));

        Assert.That(annList.Find(101, out result), Is.False);
        Assert.That(result, Is.SameAs(annList.Last));

        Assert.That(annList.Find(30, out result), Is.False);
        Assert.That(result, Is.SameAs(annList.ElementAt(3)));

        Assert.That(annList.Find(9, out result), Is.True);
        Assert.That(result, Is.SameAs(annList.First.Next));

        Assert.That(annList.Find(101, Direction.RightToLeft, out result), Is.False);
        Assert.That(result, Is.SameAs(annList.End));

        Assert.That(annList.Find(100, Direction.RightToLeft, out result), Is.True);
        Assert.That(result, Is.SameAs(annList.Last));

        Assert.That(annList.Find(1, Direction.RightToLeft, out result), Is.False);
        Assert.That(result, Is.SameAs(annList.First));

        Assert.That(annList.Find(0, Direction.RightToLeft, out result), Is.False);
        Assert.That(result, Is.SameAs(annList.First));

        Assert.That(annList.Find(15, Direction.RightToLeft, out result), Is.False);
        Assert.That(result, Is.SameAs(annList.ElementAt(2)));

        Assert.That(annList.Find(10, Direction.RightToLeft, out result), Is.True);
        Assert.That(result, Is.SameAs(annList.First.Next));
    }

    [Test]
    public void GetNodes()
    {
        var annList = new AnnotationList<int>();
        annList.Add(1, 2, FeatureStruct.New().Value);
        annList.Add(9, 10, FeatureStruct.New().Value);
        annList.Add(24, 25, FeatureStruct.New().Value);
        annList.Add(49, 50, FeatureStruct.New().Value);
        annList.Add(99, 100, FeatureStruct.New().Value);
        annList.Add(new Annotation<int>(Range<int>.Create(20, 70), FeatureStruct.New().Value), false);

        Assert.That(annList.GetNodes(0, 1).Any(), Is.False);

        Assert.That(annList.GetNodes(100, 101).Any(), Is.False);

        Annotation<int>[] anns = annList.GetNodes(8, 52).ToArray();
        Assert.That(anns.Length, Is.EqualTo(3));
        Assert.That(anns[0], Is.EqualTo(annList.First.Next));
        Assert.That(anns[2], Is.EqualTo(annList.Last.Prev));

        anns = annList.GetNodes(9, 10).ToArray();
        Assert.That(anns.Length, Is.EqualTo(1));
        Assert.That(anns[0], Is.EqualTo(annList.First.Next));

        anns = annList.GetNodes(0, 200).ToArray();
        Assert.That(anns.Length, Is.EqualTo(6));
    }

    [Test]
    public void FindDepthFirst()
    {
        var annList = new AnnotationList<int>();
        annList.Add(1, 2, FeatureStruct.New().Value);
        annList.Add(9, 10, FeatureStruct.New().Value);
        annList.Add(49, 50, FeatureStruct.New().Value);
        annList.Add(69, 70, FeatureStruct.New().Value);
        annList.Add(99, 100, FeatureStruct.New().Value);
        annList.Add(1, 49, FeatureStruct.New().Value);
        annList.Add(51, 100, FeatureStruct.New().Value);

        Annotation<int> result;
        Assert.That(annList.FindDepthFirst(0, out result), Is.False);
        Assert.That(result, Is.EqualTo(annList.Begin));

        Assert.That(annList.FindDepthFirst(100, out result), Is.False);
        Assert.That(result, Is.EqualTo(annList.Last));

        Assert.That(annList.FindDepthFirst(1, out result), Is.True);
        Assert.That(result, Is.EqualTo(annList.First));

        Assert.That(annList.FindDepthFirst(8, out result), Is.False);
        Assert.That(result, Is.EqualTo(annList.First.Children.First));

        Assert.That(annList.FindDepthFirst(99, out result), Is.True);
        Assert.That(result, Is.EqualTo(annList.Last.Children.Last));

        Assert.That(annList.FindDepthFirst(49, out result), Is.True);
        Assert.That(result, Is.EqualTo(annList.First.Next));

        Assert.That(annList.FindDepthFirst(101, Direction.RightToLeft, out result), Is.False);
        Assert.That(result, Is.EqualTo(annList.End));

        Assert.That(annList.FindDepthFirst(1, Direction.RightToLeft, out result), Is.False);
        Assert.That(result, Is.EqualTo(annList.First));

        Assert.That(annList.FindDepthFirst(100, Direction.RightToLeft, out result), Is.True);
        Assert.That(result, Is.EqualTo(annList.Last));

        Assert.That(annList.FindDepthFirst(71, Direction.RightToLeft, out result), Is.False);
        Assert.That(result, Is.EqualTo(annList.Last.Children.Last));

        Assert.That(annList.FindDepthFirst(2, Direction.RightToLeft, out result), Is.True);
        Assert.That(result, Is.EqualTo(annList.First.Children.First));

        Assert.That(annList.FindDepthFirst(50, Direction.RightToLeft, out result), Is.True);
        Assert.That(result, Is.EqualTo(annList.Last.Prev));
    }

    // Copy-on-write safety net for the Shape/ShapeNode refactor (Plan B): cloning a frozen
    // Shape and mutating a cloned node's FeatureStruct must not change the source shape.
    private static Shape BuildShape(FeatureSystem featSys)
    {
        var shape = new Shape(end => new ShapeNode(FeatureStruct.New().Value));
        shape.Add(FeatureStruct.New(featSys).Symbol("a1").Value);
        shape.Add(FeatureStruct.New(featSys).Symbol("a2").Value);
        shape.Add(FeatureStruct.New(featSys).Symbol("a3").Value);
        shape.Freeze();
        return shape;
    }

    [Test]
    public void CloneShape_MutateClonedNodeFeatureStruct_LeavesSourceShapeUnchanged()
    {
        var featSys = new FeatureSystem
        {
            new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2"), new FeatureSymbol("a3")),
            new SymbolicFeature("b", new FeatureSymbol("b1"), new FeatureSymbol("b2")),
        };
        featSys.Freeze();

        Shape source = BuildShape(featSys);
        Shape expected = BuildShape(featSys);
        Shape clone = source.Clone();

        // CopyTo fidelity: same node count and value-equal to the source.
        Assert.That(clone.Count, Is.EqualTo(source.Count));
        Assert.That(clone.ValueEquals(source), Is.True);

        // Mutate the first cloned node's feature struct (the in-place pattern HermitCrab uses).
        clone.First.Annotation.FeatureStruct.AddValue(
            featSys.GetFeature("b"),
            new SymbolicFeatureValue(featSys.GetSymbol("b1"))
        );

        // The source shape must be byte-for-byte unchanged.
        Assert.That(source.ValueEquals(expected), Is.True, "frozen source shape changed by a clone-node mutation");
        Assert.That(source.First.Annotation.FeatureStruct.ContainsFeature(featSys.GetFeature("b")), Is.False);
        Assert.That(clone.First.Annotation.FeatureStruct.ContainsFeature(featSys.GetFeature("b")), Is.True);
    }

    // RUSTIFY Stage 2 thesis check: the FST flip from TOffset = ShapeNode to TOffset = int maps each
    // annotation [startNode, endNode] to the half-open int range [startNode.Tag, endNode.Tag + 1].
    // The whole flip's correctness rests on that mapping preserving the range relationships the FST
    // traversal depends on (ordering via CompareTo, Overlaps, Contains) — for SPARSE tags (an
    // appended, unfrozen shape: rewrite rules mutate + match unfrozen) AND dense tags (frozen). This
    // validates that thesis empirically before any code is built on it.
    private static System.Collections.Generic.List<Annotation<ShapeNode>> BuildSpannedShape(
        FeatureSystem featSys,
        bool freeze
    )
    {
        var shape = new Shape(end => new ShapeNode(FeatureStruct.New().Value));
        shape.Add(FeatureStruct.New(featSys).Symbol("a1").Value);
        ShapeNode n1 = shape.Add(FeatureStruct.New(featSys).Symbol("a2").Value);
        ShapeNode n2 = shape.Add(FeatureStruct.New(featSys).Symbol("a3").Value);
        shape.Add(FeatureStruct.New(featSys).Symbol("a1").Value);
        // a spanning (morph-like) annotation over the two middle nodes — exercises start != end
        shape.Annotations.Add(Range<ShapeNode>.Create(n1, n2), FeatureStruct.New(featSys).Symbol("a2").Value);
        if (freeze)
            shape.Freeze();

        // every annotation (leaves + the span + its children), excluding the Begin/End anchors whose
        // int.MinValue/int.MaxValue tags are handled separately in the real projection
        var anns = new System.Collections.Generic.List<Annotation<ShapeNode>>();
        foreach (Annotation<ShapeNode> top in shape.Annotations)
        {
            foreach (Annotation<ShapeNode> a in top.GetNodesDepthFirst())
            {
                if (a.Range.Start.Tag != int.MinValue && a.Range.End.Tag != int.MaxValue)
                    anns.Add(a);
            }
        }
        return anns;
    }

    private static Shape BuildSpannedShapeObject(FeatureSystem featSys, bool freeze)
    {
        var shape = new Shape(end => new ShapeNode(FeatureStruct.New().Value));
        shape.Add(FeatureStruct.New(featSys).Symbol("a1").Value);
        ShapeNode n1 = shape.Add(FeatureStruct.New(featSys).Symbol("a2").Value);
        ShapeNode n2 = shape.Add(FeatureStruct.New(featSys).Symbol("a3").Value);
        shape.Add(FeatureStruct.New(featSys).Symbol("a1").Value);
        shape.Annotations.Add(Range<ShapeNode>.Create(n1, n2), FeatureStruct.New(featSys).Symbol("a2").Value);
        if (freeze)
            shape.Freeze();
        return shape;
    }

    private static void AssertProjectionMatches(Shape shape, Annotation<ShapeNode> src, Annotation<int> proj)
    {
        // offset = dense node position; a node [s,e] -> half-open [off(s), off(e)+1)
        Range<int> expected = Range<int>.Create(shape.OffsetOf(src.Range.Start), shape.OffsetOf(src.Range.End) + 1);
        Assert.That(proj.Range, Is.EqualTo(expected), "projected range");
        Assert.That(proj.Optional, Is.EqualTo(src.Optional), "projected optional");
        // FeatureStruct is shared by reference so in-place edits stay visible to the int view
        Assert.That(proj.FeatureStruct, Is.SameAs(src.FeatureStruct), "projected FeatureStruct identity");
        Assert.That(proj.Children.Count, Is.EqualTo(src.IsLeaf ? 0 : src.Children.Count), "projected child count");
        if (!src.IsLeaf)
        {
            Annotation<ShapeNode>[] sc = src.Children.ToArray();
            Annotation<int>[] pc = proj.Children.ToArray();
            for (int k = 0; k < sc.Length; k++)
                AssertProjectionMatches(shape, sc[k], pc[k]);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void IntAnnotationProjection_MirrorsShapeNodeAnnotations(bool freeze)
    {
        var featSys = new FeatureSystem
        {
            new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2"), new FeatureSymbol("a3")),
        };
        featSys.Freeze();

        Shape shape = BuildSpannedShapeObject(featSys, freeze);

        AnnotationList<int> proj = shape.IntAnnotations;
        Assert.That(proj.Count, Is.EqualTo(shape.Annotations.Count), "top-level count");

        // top-level annotations correspond in order (the int range mapping preserves ordering)
        Annotation<ShapeNode>[] srcTop = shape.Annotations.ToArray();
        Annotation<int>[] projTop = proj.ToArray();
        for (int k = 0; k < srcTop.Length; k++)
            AssertProjectionMatches(shape, srcTop[k], projTop[k]);

        // NodeAt/OffsetOf round-trip every node (dense offset), including margins
        foreach (ShapeNode node in shape)
            Assert.That(shape.NodeAt(shape.OffsetOf(node)), Is.SameAs(node), "NodeAt(OffsetOf(node)) round-trip");
        Assert.That(shape.NodeAt(shape.OffsetOf(shape.Begin)), Is.SameAs(shape.Begin));
        Assert.That(shape.NodeAt(shape.OffsetOf(shape.End)), Is.SameAs(shape.End));

        // the projection is cached against the annotation Version
        Assert.That(shape.IntAnnotations, Is.SameAs(proj), "projection cached when unchanged");
        if (!freeze)
        {
            shape.Add(FeatureStruct.New(featSys).Symbol("a1").Value);
            Assert.That(shape.IntAnnotations, Is.Not.SameAs(proj), "projection rebuilt after a mutation");
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void IntOffsetRangeMapping_PreservesShapeNodeRangeRelationships(bool freeze)
    {
        var featSys = new FeatureSystem
        {
            new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2"), new FeatureSymbol("a3")),
        };
        featSys.Freeze();

        System.Collections.Generic.List<Annotation<ShapeNode>> anns = BuildSpannedShape(featSys, freeze);
        Assert.That(anns.Count, Is.GreaterThanOrEqualTo(4));

        // sanity: appended (unfrozen) tags really are sparse, not 0..N-1
        if (!freeze)
        {
            var tags = anns.Select(a => a.Range.Start.Tag).Distinct().OrderBy(t => t).ToArray();
            Assert.That(tags.Length > 1 && tags[1] - tags[0] > 1, Is.True, "expected sparse appended tags");
        }

        static Range<int> ToInt(Annotation<ShapeNode> a) =>
            Range<int>.Create(a.Range.Start.Tag, a.Range.End.Tag + 1);

        foreach (Annotation<ShapeNode> x in anns)
        {
            foreach (Annotation<ShapeNode> y in anns)
            {
                Range<ShapeNode> xs = x.Range,
                    ys = y.Range;
                Range<int> xi = ToInt(x),
                    yi = ToInt(y);

                Assert.That(
                    System.Math.Sign(xi.CompareTo(yi)),
                    Is.EqualTo(System.Math.Sign(xs.CompareTo(ys))),
                    $"CompareTo sign diverged: shape={xs}.CompareTo({ys}) int={xi}.CompareTo({yi})"
                );
                Assert.That(
                    xi.Overlaps(yi),
                    Is.EqualTo(xs.Overlaps(ys)),
                    $"Overlaps diverged: shape={xs}/{ys} int={xi}/{yi}"
                );
                Assert.That(
                    xi.Contains(yi),
                    Is.EqualTo(xs.Contains(ys)),
                    $"Contains diverged: shape={xs}/{ys} int={xi}/{yi}"
                );
            }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void IntRange_StartsAtBoundaryAnchorInEachDirection(bool freeze)
    {
        var featSys = new FeatureSystem
        {
            new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2"), new FeatureSymbol("a3")),
        };
        featSys.Freeze();

        Shape shape = BuildSpannedShapeObject(featSys, freeze);

        // A directional match begins at IntRange.GetStart(dir); that offset must resolve to the
        // boundary anchor itself (Begin for LtR, End for RtL). The End anchor's dense node range is
        // half-open [off(End), off(End)+1), so its RtL start coordinate is off(End)+1 — IntRange must
        // carry the +1, or a RtL match would begin at the last content node and skip any edit adjacent
        // to End (e.g. inserting a deleted segment after the final vowel during analysis).
        Assert.That(
            shape.IntRange.GetStart(Direction.LeftToRight),
            Is.EqualTo(shape.MatchStartOffset(shape.Begin, Direction.LeftToRight)),
            "a LtR match must start at the Begin anchor"
        );
        Assert.That(
            shape.IntRange.GetStart(Direction.RightToLeft),
            Is.EqualTo(shape.MatchStartOffset(shape.End, Direction.RightToLeft)),
            "a RtL match must start at the End anchor"
        );
    }

    [Test]
    public void Optional_FlipInvalidatesIntProjection()
    {
        var featSys = new FeatureSystem
        {
            new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2"), new FeatureSymbol("a3")),
        };
        featSys.Freeze();

        // unfrozen: Optional is only ever flipped on a mutable shape (during analysis/unapplication)
        Shape shape = BuildSpannedShapeObject(featSys, freeze: false);

        AnnotationList<int> proj = shape.IntAnnotations;
        ShapeNode node = shape.First;
        Assert.That(node.Annotation.Optional, Is.False);

        // Flipping Optional is a non-structural change. The int projection copies Optional by value and
        // caches against the annotation Version, so the flip must invalidate the cache — otherwise the
        // matcher keeps seeing the stale flag and never forks the optional-skip instances.
        node.Annotation.Optional = true;

        AnnotationList<int> proj2 = shape.IntAnnotations;
        Assert.That(proj2, Is.Not.SameAs(proj), "projection rebuilt after an Optional flip");
        int off = shape.OffsetOf(node);
        Annotation<int> projNode = proj2.Single(a => a.Range.Start == off);
        Assert.That(projNode.Optional, Is.True, "rebuilt projection reflects the flipped Optional flag");
    }

    [Test]
    public void CopyOnWriteClone_NeverInflated_ServesProjectionFromSource()
    {
        var featSys = new FeatureSystem
        {
            new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2"), new FeatureSymbol("a3")),
        };
        featSys.Freeze();

        // RUSTIFY Stage 3 (III): a clone of a FROZEN shape is copy-on-write — it copies nothing and serves
        // the int-offset projection from its frozen source, so a traverse-only clone never materializes.
        Shape src = BuildSpannedShapeObject(featSys, freeze: true);
        AnnotationList<int> srcProj = src.IntAnnotations;

        Shape clone = src.Clone();
        Assert.That(clone.Count, Is.EqualTo(src.Count), "COW clone reports the source content count");
        Assert.That(clone.IntAnnotations, Is.SameAs(srcProj), "COW clone serves the source's projection");
        Assert.That(clone.IntRange, Is.EqualTo(src.IntRange), "COW clone serves the source's int range");
    }

    [Test]
    public void CopyOnWriteClone_MutationInflatesAndDoesNotCorruptSource()
    {
        var featSys = new FeatureSystem
        {
            new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2"), new FeatureSymbol("a3")),
        };
        featSys.Freeze();

        Shape src = BuildSpannedShapeObject(featSys, freeze: true);
        int srcCount = src.Count;
        AnnotationList<int> srcProj = src.IntAnnotations;

        Shape clone = src.Clone();
        // Touch a handle, then mutate — this must inflate the clone's own node graph, leaving the shared
        // frozen source untouched (the corruption case the gating exists to prevent).
        ShapeNode first = clone.First;
        clone.AddAfter(first, FeatureStruct.New(featSys).Symbol("a1").Value);

        Assert.That(clone.Count, Is.EqualTo(srcCount + 1), "the clone was mutated");
        Assert.That(src.Count, Is.EqualTo(srcCount), "the frozen source count is unchanged");
        Assert.That(src.IntAnnotations, Is.SameAs(srcProj), "the frozen source projection is unchanged");
    }

    [Test]
    public void CopyOnWriteClone_FrozenBySharing_HashStableAcrossInflation()
    {
        var featSys = new FeatureSystem
        {
            new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2"), new FeatureSymbol("a3")),
        };
        featSys.Freeze();

        Shape src = BuildSpannedShapeObject(featSys, freeze: true);
        int srcHash = src.GetFrozenHashCode();

        Shape clone = src.Clone();
        clone.Freeze(); // no-op: adopts the source's frozen state + hash without materializing nodes
        Assert.That(clone.GetFrozenHashCode(), Is.EqualTo(srcHash), "frozen-by-sharing hash equals the source");

        // Forcing inflation (handle access) re-materializes + re-freezes; the hash must be unchanged.
        ShapeNode _ = clone.First;
        Assert.That(clone.GetFrozenHashCode(), Is.EqualTo(srcHash), "hash stable across COW inflation");
    }
}
