using System.Linq;
using NUnit.Framework;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Test
{
	[TestFixture]
	public class AnnotationTest
	{
		private SpanFactory<int> _spanFactory;

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			_spanFactory = new IntegerSpanFactory();
		}

		[Test]
		public void Add()
		{
			var annList = new AnnotationList<int>(_spanFactory);
			// add without subsumption
			// add to empty list
			var a = new Annotation<int>(_spanFactory.Create(49, 50), FeatureStruct.New().Value);
			annList.Add(a, false);
			Assert.AreEqual(1, annList.Count);
			Assert.AreSame(a, annList.First);
			// add to beginning of list
			a = new Annotation<int>(_spanFactory.Create(0, 1), FeatureStruct.New().Value);
			annList.Add(a, false);
			Assert.AreEqual(2, annList.Count);
			Assert.AreSame(a, annList.First);
			// add to end of list
			a = new Annotation<int>(_spanFactory.Create(99, 100), FeatureStruct.New().Value);
			annList.Add(a, false);
			Assert.AreEqual(3, annList.Count);
			Assert.AreSame(a, annList.Last);
			// add to middle of list
			a = new Annotation<int>(_spanFactory.Create(24, 25), FeatureStruct.New().Value);
			annList.Add(a, false);
			Assert.AreEqual(4, annList.Count);
			Assert.AreSame(a, annList.ElementAt(1));
			// add containing annotation
			a = new Annotation<int>(_spanFactory.Create(0, 100), FeatureStruct.New().Value);
			annList.Add(a, false);
			Assert.AreEqual(5, annList.Count);
			Assert.AreSame(a, annList.First());
			// add contained annotation
			a = new Annotation<int>(_spanFactory.Create(9, 10), FeatureStruct.New().Value);
			annList.Add(a, false);
			Assert.AreEqual(6, annList.Count);
			Assert.AreSame(a, annList.ElementAt(2));

			annList.Clear();

			// add with subsumption
			// add to empty list
			a = new Annotation<int>(_spanFactory.Create(49, 50), FeatureStruct.New().Value);
			annList.Add(a);
			Assert.AreEqual(1, annList.Count);
			Assert.AreSame(a, annList.First);
			// add to beginning of list
			a = new Annotation<int>(_spanFactory.Create(0, 1), FeatureStruct.New().Value);
			annList.Add(a);
			Assert.AreEqual(2, annList.Count);
			Assert.AreSame(a, annList.First);
			// add to end of list
			a = new Annotation<int>(_spanFactory.Create(99, 100), FeatureStruct.New().Value);
			annList.Add(a);
			Assert.AreEqual(3, annList.Count);
			Assert.AreSame(a, annList.Last);
			// add to middle of list
			a = new Annotation<int>(_spanFactory.Create(24, 25), FeatureStruct.New().Value);
			annList.Add(a);
			Assert.AreEqual(4, annList.Count);
			Assert.AreSame(a, annList.ElementAt(1));
			// add containing annotation
			a = new Annotation<int>(_spanFactory.Create(0, 100), FeatureStruct.New().Value);
			annList.Add(a);
			Assert.AreEqual(1, annList.Count);
			Assert.AreSame(a, annList.First());
			Assert.AreEqual(4, a.Children.Count);
			// add contained annotation
			a = new Annotation<int>(_spanFactory.Create(9, 10), FeatureStruct.New().Value);
			annList.Add(a);
			Assert.AreEqual(1, annList.Count);
			Assert.AreEqual(5, annList.First.Children.Count);
			Assert.AreSame(a, annList.First.Children.ElementAt(1));

			annList.Clear();

			annList.Add(0, 1, FeatureStruct.New().Value);
			annList.Add(1, 2, FeatureStruct.New().Value);
			annList.Add(2, 3, FeatureStruct.New().Value);
			annList.Add(3, 4, FeatureStruct.New().Value);
			annList.Add(4, 5, FeatureStruct.New().Value);
			annList.Add(5, 6, FeatureStruct.New().Value);
			Assert.AreEqual(6, annList.Count);
			a = new Annotation<int>(_spanFactory.Create(1, 5), FeatureStruct.New().Value);
			a.Children.Add(1, 3, FeatureStruct.New().Value);
			a.Children.Add(3, 5, FeatureStruct.New().Value);
			Assert.AreEqual(2, a.Children.Count);
			annList.Add(a);
			Assert.AreEqual(3, annList.Count);
			Assert.AreSame(a, annList.ElementAt(1));
			Assert.AreEqual(2, a.Children.Count);
			Assert.AreEqual(2, a.Children.First.Children.Count);
			Assert.AreEqual(2, a.Children.Last.Children.Count);
		}

		[Test]
		public void Remove()
		{
			var annList = new AnnotationList<int>(_spanFactory);
			annList.Add(0, 1, FeatureStruct.New().Value);
			annList.Add(9, 10, FeatureStruct.New().Value);
			annList.Add(24, 25, FeatureStruct.New().Value);
			annList.Add(49, 50, FeatureStruct.New().Value);
			annList.Add(99, 100, FeatureStruct.New().Value);

			annList.Remove(annList.First);
			Assert.AreEqual(4, annList.Count);
			Assert.AreEqual(_spanFactory.Create(9, 10), annList.First.Span);

			annList.Remove(annList.Last);
			Assert.AreEqual(3, annList.Count);
			Assert.AreEqual(_spanFactory.Create(49, 50), annList.Last.Span);

			annList.Remove(annList.First.Next);
			Assert.AreEqual(2, annList.Count);
			annList.Remove(annList.First);
			Assert.AreEqual(1, annList.Count);
			annList.Remove(annList.First);
			Assert.AreEqual(0, annList.Count);

			annList.Add(0, 1, FeatureStruct.New().Value);
			annList.Add(9, 10, FeatureStruct.New().Value);
			annList.Add(49, 50, FeatureStruct.New().Value);
			annList.Add(69, 70, FeatureStruct.New().Value);
			annList.Add(99, 100, FeatureStruct.New().Value);
			annList.Add(0, 49, FeatureStruct.New().Value);
			annList.Add(51, 100, FeatureStruct.New().Value);

			annList.Remove(annList.First);
			Assert.AreEqual(4, annList.Count);
			annList.Remove(annList.Last, false);
			Assert.AreEqual(3, annList.Count);
		}

		[Test]
		public void Find()
		{
			var annList = new AnnotationList<int>(_spanFactory);
			annList.Add(1, 2, FeatureStruct.New().Value);
			annList.Add(9, 10, FeatureStruct.New().Value);
			annList.Add(24, 25, FeatureStruct.New().Value);
			annList.Add(49, 50, FeatureStruct.New().Value);
			annList.Add(99, 100, FeatureStruct.New().Value);
			annList.Add(new Annotation<int>(_spanFactory.Create(20, 70), FeatureStruct.New().Value), false);

			Annotation<int> result;
			Assert.IsFalse(annList.Find(0, out result));
			Assert.AreSame(annList.Begin, result);

			Assert.IsTrue(annList.Find(1, out result));
			Assert.AreSame(annList.First, result);

			Assert.IsFalse(annList.Find(100, out result));
			Assert.AreSame(annList.Last, result);

			Assert.IsFalse(annList.Find(30, out result));
			Assert.AreSame(annList.ElementAt(3), result);

			Assert.IsTrue(annList.Find(9, out result));
			Assert.AreSame(annList.First.Next, result);

			Assert.IsFalse(annList.Find(101, Direction.RightToLeft, out result));
			Assert.AreSame(annList.End, result);

			Assert.IsTrue(annList.Find(100, Direction.RightToLeft, out result));
			Assert.AreSame(annList.Last, result);

			Assert.IsFalse(annList.Find(0, Direction.RightToLeft, out result));
			Assert.AreSame(annList.First, result);

			Assert.IsFalse(annList.Find(15, Direction.RightToLeft, out result));
			Assert.AreSame(annList.ElementAt(3), result);

			Assert.IsTrue(annList.Find(10, Direction.RightToLeft, out result));
			Assert.AreSame(annList.First.Next, result);
		}

		[Test]
		public void GetNodes()
		{
			var annList = new AnnotationList<int>(_spanFactory);
			annList.Add(1, 2, FeatureStruct.New().Value);
			annList.Add(9, 10, FeatureStruct.New().Value);
			annList.Add(24, 25, FeatureStruct.New().Value);
			annList.Add(49, 50, FeatureStruct.New().Value);
			annList.Add(99, 100, FeatureStruct.New().Value);
			annList.Add(new Annotation<int>(_spanFactory.Create(20, 70), FeatureStruct.New().Value), false);

			Assert.IsFalse(annList.GetNodes(0, 1).Any());

			Assert.IsFalse(annList.GetNodes(100, 101).Any());

			Annotation<int>[] anns = annList.GetNodes(8, 52).ToArray();
			Assert.AreEqual(3, anns.Length);
			Assert.AreEqual(annList.First.Next, anns[0]);
			Assert.AreEqual(annList.Last.Prev, anns[2]);

			anns = annList.GetNodes(9, 10).ToArray();
			Assert.AreEqual(1, anns.Length);
			Assert.AreEqual(annList.First.Next, anns[0]);

			anns = annList.GetNodes(0, 200).ToArray();
			Assert.AreEqual(6, anns.Length);
		}

		[Test]
		public void FindDepthFirst()
		{
			var annList = new AnnotationList<int>(_spanFactory);
			annList.Add(1, 2, FeatureStruct.New().Value);
			annList.Add(9, 10, FeatureStruct.New().Value);
			annList.Add(49, 50, FeatureStruct.New().Value);
			annList.Add(69, 70, FeatureStruct.New().Value);
			annList.Add(99, 100, FeatureStruct.New().Value);
			annList.Add(1, 49, FeatureStruct.New().Value);
			annList.Add(51, 100, FeatureStruct.New().Value);

			Annotation<int> result;
			Assert.IsFalse(annList.FindDepthFirst(0, out result));
			Assert.AreEqual(annList.Begin, result);

			Assert.IsFalse(annList.FindDepthFirst(100, out result));
			Assert.AreEqual(annList.Last.Children.Last, result);

			Assert.IsTrue(annList.FindDepthFirst(1, out result));
			Assert.AreEqual(annList.First, result);

			Assert.IsFalse(annList.FindDepthFirst(8, out result));
			Assert.AreEqual(annList.First.Children.First, result);

			Assert.IsTrue(annList.FindDepthFirst(99, out result));
			Assert.AreEqual(annList.Last.Children.Last, result);

			Assert.IsTrue(annList.FindDepthFirst(49, out result));
			Assert.AreEqual(annList.First.Next, result);

			Assert.IsFalse(annList.FindDepthFirst(101, Direction.RightToLeft, out result));
			Assert.AreEqual(annList.End, result);

			Assert.IsFalse(annList.FindDepthFirst(1, Direction.RightToLeft, out result));
			Assert.AreEqual(annList.First.Children.First, result);

			Assert.IsTrue(annList.FindDepthFirst(100, Direction.RightToLeft, out result));
			Assert.AreEqual(annList.Last, result);

			Assert.IsFalse(annList.FindDepthFirst(71, Direction.RightToLeft, out result));
			Assert.AreEqual(annList.Last.Children.Last, result);

			Assert.IsTrue(annList.FindDepthFirst(2, Direction.RightToLeft, out result));
			Assert.AreEqual(annList.First.Children.First, result);

			Assert.IsTrue(annList.FindDepthFirst(50, Direction.RightToLeft, out result));
			Assert.AreEqual(annList.Last.Prev, result);
		}
	}
}
