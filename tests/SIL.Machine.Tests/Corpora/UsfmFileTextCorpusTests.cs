using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	[TestFixture]
	public class UsfmFileTextCorpusTests
	{
		[Test]
		public void Texts()
		{
			var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

			Assert.That(corpus.Texts.Select(t => t.Id), Is.EquivalentTo(new[] { "MAT", "MRK" }));
		}

		[Test]
		public void TryGetText()
		{
			var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

			Assert.That(corpus.TryGetText("MAT", out IText mat), Is.True);
			Assert.That(mat.GetRows(), Is.Not.Empty);
			Assert.That(corpus.TryGetText("LUK", out _), Is.False);
		}

		[Test]
		public void GetRows_BasedOn()
		{
			Versification.Table.Implementation.RemoveAllUnknownVersifications();

			string src = "&MAT 1:4-5 = MAT 1:4\nMAT 1:2 = MAT 1:3\nMAT 1:3 = MAT 1:2\n";
			ScrVers versification;
			using (var reader = new StringReader(src))
			{
				versification = Versification.Table.Implementation.Load(reader, "vers.txt", ScrVers.English, "custom");
			}

			var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath,
				versification);

			var origVersCorpus = new TestScriptureTextCorpus(ScrVers.Original);

			TextCorpusRow[] segments = corpus.GetRows(origVersCorpus).ToArray();
			Assert.That(segments.Length, Is.EqualTo(14));

			Assert.That(segments[0].Ref, Is.EqualTo(new VerseRef("MAT 1:1", versification)));
			Assert.That(segments[0].Text, Is.EqualTo("Chapter one, verse one."));

			Assert.That(segments[1].Ref, Is.EqualTo(new VerseRef("MAT 1:3", versification)));
			Assert.That(segments[1].Text, Is.EqualTo("Chapter one, verse three."));

			Assert.That(segments[2].Ref, Is.EqualTo(new VerseRef("MAT 1:2", versification)));
			Assert.That(segments[2].Text, Is.EqualTo("Chapter one, verse two."));

			Assert.That(segments[3].Ref, Is.EqualTo(new VerseRef("MAT 1:4", versification)));
			Assert.That(segments[3].Segment, Is.EqualTo(
				new[] { "Chapter one, verse four,", "Chapter one, verse five." }));
			Assert.That(segments[3].IsInRange, Is.True);
			Assert.That(segments[3].IsRangeStart, Is.True);

			Assert.That(segments[4].Ref, Is.EqualTo(new VerseRef("MAT 1:5", versification)));
			Assert.That(segments[4].Text, Is.Empty);
			Assert.That(segments[4].IsInRange, Is.True);
			Assert.That(segments[4].IsRangeStart, Is.False);

			Assert.That(segments[5].Ref, Is.EqualTo(new VerseRef("MAT 2:1", versification)));
			Assert.That(segments[5].Text, Is.EqualTo("Chapter two, verse one."));

			Assert.That(segments[6].Ref, Is.EqualTo(new VerseRef("MAT 2:2", versification)));
			Assert.That(segments[6].Text, Is.EqualTo("Chapter two, verse two. Chapter two, verse three."));
			Assert.That(segments[6].IsInRange, Is.True);
			Assert.That(segments[6].IsRangeStart, Is.True);

			Assert.That(segments[7].Ref, Is.EqualTo(new VerseRef("MAT 2:3", versification)));
			Assert.That(segments[7].Text, Is.Empty);
			Assert.That(segments[7].IsInRange, Is.True);
			Assert.That(segments[7].IsRangeStart, Is.False);

			Assert.That(segments[8].Ref, Is.EqualTo(new VerseRef("MAT 2:4a", versification)));
			Assert.That(segments[8].Text, Is.Empty);
			Assert.That(segments[8].IsInRange, Is.True);
			Assert.That(segments[8].IsRangeStart, Is.False);

			Assert.That(segments[9].Ref, Is.EqualTo(new VerseRef("MAT 2:4b", versification)));
			Assert.That(segments[9].Text, Is.EqualTo("Chapter two, verse four."));

			Assert.That(segments[10].Ref, Is.EqualTo(new VerseRef("MAT 2:5", versification)));
			Assert.That(segments[10].Text, Is.EqualTo("Chapter two, verse five."));

			Assert.That(segments[11].Ref, Is.EqualTo(new VerseRef("MAT 2:6", versification)));
			Assert.That(segments[11].Text, Is.EqualTo("Chapter two, verse six."));
		}

		private class TestScriptureTextCorpus : ScriptureTextCorpus
		{
			public TestScriptureTextCorpus(ScrVers versification)
			{
				Versification = versification;
			}

			public override ScrVers Versification { get; }
		}
	}
}
