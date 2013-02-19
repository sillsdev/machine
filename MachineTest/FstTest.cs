using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.Machine.FeatureModel;
using SIL.Machine.FiniteState;

namespace SIL.Machine.Test
{
	[TestFixture]
	public class FstTest : PhoneticTestBase
	{
		private PhoneticFstOperations _operations;

		public override void FixtureSetUp()
		{
			base.FixtureSetUp();
			_operations = new PhoneticFstOperations(SpanFactory, Characters);
		}


		[Test]
		public void IsDeterminizable()
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

			var fst = new Fst<StringData, int>(_operations);
			fst.StartState = fst.CreateState();
			State<StringData, int> s1 = fst.StartState.Arcs.Add(FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("E").EqualTo("true").Value, fst.CreateState());
			State<StringData, int> s2 = fst.StartState.Arcs.Add(FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("F").EqualTo("true").Value, fst.CreateState());
			State<StringData, int> s3 = s1.Arcs.Add(FeatureStruct.New(featSys).Feature("B").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("D").EqualTo("true").Value, fst.CreateState());
			s2.Arcs.Add(FeatureStruct.New(featSys).Feature("C").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("D").EqualTo("true").Value, s3);
			State<StringData, int> s4 = s3.Arcs.Add(FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value, fst.CreateAcceptingState());
			Assert.That(fst.IsDeterminizable, Is.True);

			s4.Arcs.Add(FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value, s4);

			var writer = new System.IO.StreamWriter(string.Format("c:\\ltor-nfst.dot"));
			fst.ToGraphViz(writer);
			writer.Close();

			Assert.That(fst.IsDeterminizable, Is.False);
		}

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

			var nfst = new Fst<StringData, int>(_operations);
			nfst.StartState = nfst.CreateState();
			State<StringData, int> s1 = nfst.StartState.Arcs.Add(FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("C").EqualTo("true").Value, nfst.CreateState());
			State<StringData, int> sa = s1.Arcs.Add(FeatureStruct.New(featSys).Feature("D").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("E").EqualTo("true").Value, nfst.CreateAcceptingState());

			State<StringData, int> s2 = nfst.StartState.Arcs.Add(FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("B").EqualTo("true").Value, nfst.CreateState());
			State<StringData, int> s3 = s2.Arcs.Add(FeatureStruct.New(featSys).Value, FeatureStruct.New(featSys).Value, nfst.CreateState());
			s3.Arcs.Add(FeatureStruct.New(featSys).Feature("B").EqualTo("true").Value, FeatureStruct.New(featSys).Feature("B").EqualTo("true").Value, sa);

			var writer = new System.IO.StreamWriter(string.Format("c:\\ltor-nfst.dot"));
			nfst.ToGraphViz(writer);
			writer.Close();

			Fst<StringData, int> dfst;
			Assert.That(nfst.TryDeterminize(out dfst), Is.True);

			writer = new System.IO.StreamWriter(string.Format("c:\\ltor-dfst.dot"));
			dfst.ToGraphViz(writer);
			writer.Close();
		}

		[Test]
		public void Compose()
		{
			var featSys = new FeatureSystem
				{
					new StringFeature("value")
				};

			var fst1 = new Fst<StringData, int>(_operations);
			fst1.StartState = fst1.CreateState();
			State<StringData, int> s1 = fst1.StartState.Arcs.Add(FeatureStruct.New(featSys).Feature("value").EqualTo("a").Value, FeatureStruct.New(featSys).Feature("value").EqualTo("x").Value, fst1.CreateAcceptingState());
			s1.Arcs.Add(FeatureStruct.New(featSys).Feature("value").EqualTo("b").Value, FeatureStruct.New(featSys).Feature("value").EqualTo("y").Value, s1);

			var fst2 = new Fst<StringData, int>(_operations);
			fst2.StartState = fst2.CreateAcceptingState();
			fst2.StartState.Arcs.Add(FeatureStruct.New(featSys).Feature("value").EqualTo("x").Value, null, fst2.StartState);
			fst2.StartState.Arcs.Add(FeatureStruct.New(featSys).Feature("value").EqualTo("y").Value, FeatureStruct.New(featSys).Feature("value").EqualTo("z").Value, fst2.StartState);

			Fst<StringData, int> composedFsa = fst1.Compose(fst2);
			var writer = new System.IO.StreamWriter(string.Format("c:\\ltor-composed-nfst.dot"));
			composedFsa.ToGraphViz(writer);
			writer.Close();
		}

		[Test]
		public void Transduce()
		{

			var fst = new Fst<StringData, int>(_operations);
			fst.StartState = fst.CreateAcceptingState();
			fst.StartState.Arcs.Add(FeatureStruct.New(PhoneticFeatSys).Symbol("nas-", "nas?").Value, fst.StartState);
			fst.StartState.Arcs.Add(FeatureStruct.New(PhoneticFeatSys).Symbol("nas+").Symbol("cor+", "cor-").Value, fst.StartState);
			State<StringData, int> s1 = fst.StartState.Arcs.Add(FeatureStruct.New(PhoneticFeatSys).Symbol("cor?").Symbol("nas+").Value, FeatureStruct.New(PhoneticFeatSys).Symbol("cor-").Value, fst.CreateState());
			s1.Arcs.Add(FeatureStruct.New(PhoneticFeatSys).Symbol("cor-").Value, fst.StartState);
			State<StringData, int> s2 = fst.StartState.Arcs.Add(FeatureStruct.New(PhoneticFeatSys).Symbol("cor?").Symbol("nas+").Value, FeatureStruct.New(PhoneticFeatSys).Symbol("cor+").Value, fst.CreateAcceptingState());
			s2.Arcs.Add(FeatureStruct.New(PhoneticFeatSys).Symbol("cor?").Symbol("nas+").Value, FeatureStruct.New(PhoneticFeatSys).Symbol("cor+").Value, s2);
			s2.Arcs.Add(FeatureStruct.New(PhoneticFeatSys).Symbol("nas-", "nas?").Symbol("cor+", "cor?").Value, fst.StartState);
			s2.Arcs.Add(FeatureStruct.New(PhoneticFeatSys).Symbol("nas+").Symbol("cor+").Value, fst.StartState);
			s2.Arcs.Add(FeatureStruct.New(PhoneticFeatSys).Symbol("cor?").Symbol("nas+").Value, FeatureStruct.New(PhoneticFeatSys).Symbol("cor-").Value, s1);

			var writer = new System.IO.StreamWriter(string.Format("c:\\ltor-nfst.dot"));
			fst.ToGraphViz(writer);
			writer.Close();

			Fst<StringData, int> dfst = fst.Determinize();

			writer = new System.IO.StreamWriter(string.Format("c:\\ltor-dfst.dot"));
			fst.ToGraphViz(writer);
			writer.Close();

			StringData data = CreateStringData("caNp");
			FstResult<StringData, int> result;
			Assert.That(dfst.Transduce(data, data.Annotations.First, true, true, false, true, out result), Is.True);
			Assert.That(result.Output.String, Is.EqualTo("camp"));

			data = CreateStringData("caN");
			Assert.That(dfst.Transduce(data, data.Annotations.First, true, true, false, true, out result), Is.True);
			Assert.That(result.Output.String, Is.EqualTo("can"));

			data = CreateStringData("carp");
			Assert.That(dfst.Transduce(data, data.Annotations.First, true, true, false, true, out result), Is.True);
			Assert.That(result.Output.String, Is.EqualTo("carp"));

			fst = new Fst<StringData, int>(_operations);
			fst.StartState = fst.CreateAcceptingState();
			s1 = fst.StartState.Arcs.Add(FeatureStruct.New(PhoneticFeatSys).Symbol("cons+").Value, fst.CreateState())
				.Arcs.Add(FeatureStruct.New(PhoneticFeatSys).Symbol("cons-").Value, fst.CreateState());
			s2 = s1.Arcs.Add(FeatureStruct.New(PhoneticFeatSys).Symbol("nas+").Value, null, fst.CreateState());
			State<StringData, int> s3 = s1.Arcs.Add(FeatureStruct.New(PhoneticFeatSys).Symbol("voice-").Value, fst.CreateState());
			s3.Arcs.Add(null, FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(".").Value, s2);
			s3.Arcs.Add(null, FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo("+").Value, fst.CreateState())
				.Arcs.Add(null, FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo("+").Value, s2);
			s2.Arcs.Add(FeatureStruct.New(PhoneticFeatSys).Symbol("cons+").Value, fst.CreateAcceptingState());


			writer = new System.IO.StreamWriter(string.Format("c:\\ltor-nfst.dot"));
			fst.ToGraphViz(writer);
			writer.Close();

			dfst = fst.Determinize();

			writer = new System.IO.StreamWriter(string.Format("c:\\ltor-dfst.dot"));
			dfst.ToGraphViz(writer);
			writer.Close();

			data = CreateStringData("camp");
			Assert.That(dfst.Transduce(data, data.Annotations.First, true, true, false, true, out result), Is.True);
			Assert.That(result.Output.String, Is.EqualTo("cap"));

			data = CreateStringData("casp");
			IEnumerable<FstResult<StringData, int>> results;
			Assert.That(dfst.Transduce(data, data.Annotations.First, true, true, false, true, out results), Is.True);
			FstResult<StringData, int>[] resultsArray = results.ToArray();
			Assert.That(resultsArray.Length, Is.EqualTo(2));
			Assert.That(resultsArray.Select(r => r.Output.String), Is.EquivalentTo(new [] {"cas++p", "cas.p"}));
		}
	}
}
