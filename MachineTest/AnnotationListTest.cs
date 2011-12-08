using System.Linq;
using NUnit.Framework;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Test
{
	[TestFixture]
	public class AnnotationListTest
	{
		private SpanFactory<int> _spanFactory;

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			_spanFactory = new IntegerSpanFactory();
		}

		[Test]
		public void AddAnnotations()
		{
			var annList = new AnnotationList<int>(_spanFactory);
			var a = new Annotation<int>("Last", _spanFactory.Create(99), new FeatureStruct());
			annList.Add(a);
			Assert.AreSame(a, annList.First);
			a = new Annotation<int>("First", _spanFactory.Create(0), new FeatureStruct());
			annList.Add(a);
			Assert.AreSame(a, annList.First);
			a = new Annotation<int>("Entire", _spanFactory.Create(0, 99), new FeatureStruct());
			annList.Add(a);
			Assert.AreSame(a, annList.First());
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
