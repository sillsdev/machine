using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.Collections;
using SIL.Machine.FeatureModel;
using SIL.Machine.Fsa;

namespace SIL.Machine.Test
{
	[TestFixture]
	public class FiniteStateTransducerTest : PhoneticTestBase
	{

		[Test]
		public void Determinize()
		{
			var featSys = new FeatureSystem
				{
					new StringFeature("A"),
					new StringFeature("B"),
					new StringFeature("C"),
					new StringFeature("D"),
					new StringFeature("E"),
					new StringFeature("F")
				};

			var fst = new FiniteStateTransducer<StringData, int>();
			fst.StartState = fst.CreateState();
			State<StringData, int, FstResult<StringData, int>> s1 = fst.StartState.Arcs.Add(FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("E").EqualTo("true").Value, fst.CreateState());
			State<StringData, int, FstResult<StringData, int>> s2 = fst.StartState.Arcs.Add(FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("F").EqualTo("true").Value, fst.CreateState());
			State<StringData, int, FstResult<StringData, int>> s3 = s1.Arcs.Add(FeatureStruct.New(featSys).Feature("B").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("D").EqualTo("true").Value, fst.CreateState());
			s2.Arcs.Add(FeatureStruct.New(featSys).Feature("C").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("D").EqualTo("true").Value, s3);
			State<StringData, int, FstResult<StringData, int>> s4 = s3.Arcs.Add(FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value, fst.CreateAcceptingState());
			s4.Arcs.Add(FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value, s4);

			var writer = new System.IO.StreamWriter(string.Format("c:\\ltor-nfst.dot"));
			fst.ToGraphViz(writer);
			writer.Close();

			fst.Determinize();

			writer = new System.IO.StreamWriter(string.Format("c:\\ltor-dfst.dot"));
			fst.ToGraphViz(writer);
			writer.Close();

		}

		[Test]
		public void Compose()
		{
			var featSys = new FeatureSystem
				{
					new StringFeature("value")
				};

			var fst1 = new FiniteStateTransducer<StringData, int>();
			fst1.StartState = fst1.CreateState();
			State<StringData, int, FstResult<StringData, int>> s1 = fst1.StartState.Arcs.Add(FeatureStruct.New(featSys).Feature("value").EqualTo("a").Value, FeatureStruct.New(featSys).Feature("value").EqualTo("x").Value, fst1.CreateAcceptingState());
			s1.Arcs.Add(FeatureStruct.New(featSys).Feature("value").EqualTo("b").Value, FeatureStruct.New(featSys).Feature("value").EqualTo("y").Value, s1);

			var fst2 = new FiniteStateTransducer<StringData, int>();
			fst2.StartState = fst2.CreateAcceptingState();
			fst2.StartState.Arcs.Add(FeatureStruct.New(featSys).Feature("value").EqualTo("x").Value, fst2.StartState);
			fst2.StartState.Arcs.Add(FeatureStruct.New(featSys).Feature("value").EqualTo("y").Value, FeatureStruct.New(featSys).Feature("value").EqualTo("z").Value, fst2.StartState);

			FiniteStateTransducer<StringData, int> composedFsa = fst1.Compose(fst2);
			var writer = new System.IO.StreamWriter(string.Format("c:\\ltor-composed-nfst.dot"));
			composedFsa.ToGraphViz(writer);
			writer.Close();
		}

		[Test]
		public void Identity()
		{
			var featSys = new FeatureSystem
				{
					new StringFeature("A"),
					new StringFeature("B"),
					new StringFeature("C"),
					new StringFeature("D"),
					new StringFeature("E"),
					new StringFeature("F")
				};

			var fst = new FiniteStateTransducer<StringData, int>();
			fst.StartState = fst.CreateState();
			State<StringData, int, FstResult<StringData, int>> s1 = fst.StartState.Arcs.Add(FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("C").EqualTo("true").Value, fst.CreateState());
			State<StringData, int, FstResult<StringData, int>> sa = s1.Arcs.Add(FeatureStruct.New(featSys).Feature("D").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("E").EqualTo("true").Value, fst.CreateAcceptingState());

			State<StringData, int, FstResult<StringData, int>> s2 = fst.StartState.Arcs.Add(FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("B").EqualTo("true").Value, fst.CreateState());
			State<StringData, int, FstResult<StringData, int>> s3 = s2.Arcs.Add(FeatureStruct.New(featSys).Value, FeatureStruct.New(featSys).Value, true, fst.CreateState());
			s3.Arcs.Add(FeatureStruct.New(featSys).Feature("B").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("B").EqualTo("true").Value, sa);

			var writer = new System.IO.StreamWriter(string.Format("c:\\ltor-nfst.dot"));
			fst.ToGraphViz(writer);
			writer.Close();

			fst.Determinize();

			writer = new System.IO.StreamWriter(string.Format("c:\\ltor-dfst.dot"));
			fst.ToGraphViz(writer);
			writer.Close();
		}

		[Test]
		public void Transduce()
		{

			var fst = new FiniteStateTransducer<StringData, int>();
			fst.StartState = fst.CreateAcceptingState();
			fst.StartState.Arcs.Add(FeatureStruct.New().Value, FeatureStruct.New().Value, true, fst.StartState);
			State<StringData, int, FstResult<StringData, int>> s1 = fst.StartState.Arcs.Add(FeatureStruct.New(PhoneticFeatSys).Symbol("nas+").Value, FeatureStruct.New(PhoneticFeatSys).Symbol("cor-").Value, true, fst.CreateState());
			State<StringData, int, FstResult<StringData, int>> s2 = s1.Arcs.Add(FeatureStruct.New(PhoneticFeatSys).Symbol("cor-").Value, FeatureStruct.New().Value, true, fst.CreateAcceptingState());
			s2.Arcs.Add(FeatureStruct.New().Value, FeatureStruct.New().Value, true, s2);

			var writer = new System.IO.StreamWriter(string.Format("c:\\ltor-nfst.dot"));
			fst.ToGraphViz(writer);
			writer.Close();

			fst.Determinize();

			writer = new System.IO.StreamWriter(string.Format("c:\\ltor-dfst.dot"));
			fst.ToGraphViz(writer);
			writer.Close();

			StringData data = CreateStringData("canp");
			IEnumerable<FstResult<StringData, int>> results;
			Assert.That(fst.Transduce(data, data.Annotations.First, true, true, false, false, out results), Is.True);
			FstResult<StringData, int>[] resultsArray = results.ToArray();
			Assert.That(resultsArray.Length, Is.EqualTo(1));
			Assert.That(resultsArray[0].Output.Annotations, Is.EquivalentTo(CreateStringData("camp").Annotations).Using((IEqualityComparer<Annotation<int>>) FreezableEqualityComparer<Annotation<int>>.Instance));

			data = CreateStringData("can");
			Assert.That(fst.Transduce(data, data.Annotations.First, true, true, false, false, out results), Is.True);
			resultsArray = results.ToArray();
			Assert.That(resultsArray.Length, Is.EqualTo(1));
			Assert.That(resultsArray[0].Output.Annotations, Is.EquivalentTo(CreateStringData("can").Annotations).Using((IEqualityComparer<Annotation<int>>) FreezableEqualityComparer<Annotation<int>>.Instance));

			data = CreateStringData("carp");
			Assert.That(fst.Transduce(data, data.Annotations.First, true, true, false, false, out results), Is.True);
			resultsArray = results.ToArray();
			Assert.That(resultsArray.Length, Is.EqualTo(1));
			Assert.That(resultsArray[0].Output.Annotations, Is.EquivalentTo(CreateStringData("carp").Annotations).Using((IEqualityComparer<Annotation<int>>) FreezableEqualityComparer<Annotation<int>>.Instance));
		}
	}
}
