using System.Linq;
using NUnit.Framework;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace SIL.Machine.Tests.Corpora
{
	[TestFixture]
	public class ParallelTextTests
	{
		[Test]
		public void Segments_NoSegments_ReturnsEmpty()
		{
			var sourceText = new MemoryText("text1", Enumerable.Empty<TextSegment>());
			var targetText = new MemoryText("text1", Enumerable.Empty<TextSegment>());
			var parallelText = new ParallelText(sourceText, targetText);
			Assert.That(parallelText.Segments, Is.Empty);
		}

		[Test]
		public void Segments_NoMissingSegments_ReturnsSegments()
		{
			var sourceText = new MemoryText("text1", new[]
				{
					new TextSegment(new TextSegmentRef(1, 1), "source segment 1 1 .".Split()),
					new TextSegment(new TextSegmentRef(1, 2), "source segment 1 2 .".Split()),
					new TextSegment(new TextSegmentRef(1, 3), "source segment 1 3 .".Split())   
				});
			var targetText = new MemoryText("text1", new[]
				{
					new TextSegment(new TextSegmentRef(1, 1), "target segment 1 1 .".Split()),
					new TextSegment(new TextSegmentRef(1, 2), "target segment 1 2 .".Split()),
					new TextSegment(new TextSegmentRef(1, 3), "target segment 1 3 .".Split())
				});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
				{
					new TextAlignment(new TextSegmentRef(1, 1), new WordAlignmentMatrix(4, 4) {[0, 0] = AlignmentType.Aligned}),
					new TextAlignment(new TextSegmentRef(1, 2), new WordAlignmentMatrix(4, 4) {[1, 1] = AlignmentType.Aligned}),
					new TextAlignment(new TextSegmentRef(1, 3), new WordAlignmentMatrix(4, 4) {[2, 2] = AlignmentType.Aligned})   
				});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(3));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 1 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 1 .".Split()));
			Assert.That(segments[0].Alignment[0, 0], Is.EqualTo(AlignmentType.Aligned));
			Assert.That(segments[2].SourceSegment, Is.EqualTo("source segment 1 3 .".Split()));
			Assert.That(segments[2].TargetSegment, Is.EqualTo("target segment 1 3 .".Split()));
			Assert.That(segments[2].Alignment[2, 2], Is.EqualTo(AlignmentType.Aligned));
		}

		[Test]
		public void Segments_MissingMiddleTargetSegment_ReturnsSegments()
		{
			var sourceText = new MemoryText("text1", new[]
				{
					new TextSegment(new TextSegmentRef(1, 1), "source segment 1 1 .".Split()),
					new TextSegment(new TextSegmentRef(1, 2), "source segment 1 2 .".Split()),
					new TextSegment(new TextSegmentRef(1, 3), "source segment 1 3 .".Split())
				});
			var targetText = new MemoryText("text1", new[]
				{
					new TextSegment(new TextSegmentRef(1, 1), "target segment 1 1 .".Split()),
					new TextSegment(new TextSegmentRef(1, 3), "target segment 1 3 .".Split())
				});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
				{
					new TextAlignment(new TextSegmentRef(1, 1), new WordAlignmentMatrix(4, 4) {[0, 0] = AlignmentType.Aligned}),
					new TextAlignment(new TextSegmentRef(1, 3), new WordAlignmentMatrix(4, 4) {[2, 2] = AlignmentType.Aligned})
				});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 1 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 1 .".Split()));
			Assert.That(segments[0].Alignment[0, 0], Is.EqualTo(AlignmentType.Aligned));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 1 3 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 1 3 .".Split()));
			Assert.That(segments[1].Alignment[2, 2], Is.EqualTo(AlignmentType.Aligned));
		}

		[Test]
		public void Segments_MissingMiddleSourceSegment_ReturnsSegments()
		{
			var sourceText = new MemoryText("text1", new[]
				{
					new TextSegment(new TextSegmentRef(1, 1), "source segment 1 1 .".Split()),
					new TextSegment(new TextSegmentRef(1, 3), "source segment 1 3 .".Split())
				});
			var targetText = new MemoryText("text1", new[]
				{
					new TextSegment(new TextSegmentRef(1, 1), "target segment 1 1 .".Split()),
					new TextSegment(new TextSegmentRef(1, 2), "target segment 1 2 .".Split()),
					new TextSegment(new TextSegmentRef(1, 3), "target segment 1 3 .".Split())
				});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
				{
					new TextAlignment(new TextSegmentRef(1, 1), new WordAlignmentMatrix(4, 4) {[0, 0] = AlignmentType.Aligned}),
					new TextAlignment(new TextSegmentRef(1, 3), new WordAlignmentMatrix(4, 4) {[2, 2] = AlignmentType.Aligned})
				});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 1 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 1 .".Split()));
			Assert.That(segments[0].Alignment[0, 0], Is.EqualTo(AlignmentType.Aligned));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 1 3 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 1 3 .".Split()));
			Assert.That(segments[1].Alignment[2, 2], Is.EqualTo(AlignmentType.Aligned));
		}

		[Test]
		public void Segments_MissingLastTargetSegment_ReturnsSegments()
		{
			var sourceText = new MemoryText("text1", new[]
				{
					new TextSegment(new TextSegmentRef(1, 1), "source segment 1 1 .".Split()),
					new TextSegment(new TextSegmentRef(1, 2), "source segment 1 2 .".Split()),
					new TextSegment(new TextSegmentRef(1, 3), "source segment 1 3 .".Split())
				});
			var targetText = new MemoryText("text1", new[]
				{
					new TextSegment(new TextSegmentRef(1, 1), "target segment 1 1 .".Split()),
					new TextSegment(new TextSegmentRef(1, 2), "target segment 1 2 .".Split())
				});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
				{
					new TextAlignment(new TextSegmentRef(1, 1), new WordAlignmentMatrix(4, 4) {[0, 0] = AlignmentType.Aligned}),
					new TextAlignment(new TextSegmentRef(1, 2), new WordAlignmentMatrix(4, 4) {[1, 1] = AlignmentType.Aligned})
				});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 1 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 1 .".Split()));
			Assert.That(segments[0].Alignment[0, 0], Is.EqualTo(AlignmentType.Aligned));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 1 2 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 1 2 .".Split()));
			Assert.That(segments[1].Alignment[1, 1], Is.EqualTo(AlignmentType.Aligned));
		}

		[Test]
		public void Segments_MissingLastSourceSegment_ReturnsSegments()
		{
			var sourceText = new MemoryText("text1", new[]
				{
					new TextSegment(new TextSegmentRef(1, 1), "source segment 1 1 .".Split()),
					new TextSegment(new TextSegmentRef(1, 2), "source segment 1 2 .".Split())
				});
			var targetText = new MemoryText("text1", new[]
				{
					new TextSegment(new TextSegmentRef(1, 1), "target segment 1 1 .".Split()),
					new TextSegment(new TextSegmentRef(1, 2), "target segment 1 2 .".Split()),
					new TextSegment(new TextSegmentRef(1, 3), "target segment 1 3 .".Split())
				});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
				{
					new TextAlignment(new TextSegmentRef(1, 1), new WordAlignmentMatrix(4, 4) {[0, 0] = AlignmentType.Aligned}),
					new TextAlignment(new TextSegmentRef(1, 2), new WordAlignmentMatrix(4, 4) {[1, 1] = AlignmentType.Aligned})
				});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 1 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 1 .".Split()));
			Assert.That(segments[0].Alignment[0, 0], Is.EqualTo(AlignmentType.Aligned));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 1 2 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 1 2 .".Split()));
			Assert.That(segments[1].Alignment[1, 1], Is.EqualTo(AlignmentType.Aligned));
		}

		[Test]
		public void Segments_MissingFirstTargetSegment_ReturnsSegments()
		{
			var sourceText = new MemoryText("text1", new[]
				{
					new TextSegment(new TextSegmentRef(1, 1), "source segment 1 1 .".Split()),
					new TextSegment(new TextSegmentRef(1, 2), "source segment 1 2 .".Split()),
					new TextSegment(new TextSegmentRef(1, 3), "source segment 1 3 .".Split())
				});
			var targetText = new MemoryText("text1", new[]
				{
					new TextSegment(new TextSegmentRef(1, 2), "target segment 1 2 .".Split()),
					new TextSegment(new TextSegmentRef(1, 3), "target segment 1 3 .".Split())
				});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
				{
					new TextAlignment(new TextSegmentRef(1, 2), new WordAlignmentMatrix(4, 4) {[1, 1] = AlignmentType.Aligned}),
					new TextAlignment(new TextSegmentRef(1, 3), new WordAlignmentMatrix(4, 4) {[2, 2] = AlignmentType.Aligned})
				});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 2 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 2 .".Split()));
			Assert.That(segments[0].Alignment[1, 1], Is.EqualTo(AlignmentType.Aligned));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 1 3 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 1 3 .".Split()));
			Assert.That(segments[1].Alignment[2, 2], Is.EqualTo(AlignmentType.Aligned));
		}

		[Test]
		public void Segments_MissingFirstSourceSegment_ReturnsSegments()
		{
			var sourceText = new MemoryText("text1", new[]
				{
					new TextSegment(new TextSegmentRef(1, 2), "source segment 1 2 .".Split()),
					new TextSegment(new TextSegmentRef(1, 3), "source segment 1 3 .".Split())
				});
			var targetText = new MemoryText("text1", new[]
				{
					new TextSegment(new TextSegmentRef(1, 1), "target segment 1 1 .".Split()),
					new TextSegment(new TextSegmentRef(1, 2), "target segment 1 2 .".Split()),
					new TextSegment(new TextSegmentRef(1, 3), "target segment 1 3 .".Split())
				});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
				{
					new TextAlignment(new TextSegmentRef(1, 2), new WordAlignmentMatrix(4, 4) {[1, 1] = AlignmentType.Aligned}),
					new TextAlignment(new TextSegmentRef(1, 3), new WordAlignmentMatrix(4, 4) {[2, 2] = AlignmentType.Aligned})
				});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 2 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 2 .".Split()));
			Assert.That(segments[0].Alignment[1, 1], Is.EqualTo(AlignmentType.Aligned));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 1 3 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 1 3 .".Split()));
			Assert.That(segments[1].Alignment[2, 2], Is.EqualTo(AlignmentType.Aligned));
		}
	}
}
