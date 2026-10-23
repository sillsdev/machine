using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Annotations;

[TestFixture]
public class AnnotationTieTests
{
    private const int ExtraCount = 5;
    private const string TagFeatureID = "test-tag";
    private static readonly FeatureSystem TagFeatureSystem = new() { new StringFeature(TagFeatureID) };
    private static readonly FieldInfo RandomField = typeof(BidirList<Annotation<ShapeNode>>).GetField(
        "_rand",
        BindingFlags.Instance | BindingFlags.NonPublic
    )!;

    private static Shape CreateEmptyShape(int seed)
    {
        var annotations = new AnnotationList<ShapeNode>();
        // Seed before Shape construction inserts its margins and later annotations.
        RandomField.SetValue(annotations, new Random(seed));
        return new Shape(begin => new ShapeNode(TaggedFeatures(begin ? "margin-begin" : "margin-end")), annotations);
    }

    private static (Shape Shape, ShapeNode[] Nodes) BuildShape(int seed)
    {
        var shape = CreateEmptyShape(seed);
        var nodes = new ShapeNode[3];
        for (int i = 0; i < nodes.Length; i++)
            nodes[i] = shape.Add(TaggedFeatures(IndexTag("node", i)));

        foreach (int nodeIndex in new[] { 1, 2 })
        {
            for (int i = 0; i < ExtraCount; i++)
            {
                var ann = new Annotation<ShapeNode>(
                    Range<ShapeNode>.Create(nodes[nodeIndex], nodes[nodeIndex]),
                    TaggedFeatures(IndexTag("point-" + nodeIndex.ToString(CultureInfo.InvariantCulture), i))
                );
                shape.Annotations.Add(ann, false);
            }
        }

        return (shape, nodes);
    }

    private static (
        Shape Shape,
        ShapeNode[] Nodes,
        Annotation<ShapeNode> Owner,
        Annotation<ShapeNode>[] TiedAnnotations
    ) BuildNestedEndpointTieShape(int seed, int tiedEndpointNodeIndex)
    {
        var shape = CreateEmptyShape(seed);
        var nodes = new ShapeNode[4];
        for (int i = 0; i < nodes.Length; i++)
            nodes[i] = shape.Add(TaggedFeatures(IndexTag("nested-node", i)));

        var owner = new Annotation<ShapeNode>(
            Range<ShapeNode>.Create(nodes[1], nodes[3]),
            TaggedFeatures("multi-node-owner")
        );
        RandomField.SetValue(owner.Children, new Random(seed + 200));
        owner.Children.AddRange(new[] { nodes[1].Annotation, nodes[2].Annotation, nodes[3].Annotation });
        var tiedAnnotations = new Annotation<ShapeNode>[ExtraCount];
        for (int i = 0; i < ExtraCount; i++)
        {
            tiedAnnotations[i] = new Annotation<ShapeNode>(
                Range<ShapeNode>.Create(nodes[tiedEndpointNodeIndex], nodes[tiedEndpointNodeIndex]),
                TaggedFeatures(IndexTag("endpoint-tie", i))
            );
            owner.Children.Add(tiedAnnotations[i], false);
        }

        shape.Annotations.Add(owner, false);
        return (shape, nodes, owner, tiedAnnotations);
    }

    private static FeatureStruct TaggedFeatures(string tag)
    {
        return FeatureStruct.New(TagFeatureSystem).Feature(TagFeatureID).EqualTo(tag).Value;
    }

    private static string IndexTag(string prefix, int index)
    {
        return prefix + "-" + index.ToString(CultureInfo.InvariantCulture);
    }

    private static AnnotationSnapshot Snapshot(Annotation<ShapeNode> annotation)
    {
        return new AnnotationSnapshot(
            annotation.Range.Start,
            annotation.Range.End,
            annotation.FeatureStruct.GetValue<StringFeatureValue>(TagFeatureID).Values.Single()
        );
    }

    private static void AssertMappedAnnotations(Shape source, Shape destination)
    {
        ShapeNode[] sourceNodes = source.ToArray();
        ShapeNode[] destinationNodes = destination.ToArray();
        Assert.That(destinationNodes, Has.Length.EqualTo(sourceNodes.Length));

        var nodeMap = new Dictionary<ShapeNode, ShapeNode>();
        for (int i = 0; i < sourceNodes.Length; i++)
            nodeMap.Add(sourceNodes[i], destinationNodes[i]);

        AnnotationSnapshot[] expected = source
            .Annotations.Where(annotation =>
                annotation != source.Begin.Annotation && annotation != source.End.Annotation
            )
            .Select(annotation => new AnnotationSnapshot(
                nodeMap[annotation.Range.Start],
                nodeMap[annotation.Range.End],
                Snapshot(annotation).Tag
            ))
            .Concat(new[] { Snapshot(destination.Begin.Annotation), Snapshot(destination.End.Annotation) })
            .ToArray();
        AnnotationSnapshot[] actual = destination.Annotations.Select(Snapshot).ToArray();

        Assert.That(actual, Is.EquivalentTo(expected));
        Assert.That(actual.All(annotation => destination.Contains(annotation.Start)), Is.True);
        Assert.That(actual.All(annotation => destination.Contains(annotation.End)), Is.True);
    }

    private static void AssertNestedEndpointTieShrinks(int seed, int removedNodeIndex, int expectedStartNodeIndex)
    {
        (Shape shape, ShapeNode[] nodes, Annotation<ShapeNode> owner, Annotation<ShapeNode>[] tiedAnnotations) =
            BuildNestedEndpointTieShape(seed, removedNodeIndex);
        Annotation<ShapeNode>[] expectedRootAnnotations = shape
            .Annotations.Where(annotation => annotation != owner)
            .ToArray();
        Annotation<ShapeNode>[] expectedChildAnnotations = owner
            .Children.Where(annotation =>
                annotation != nodes[removedNodeIndex].Annotation && !tiedAnnotations.Contains(annotation)
            )
            .ToArray();

        shape.Remove(nodes[removedNodeIndex]);

        Annotation<ShapeNode>[] actualRootAnnotations = shape.Annotations.ToArray();
        Assert.That(actualRootAnnotations, Has.Length.EqualTo(expectedRootAnnotations.Length + 1));
        foreach (Annotation<ShapeNode> annotation in expectedRootAnnotations)
            Assert.That(actualRootAnnotations, Does.Contain(annotation));
        Assert.That(actualRootAnnotations, Does.Not.Contain(owner));

        Annotation<ShapeNode> resizedOwner = actualRootAnnotations.Single(annotation =>
            Snapshot(annotation).Tag == "multi-node-owner"
        );
        ShapeNode expectedEnd = removedNodeIndex == 3 ? nodes[2] : nodes[3];
        Assert.That(resizedOwner.Range.Start, Is.SameAs(nodes[expectedStartNodeIndex]));
        Assert.That(resizedOwner.Range.End, Is.SameAs(expectedEnd));
        Assert.That(resizedOwner.Children.Count, Is.EqualTo(expectedChildAnnotations.Length));
        Assert.That(resizedOwner.Children, Is.EquivalentTo(expectedChildAnnotations));
        foreach (Annotation<ShapeNode> annotation in tiedAnnotations)
            Assert.That(resizedOwner.Children, Does.Not.Contain(annotation));
        Assert.That(
            resizedOwner.Children.Any(annotation =>
                Snapshot(annotation).Tag.StartsWith("endpoint-tie-", StringComparison.Ordinal)
            ),
            Is.False
        );
    }

    [Test]
    public void Find_ReturnsFirstMatchingAnnotationInSearchDirection([Range(0, 199)] int seed)
    {
        (Shape shape, ShapeNode[] nodes) = BuildShape(seed);
        Annotation<ShapeNode>[] tied = shape
            .Annotations.Where(ann => ann.Range.Start == nodes[1] && ann.Range.End == nodes[1])
            .ToArray();
        var probe = new Annotation<ShapeNode>(Range<ShapeNode>.Create(nodes[1], nodes[1]), TaggedFeatures("probe"));

        Assert.That(
            shape.Annotations.Find(probe, Direction.LeftToRight, out Annotation<ShapeNode> leftToRight),
            Is.True
        );
        Assert.That(leftToRight, Is.SameAs(tied[0]));
        Assert.That(
            shape.Annotations.Find(probe, Direction.RightToLeft, out Annotation<ShapeNode> rightToLeft),
            Is.True
        );
        Assert.That(rightToLeft, Is.SameAs(tied[^1]));
    }

    [Test]
    public void GetNodes_ReturnsAnnotationsAtBothTiedBoundaries([Range(0, 199)] int seed)
    {
        (Shape shape, ShapeNode[] nodes) = BuildShape(seed);

        Assert.That(shape.Annotations.GetNodes(nodes[0], nodes[2]).Count(), Is.EqualTo(3 + 2 * ExtraCount));
        Assert.That(shape.Annotations.GetNodes(nodes[1], nodes[2]).Count(), Is.EqualTo(2 + 2 * ExtraCount));
        Assert.That(shape.Annotations.GetNodes(nodes[0], nodes[1]).Count(), Is.EqualTo(2 + ExtraCount));
        Assert.That(shape.Annotations.GetNodes(nodes[1], nodes[1]).Count(), Is.EqualTo(1 + ExtraCount));
    }

    [Test]
    public void CopyAndClone_PreserveMappedAnnotationRangesAndPayloads([Range(0, 199)] int seed)
    {
        (Shape shape, _) = BuildShape(seed);
        Shape copy = CreateEmptyShape(seed + 200);

        shape.CopyTo(copy);
        AssertMappedAnnotations(shape, copy);

        Shape clone = shape.Clone();
        AssertMappedAnnotations(shape, clone);
    }

    [Test]
    public void RemoveNode_PreservesExactUnaffectedAnnotations([Range(0, 199)] int seed)
    {
        (Shape shape, ShapeNode[] nodes) = BuildShape(seed);
        Annotation<ShapeNode>[] expectedSurvivors = shape
            .Annotations.Where(annotation => annotation.Range.Start != nodes[1] && annotation.Range.End != nodes[1])
            .ToArray();

        shape.Remove(nodes[1]);

        Assert.That(shape.Annotations.Count, Is.EqualTo(expectedSurvivors.Length));
        Assert.That(shape.Annotations, Is.EquivalentTo(expectedSurvivors));
        Assert.That(
            shape.Annotations.Any(annotation => annotation.Range.Start == nodes[1] || annotation.Range.End == nodes[1]),
            Is.False
        );
    }

    [Test]
    public void RemoveStartEndpoint_ShrinksMultiNodeOwnerAndRemovesEveryTie([Range(0, 199)] int seed)
    {
        AssertNestedEndpointTieShrinks(seed, removedNodeIndex: 1, expectedStartNodeIndex: 2);
    }

    [Test]
    public void RemoveEndEndpoint_ShrinksMultiNodeOwnerAndRemovesEveryTie([Range(0, 199)] int seed)
    {
        AssertNestedEndpointTieShrinks(seed, removedNodeIndex: 3, expectedStartNodeIndex: 1);
    }

    private readonly record struct AnnotationSnapshot(ShapeNode Start, ShapeNode End, string Tag);
}
