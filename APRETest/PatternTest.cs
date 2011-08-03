using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SIL.APRE.Test
{
	[TestFixture]
	public class PatternTest
	{
		static AnnotationList<int> CreateShapeAnnotations(string str, SpanFactory<int> spanFactory, FeatureSystem featSys)
		{
			var annList = new AnnotationList<int>();
			for (int i = 0; i < str.Length; i++)
			{
				string annType = "Seg";
				switch (str[i])
				{
					case ',':
					case ' ':
					case '.':
						annType = "Bdry";
						break;
				}
				annList.Add(new Annotation<int>(annType, spanFactory.Create(i, i + 1),
					featSys.CreateFeatureStructure(new FeatValPair("str", str[i].ToString()))));
			}
			return annList;
		}

		[Test]
		public void PatternMatch()
		{
			var spanFactory = new SpanFactory<int>((x, y) => x.CompareTo(y), (start, end) => end - start);
			var featSys = new FeatureSystem();

			var pattern = new Pattern<int>(spanFactory, new[] { "Noun", "Verb", "NP", "VP", "Det", "Adj", "Adv" },
				new Alternation<int>(
					new Constraints<int>("Det"),
					new Group<int>(
						new Group<int>("adj", new Constraints<int>("Adj")),
						new Group<int>("noun", new Constraints<int>("Noun")),
						new Group<int>("verb", new Constraints<int>("Verb")),
						new Group<int>("range", new Quantifier<int>(0, 1,
							new Group<int>("adv", new Constraints<int>("Adv")))))));

			pattern.Compile();

			//FiniteStateAutomaton<int, FeatureStructure> fsa = pattern.GetFsa(Direction.LeftToRight);
			//var writer = new StreamWriter("c:\\ltor-dfa.dot");
			//fsa.ToGraphViz(writer);
			//writer.Close();

			//fsa = pattern.GetFsa(Direction.RightToLeft);
			//writer = new StreamWriter("c:\\rtol-fsa.dot");
			//fsa.ToGraphViz(writer);
			//writer.Close();

			AnnotationList<int> annList = CreateShapeAnnotations("the old, angry man slept well", spanFactory, featSys);
			annList.Add(new Annotation<int>("Det", spanFactory.Create(0, 2)) { IsOptional = true });
			annList.Add(new Annotation<int>("Adj", spanFactory.Create(4, 6)));
			annList.Add(new Annotation<int>("Adj", spanFactory.Create(9, 13)) { IsOptional = true });
			annList.Add(new Annotation<int>("Noun", spanFactory.Create(15, 17)));
			annList.Add(new Annotation<int>("Verb", spanFactory.Create(19, 23)));
			annList.Add(new Annotation<int>("Adv", spanFactory.Create(25, 28)));
			annList.Add(new Annotation<int>("NP", spanFactory.Create(0, 17)));
			annList.Add(new Annotation<int>("VP", spanFactory.Create(19, 28)));

			IEnumerable<PatternMatch<int>> matches;
			Assert.True(pattern.IsMatch(annList, Direction.LeftToRight, ModeType.Synthesis, out matches));
			Assert.AreEqual(7, matches.Count());
			Assert.AreEqual(0, matches.First().Start);
			Assert.AreEqual(28, matches.First().End);
			Assert.AreEqual(4, matches.First()["adj"].Start);
			Assert.AreEqual(6, matches.First()["adj"].End);
			Assert.AreEqual(9, matches.Last().Start);
			Assert.AreEqual(23, matches.Last().End);

			Assert.True(pattern.IsMatch(annList, Direction.RightToLeft, ModeType.Synthesis, out matches));
			Assert.AreEqual(7, matches.Count());
			Assert.AreEqual(0, matches.First().Start);
			Assert.AreEqual(28, matches.First().End);
			Assert.AreEqual(4, matches.First()["adj"].Start);
			Assert.AreEqual(6, matches.First()["adj"].End);
			Assert.AreEqual(0, matches.Last().Start);
			Assert.AreEqual(2, matches.Last().End);
		}

		private static AnnotationList<int> CreateFeatShapeAnnotations(string str, SpanFactory<int> spanFactory, FeatureSystem featSys)
		{
			var annList = new AnnotationList<int>();
			for (int i = 0; i < str.Length; i++)
			{
				FeatureStructure fs = null;
				string annType = "Seg";
				switch (str[i])
				{
					case 'f':
						fs = featSys.CreateFeatureStructure("cons+", "voice-", "sib-", "cor-", "lab+", "low-", new FeatValPair("str", "f"));
						break;
					case 'k':
						fs = featSys.CreateFeatureStructure("cons+", "voice-", "sib-", "cor-", "lab-", "low-", new FeatValPair("str", "k"));
						break;
					case 'z':
						fs = featSys.CreateFeatureStructure("cons+", "voice+", "sib+", "cor+", "lab-", "low-", new FeatValPair("str", "z"));
						break;
					case 's':
						fs = featSys.CreateFeatureStructure("cons+", "voice-", "sib+", "cor+", "lab-", "low-", new FeatValPair("str", "s"));
						break;
					case 'a':
						fs = featSys.CreateFeatureStructure("cons-", "voice+", "sib-", "cor-", "lab-", "low+", new FeatValPair("str", "a"));
						break;
					case 'ɨ':
						fs = featSys.CreateFeatureStructure("cons-", "voice+", "sib-", "cor-", "lab-", "low-", new FeatValPair("str", "ɨ"));
						break;
					case '+':
						annType = "Bdry";
						fs = featSys.CreateFeatureStructure(new FeatValPair("str", "+"));
						break;
				}
				annList.Add(new Annotation<int>(annType, spanFactory.Create(i, i + 1), fs));
			}
			return annList;
		}

		private static FeatureSystem CreateFeatureSystem()
		{
			var featSys = new FeatureSystem();
			var symf = new SymbolicFeature("cons");
			symf.AddPossibleSymbol(new FeatureSymbol("cons+", "+"));
			symf.AddPossibleSymbol(new FeatureSymbol("cons-", "-"));
			featSys.AddFeature(symf);
			symf = new SymbolicFeature("voice");
			symf.AddPossibleSymbol(new FeatureSymbol("voice+", "+"));
			symf.AddPossibleSymbol(new FeatureSymbol("voice-", "-"));
			featSys.AddFeature(symf);
			symf = new SymbolicFeature("sib");
			symf.AddPossibleSymbol(new FeatureSymbol("sib+", "+"));
			symf.AddPossibleSymbol(new FeatureSymbol("sib-", "-"));
			featSys.AddFeature(symf);
			symf = new SymbolicFeature("cor");
			symf.AddPossibleSymbol(new FeatureSymbol("cor+", "+"));
			symf.AddPossibleSymbol(new FeatureSymbol("cor-", "-"));
			featSys.AddFeature(symf);
			symf = new SymbolicFeature("lab");
			symf.AddPossibleSymbol(new FeatureSymbol("lab+", "+"));
			symf.AddPossibleSymbol(new FeatureSymbol("lab-", "-"));
			featSys.AddFeature(symf);
			symf = new SymbolicFeature("low");
			symf.AddPossibleSymbol(new FeatureSymbol("low+", "+"));
			symf.AddPossibleSymbol(new FeatureSymbol("low-", "-"));
			featSys.AddFeature(symf);
			featSys.AddFeature(new StringFeature("str"));

			return featSys;
		}

		[Test]
		public void Variables()
		{
			var spanFactory = new SpanFactory<int>((x, y) => x.CompareTo(y), (start, end) => end - start);
			FeatureSystem featSys = CreateFeatureSystem();
			Feature voice = featSys.GetFeature("voice");

			var pattern = new Pattern<int>(spanFactory, null, null, false, false, new Dictionary<string, IEnumerable<Feature>> { { "a", new[] { voice } } },
				new Group<int>("leftEnv",
					new Constraints<int>("Seg", featSys.CreateFeatureStructure("cons+"), new Dictionary<string, bool> { { "a", true } }),
				new Group<int>("lhs",
					new Constraints<int>("Seg", featSys.CreateFeatureStructure(new FeatValPair("str", "a")))),
				new Group<int>("rightEnv",
					new Constraints<int>("Seg", featSys.CreateFeatureStructure("cons+"), new Dictionary<string, bool> { { "a", false } }))));
			pattern.Compile();

			AnnotationList<int> word = CreateFeatShapeAnnotations("fazk", spanFactory, featSys);
			Assert.IsTrue(pattern.IsMatch(word, Direction.LeftToRight, ModeType.Synthesis));
		}
	}
}
