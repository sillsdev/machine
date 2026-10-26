using System.Reflection;
using NUnit.Framework;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Annotations;

[TestFixture]
public class AnnotationTiedContainerTests
{
    private const int ContainerCount = 4;

    private static Shape CreateShape(int seed, out ShapeNode[] nodes)
    {
        var annotations = new AnnotationList<ShapeNode>();
        typeof(BidirList<Annotation<ShapeNode>>)
            .GetField("_rand", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(annotations, new Random(seed));
        var shape = new Shape(begin => new ShapeNode(FeatureStruct.New().Value), annotations);
        nodes = new ShapeNode[3];
        for (int i = 0; i < nodes.Length; i++)
            nodes[i] = shape.Add(FeatureStruct.New().Value);
        return shape;
    }

    private static Annotation<ShapeNode> NewContainer(ShapeNode[] nodes)
    {
        return new Annotation<ShapeNode>(Range<ShapeNode>.Create(nodes[0], nodes[2]), FeatureStruct.New().Value);
    }

    // Only the tied container at ownerIndex owns the segments, so the one Find returns varies with insertion order.
    [Test]
    public void FindDepthFirst_TiedContainers_FindsSegmentInOwner(
        [Values(0, 1, 2, 3, 4, 5)] int seed,
        [Range(0, ContainerCount - 1)] int ownerIndex,
        [Values] Direction dir
    )
    {
        Shape shape = CreateShape(seed, out ShapeNode[] nodes);
        for (int i = 0; i < ContainerCount; i++)
        {
            Annotation<ShapeNode> container = NewContainer(nodes);
            if (i == ownerIndex)
                container.Children.AddRange(nodes.Select(n => n.Annotation));
            shape.Annotations.Add(container, false);
        }

        Assert.That(shape.Annotations.FindDepthFirst(nodes[1], dir, out Annotation<ShapeNode> result), Is.True);
        Assert.That(result, Is.SameAs(nodes[1].Annotation));
    }

    // The owner is itself a container of tied containers, only one of which owns the segment annotations.
    [Test]
    public void FindDepthFirst_NestedTiedContainers_FindsSegmentInInnermostOwner(
        [Values(0, 1, 2, 3, 4, 5)] int seed,
        [Range(0, ContainerCount - 1)] int outerOwnerIndex,
        [Range(0, ContainerCount - 1)] int innerOwnerIndex,
        [Values] Direction dir
    )
    {
        Shape shape = CreateShape(seed, out ShapeNode[] nodes);
        for (int i = 0; i < ContainerCount; i++)
        {
            Annotation<ShapeNode> outer = NewContainer(nodes);
            if (i == outerOwnerIndex)
            {
                for (int j = 0; j < ContainerCount; j++)
                {
                    Annotation<ShapeNode> inner = NewContainer(nodes);
                    if (j == innerOwnerIndex)
                        inner.Children.AddRange(nodes.Select(n => n.Annotation));
                    outer.Children.Add(inner, false);
                }
            }
            shape.Annotations.Add(outer, false);
        }

        Assert.That(shape.Annotations.FindDepthFirst(nodes[1], dir, out Annotation<ShapeNode> result), Is.True);
        Assert.That(result, Is.SameAs(nodes[1].Annotation));
    }

    // Tied containers that own only the other segments must not hide, or stand in for, an annotation that is not
    // inside any of them.
    [Test]
    public void FindDepthFirst_TiedContainersNotOwningTheOffset_ReturnsTheUnownedAnnotation(
        [Values(0, 1, 2, 3, 4, 5)] int seed,
        [Values] Direction dir
    )
    {
        Shape shape = CreateShape(seed, out ShapeNode[] nodes);
        for (int i = 0; i < ContainerCount; i++)
        {
            Annotation<ShapeNode> container = NewContainer(nodes);
            if (i == 0)
                container.Children.AddRange(new[] { nodes[0].Annotation, nodes[2].Annotation });
            shape.Annotations.Add(container, false);
        }

        Assert.That(shape.Annotations.FindDepthFirst(nodes[1], dir, out Annotation<ShapeNode> result), Is.True);
        Assert.That(result, Is.SameAs(nodes[1].Annotation));
    }
}
