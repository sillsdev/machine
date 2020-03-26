using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.Corpora
{
	[TestFixture]
	public class ParallelTextCorpusTests
	{
		[Test]
		public void Texts_NoTexts()
		{
			var sourceCorpus = new DictionaryTextCorpus();
			var targetCorpus = new DictionaryTextCorpus();

			var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
			Assert.That(parallelCorpus.Texts, Is.Empty);
		}

		[Test]
		public void Texts_NoMissingTexts()
		{
			var sourceCorpus = new DictionaryTextCorpus(
				new MemoryText("text1"),
				new MemoryText("text2"),
				new MemoryText("text3"));
			var targetCorpus = new DictionaryTextCorpus(
				new MemoryText("text1"),
				new MemoryText("text2"),
				new MemoryText("text3"));
			var alignmentCorpus = new DictionaryTextAlignmentCorpus(
				new MemoryTextAlignmentCollection("text1"),
				new MemoryTextAlignmentCollection("text2"),
				new MemoryTextAlignmentCollection("text3"));

			var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignmentCorpus);
			ParallelText[] texts = parallelCorpus.Texts.ToArray();
			Assert.That(texts.Select(t => t.Id), Is.EqualTo(new[] { "text1", "text2", "text3" }));
		}

		[Test]
		public void Texts_MissingText()
		{
			var sourceCorpus = new DictionaryTextCorpus(
				new MemoryText("text1"),
				new MemoryText("text2"),
				new MemoryText("text3"));
			var targetCorpus = new DictionaryTextCorpus(
				new MemoryText("text1"),
				new MemoryText("text3"));
			var alignmentCorpus = new DictionaryTextAlignmentCorpus(
				new MemoryTextAlignmentCollection("text1"),
				new MemoryTextAlignmentCollection("text3"));

			var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignmentCorpus);
			ParallelText[] texts = parallelCorpus.Texts.ToArray();
			Assert.That(texts.Select(t => t.Id), Is.EqualTo(new[] { "text1", "text3" }));
		}

		[Test]
		public void GetTexts_MissingTargetTextAllSourceSegments()
		{
			var sourceCorpus = new DictionaryTextCorpus(
				new MemoryText("text1"),
				new MemoryText("text2"),
				new MemoryText("text3"));
			var targetCorpus = new DictionaryTextCorpus(
				new MemoryText("text1"),
				new MemoryText("text3"));
			var alignmentCorpus = new DictionaryTextAlignmentCorpus(
				new MemoryTextAlignmentCollection("text1"),
				new MemoryTextAlignmentCollection("text3"));

			var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignmentCorpus);
			ParallelText[] texts = parallelCorpus.GetTexts(true, false).ToArray();
			Assert.That(texts.Select(t => t.Id), Is.EqualTo(new[] { "text1", "text2", "text3" }));
		}

		[Test]
		public void GetTexts_MissingSourceTextAllTargetSegments()
		{
			var sourceCorpus = new DictionaryTextCorpus(
				new MemoryText("text1"),
				new MemoryText("text3"));
			var targetCorpus = new DictionaryTextCorpus(
				new MemoryText("text1"),
				new MemoryText("text2"),
				new MemoryText("text3"));
			var alignmentCorpus = new DictionaryTextAlignmentCorpus(
				new MemoryTextAlignmentCollection("text1"),
				new MemoryTextAlignmentCollection("text3"));

			var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignmentCorpus);
			ParallelText[] texts = parallelCorpus.GetTexts(false, true).ToArray();
			Assert.That(texts.Select(t => t.Id), Is.EqualTo(new[] { "text1", "text2", "text3" }));
		}

		[Test]
		public void GetTexts_MissingSourceAndTargetTextAllSourceAndTargetSegments()
		{
			var sourceCorpus = new DictionaryTextCorpus(
				new MemoryText("text1"),
				new MemoryText("text3"));
			var targetCorpus = new DictionaryTextCorpus(
				new MemoryText("text1"),
				new MemoryText("text2"));
			var alignmentCorpus = new DictionaryTextAlignmentCorpus(
				new MemoryTextAlignmentCollection("text1"));

			var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignmentCorpus);
			ParallelText[] texts = parallelCorpus.GetTexts(true, true).ToArray();
			Assert.That(texts.Select(t => t.Id), Is.EqualTo(new[] { "text1", "text2", "text3" }));
		}
	}
}
