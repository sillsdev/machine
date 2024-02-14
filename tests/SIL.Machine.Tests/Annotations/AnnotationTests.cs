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
        Assert.IsFalse(annList.Find(0, out result));
        Assert.That(result, Is.SameAs(annList.Begin));

        Assert.IsTrue(annList.Find(1, out result));
        Assert.That(result, Is.SameAs(annList.First));

        Assert.IsFalse(annList.Find(100, out result));
        Assert.That(result, Is.SameAs(annList.Last));

        Assert.IsFalse(annList.Find(101, out result));
        Assert.That(result, Is.SameAs(annList.Last));

        Assert.IsFalse(annList.Find(30, out result));
        Assert.That(result, Is.SameAs(annList.ElementAt(3)));

        Assert.IsTrue(annList.Find(9, out result));
        Assert.That(result, Is.SameAs(annList.First.Next));

        Assert.IsFalse(annList.Find(101, Direction.RightToLeft, out result));
        Assert.That(result, Is.SameAs(annList.End));

        Assert.IsTrue(annList.Find(100, Direction.RightToLeft, out result));
        Assert.That(result, Is.SameAs(annList.Last));

        Assert.IsFalse(annList.Find(1, Direction.RightToLeft, out result));
        Assert.That(result, Is.SameAs(annList.First));

        Assert.IsFalse(annList.Find(0, Direction.RightToLeft, out result));
        Assert.That(result, Is.SameAs(annList.First));

        Assert.IsFalse(annList.Find(15, Direction.RightToLeft, out result));
        Assert.That(result, Is.SameAs(annList.ElementAt(2)));

        Assert.IsTrue(annList.Find(10, Direction.RightToLeft, out result));
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

        Assert.IsFalse(annList.GetNodes(0, 1).Any());

        Assert.IsFalse(annList.GetNodes(100, 101).Any());

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
        Assert.IsFalse(annList.FindDepthFirst(0, out result));
        Assert.That(result, Is.EqualTo(annList.Begin));

        Assert.IsFalse(annList.FindDepthFirst(100, out result));
        Assert.That(result, Is.EqualTo(annList.Last));

        Assert.IsTrue(annList.FindDepthFirst(1, out result));
        Assert.That(result, Is.EqualTo(annList.First));

        Assert.IsFalse(annList.FindDepthFirst(8, out result));
        Assert.That(result, Is.EqualTo(annList.First.Children.First));

        Assert.IsTrue(annList.FindDepthFirst(99, out result));
        Assert.That(result, Is.EqualTo(annList.Last.Children.Last));

        Assert.IsTrue(annList.FindDepthFirst(49, out result));
        Assert.That(result, Is.EqualTo(annList.First.Next));

        Assert.IsFalse(annList.FindDepthFirst(101, Direction.RightToLeft, out result));
        Assert.That(result, Is.EqualTo(annList.End));

        Assert.IsFalse(annList.FindDepthFirst(1, Direction.RightToLeft, out result));
        Assert.That(result, Is.EqualTo(annList.First));

        Assert.IsTrue(annList.FindDepthFirst(100, Direction.RightToLeft, out result));
        Assert.That(result, Is.EqualTo(annList.Last));

        Assert.IsFalse(annList.FindDepthFirst(71, Direction.RightToLeft, out result));
        Assert.That(result, Is.EqualTo(annList.Last.Children.Last));

        Assert.IsTrue(annList.FindDepthFirst(2, Direction.RightToLeft, out result));
        Assert.That(result, Is.EqualTo(annList.First.Children.First));

        Assert.IsTrue(annList.FindDepthFirst(50, Direction.RightToLeft, out result));
        Assert.That(result, Is.EqualTo(annList.Last.Prev));
    }
}
