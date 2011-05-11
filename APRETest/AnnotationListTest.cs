using NUnit.Framework;
using System.Linq;

namespace SIL.APRE.Test
{
	[TestFixture]
	public class AnnotationListTest
	{
		private SpanFactory<Atom> _spanFactory;
		private AtomList _atomList;

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			_spanFactory = new SpanFactory<Atom>((x, y) => x.CompareTo(y), (start, end) => start.GetNodes(end, Direction.LeftToRight).Count());
			_atomList = new AtomList();
			for (int i = 0; i < 10; i++)
				_atomList.Add(new Atom(i));
		}

		[SetUp]
		public void SetUp()
		{
			_atomList.Annotations.Clear();
		}

		[Test]
		public void AddAnnotations()
		{
			var a = new Annotation<Atom>("Last", _spanFactory.Create(_atomList.Last), null);
			_atomList.Annotations.Add(a);
			Assert.AreSame(a, _atomList.Annotations.First);
			a = new Annotation<Atom>("First", _spanFactory.Create(_atomList.First), null);
			_atomList.Annotations.Add(a);
			Assert.AreSame(a, _atomList.Annotations.First);
			a = new Annotation<Atom>("Entire", _spanFactory.Create(_atomList.First, _atomList.Last), null);
			_atomList.Annotations.Add(a);
			Assert.AreSame(a, _atomList.Annotations.ElementAt(1));
		}

		/*
		[Test]
		public void GetSubset()
		{
			foreach (Atom atom in _atomList)
				_atomList.Annotations.Add(new Annotation<Atom>("Short", _spanFactory.Create(atom), null));
			_atomList.Annotations.Add(new Annotation<Atom>("Mid", _spanFactory.Create(_atomList.First, _atomList.ElementAt(2)), null));
			_atomList.Annotations.Add(new Annotation<Atom>("Mid", _spanFactory.Create(_atomList.ElementAt(3), _atomList.ElementAt(6)), null));
			_atomList.Annotations.Add(new Annotation<Atom>("Mid", _spanFactory.Create(_atomList.ElementAt(7), _atomList.Last), null));
			_atomList.Annotations.Add(new Annotation<Atom>("Long", _spanFactory.Create(_atomList.First, _atomList.Last), null));

			BidirListView subset = _atomList.Annotations.GetSubset(_atomList.First, _atomList.ElementAt(3));
			Assert.AreEqual(5, subset.Count);
			subset = _atomList.Annotations.GetSubset(_atomList.ElementAt(6), _atomList.Last);
			Assert.AreEqual(5, subset.Count);
			subset = _atomList.Annotations.GetSubset(_atomList.ElementAt(3), _atomList.ElementAt(3));
			Assert.AreEqual(1, subset.Count);

			subset = _atomList.Annotations.GetSubset(_atomList.ElementAt(1), _atomList.ElementAt(8), "Mid");
			Assert.AreEqual(1, subset.Count);
			subset = _atomList.Annotations.GetSubset(_atomList.ElementAt(1), _atomList.ElementAt(8), "Short");
			Assert.AreEqual(8, subset.Count);
			subset = _atomList.Annotations.GetSubset(_atomList.ElementAt(1), _atomList.ElementAt(8), "Long");
			Assert.AreEqual(0, subset.Count);
		}

		[Test]
		public void GetNextNonOverlapping()
		{
			foreach (Atom atom in _atomList)
				_atomList.Annotations.Add(new Annotation("Short", atom, atom, null));
			_atomList.Annotations.Add(new Annotation("Mid", _atomList.First, _atomList.ElementAt(2), null));
			_atomList.Annotations.Add(new Annotation("Mid", _atomList.ElementAt(3), _atomList.ElementAt(6), null));
			_atomList.Annotations.Add(new Annotation("Mid", _atomList.ElementAt(7), _atomList.Last, null));
			_atomList.Annotations.Add(new Annotation("Long", _atomList.First, _atomList.Last, null));

			for (Annotation ann = _atomList.Annotations.First; ann != null; ann = ann.NextNonOverlapping)
				Assert.AreEqual("Short", ann.Type);
			Assert.IsNull(_atomList.Annotations.ElementAt(2).NextNonOverlapping);
		}
		 */
	}
}
