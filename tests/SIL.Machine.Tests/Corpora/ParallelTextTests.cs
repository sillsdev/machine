using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.Corpora
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
					new TextAlignment(new TextSegmentRef(1, 1), new[] { new AlignedWordPair(0, 0) }),
					new TextAlignment(new TextSegmentRef(1, 2), new[] { new AlignedWordPair(1, 1) }),
					new TextAlignment(new TextSegmentRef(1, 3), new[] { new AlignedWordPair(2, 2) })
				});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(3));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 1 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 1 .".Split()));
			Assert.That(segments[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
			Assert.That(segments[2].SourceSegment, Is.EqualTo("source segment 1 3 .".Split()));
			Assert.That(segments[2].TargetSegment, Is.EqualTo("target segment 1 3 .".Split()));
			Assert.That(segments[2].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(2, 2) }));
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
					new TextAlignment(new TextSegmentRef(1, 1), new[] { new AlignedWordPair(0, 0) }),
					new TextAlignment(new TextSegmentRef(1, 3), new[] { new AlignedWordPair(2, 2) })
				});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 1 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 1 .".Split()));
			Assert.That(segments[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 1 3 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 1 3 .".Split()));
			Assert.That(segments[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(2, 2) }));
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
					new TextAlignment(new TextSegmentRef(1, 1), new[] { new AlignedWordPair(0, 0) }),
					new TextAlignment(new TextSegmentRef(1, 3), new[] { new AlignedWordPair(2, 2) })
				});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 1 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 1 .".Split()));
			Assert.That(segments[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 1 3 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 1 3 .".Split()));
			Assert.That(segments[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(2, 2) }));
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
					new TextAlignment(new TextSegmentRef(1, 1), new[] { new AlignedWordPair(0, 0) }),
					new TextAlignment(new TextSegmentRef(1, 2), new[] { new AlignedWordPair(1, 1) })
				});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 1 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 1 .".Split()));
			Assert.That(segments[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 1 2 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 1 2 .".Split()));
			Assert.That(segments[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(1, 1) }));
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
					new TextAlignment(new TextSegmentRef(1, 1), new[] { new AlignedWordPair(0, 0) }),
					new TextAlignment(new TextSegmentRef(1, 2), new[] { new AlignedWordPair(1, 1) })
				});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 1 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 1 .".Split()));
			Assert.That(segments[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 1 2 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 1 2 .".Split()));
			Assert.That(segments[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(1, 1) }));
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
					new TextAlignment(new TextSegmentRef(1, 2), new[] { new AlignedWordPair(1, 1) }),
					new TextAlignment(new TextSegmentRef(1, 3), new[] { new AlignedWordPair(2, 2) })
				});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 2 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 2 .".Split()));
			Assert.That(segments[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(1, 1) }));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 1 3 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 1 3 .".Split()));
			Assert.That(segments[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(2, 2) }));
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
					new TextAlignment(new TextSegmentRef(1, 2), new[] { new AlignedWordPair(1, 1) }),
					new TextAlignment(new TextSegmentRef(1, 3), new[] { new AlignedWordPair(2, 2) })
				});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 2 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 2 .".Split()));
			Assert.That(segments[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(1, 1) }));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 1 3 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 1 3 .".Split()));
			Assert.That(segments[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(2, 2) }));
		}
	}
}
