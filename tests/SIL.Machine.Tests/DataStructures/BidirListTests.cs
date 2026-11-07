using System.Reflection;
using NUnit.Framework;
using SIL.Machine.DataStructures;

namespace SIL.Machine.Tests.DataStructures;

[TestFixture]
public class BidirListTests
{
    [TestCase(Direction.LeftToRight)]
    [TestCase(Direction.RightToLeft)]
    public void Find_WithStart_DoesNotReturnEarlierTiedNode(Direction direction)
    {
        var list = new TestList();
        var left = new TestNode(1);
        var start = new TestNode(1);
        var right = new TestNode(1);
        list.Add(left);
        list.Add(start);
        list.Add(right);

        Assert.That(list.Find(start, new TestNode(1), direction, out TestNode result), Is.True);
        Assert.That(result, Is.SameAs(start));
    }

    [TestCase(Direction.LeftToRight, 0)]
    [TestCase(Direction.RightToLeft, 2)]
    public void Find_WithStartAndEarlierExample_DoesNotReturnEarlierNode(Direction direction, int example)
    {
        var list = new TestList();
        list.Add(new TestNode(1));
        var start = new TestNode(1);
        list.Add(start);
        list.Add(new TestNode(1));

        Assert.That(list.Find(start, new TestNode(example), direction, out TestNode result), Is.False);
        Assert.That(result, Is.SameAs(start));
    }

    [TestCase(Direction.LeftToRight)]
    [TestCase(Direction.RightToLeft)]
    public void Find_WithStart_ReturnsFirstLaterTie(Direction direction)
    {
        var list = new TestList();
        var left = new TestNode(direction == Direction.LeftToRight ? 0 : 1);
        var middle = new TestNode(1);
        var right = new TestNode(direction == Direction.LeftToRight ? 1 : 2);
        list.Add(left);
        list.Add(middle);
        list.Add(right);
        TestNode start = direction == Direction.LeftToRight ? left : right;

        Assert.That(list.Find(start, new TestNode(1), direction, out TestNode result), Is.True);
        Assert.That(result, Is.SameAs(middle));
    }

    private sealed class TestNode(int value) : BidirListNode<TestNode>
    {
        public int Value { get; } = value;
    }

    private sealed class TestList : BidirList<TestNode>
    {
        public TestList()
            : base(
                Comparer<TestNode>.Create((x, y) => x.Value.CompareTo(y.Value)),
                begin => new TestNode(begin ? int.MinValue : int.MaxValue)
            )
        {
            typeof(BidirList<TestNode>)
                .GetField("_rand", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(this, new LevelRandom());
        }
    }

    private sealed class LevelRandom : Random
    {
        private readonly Queue<int> _values = new(new[] { 0, 1, 0 });

        public override int Next() => _values.Dequeue();
    }
}
