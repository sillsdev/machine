using System.Collections.Generic;
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
			FeatureStructure fs = featSys.CreateFeatureStructure();

			var pattern = new Pattern<int>(spanFactory, new[] { "Noun", "Verb", "NP", "VP", "Det", "Adj", "Adv" },
				new CapturingGroup<int>(1, new AnnotationConstraints<int>("Adj", fs)),
				new CapturingGroup<int>(2, new AnnotationConstraints<int>("Noun", fs)),
				new CapturingGroup<int>(3, new AnnotationConstraints<int>("Verb", fs)),
				new CapturingGroup<int>(4, new RangeQuantifier<int>(0, 1,
					new CapturingGroup<int>(5, new AnnotationConstraints<int>("Adv", fs)))));

			pattern.Compile();

			//FiniteStateAutomaton<int, VariableValues<int>> fsa = pattern.GetFsa(Direction.LeftToRight);
			//var writer = new StreamWriter("c:\\ltor-fsa.dot");
			//fsa.ToGraphViz(writer);
			//writer.Close();

			//fsa = pattern.GetFsa(Direction.RightToLeft);
			//writer = new StreamWriter("c:\\rtol-fsa.dot");
			//fsa.ToGraphViz(writer);
			//writer.Close();

			AnnotationList<int> annList = CreateShapeAnnotations("the old, angry man slept well", spanFactory, featSys);
			annList.Add(new Annotation<int>("Det", spanFactory.Create(0, 2), fs) { IsOptional = true });
			annList.Add(new Annotation<int>("Adj", spanFactory.Create(4, 6), fs));
			annList.Add(new Annotation<int>("Adj", spanFactory.Create(9, 13), fs) { IsOptional = true });
			annList.Add(new Annotation<int>("Noun", spanFactory.Create(15, 17), fs));
			annList.Add(new Annotation<int>("Verb", spanFactory.Create(19, 23), fs));
			annList.Add(new Annotation<int>("Adv", spanFactory.Create(25, 28), fs));
			annList.Add(new Annotation<int>("NP", spanFactory.Create(0, 17), fs));
			annList.Add(new Annotation<int>("VP", spanFactory.Create(19, 28), fs));

			IList<PatternMatch<int>> matches;
			Assert.True(pattern.IsMatch(annList, Direction.LeftToRight, ModeType.Synthesis, out matches));
			Assert.AreEqual(6, matches.Count);
			Assert.AreEqual(0, matches[0].EntireMatch.Start);
			Assert.AreEqual(28, matches[0].EntireMatch.End);
			Assert.AreEqual(4, matches[0][1].Start);
			Assert.AreEqual(6, matches[0][1].End);
			Assert.AreEqual(9, matches[5].EntireMatch.Start);
			Assert.AreEqual(23, matches[5].EntireMatch.End);

			Assert.True(pattern.IsMatch(annList, Direction.RightToLeft, ModeType.Synthesis, out matches));
			Assert.AreEqual(6, matches.Count);
			Assert.AreEqual(0, matches[0].EntireMatch.Start);
			Assert.AreEqual(28, matches[0].EntireMatch.End);
			Assert.AreEqual(4, matches[0][1].Start);
			Assert.AreEqual(6, matches[0][1].End);
			Assert.AreEqual(9, matches[5].EntireMatch.Start);
			Assert.AreEqual(23, matches[5].EntireMatch.End);
		}
	}
}
