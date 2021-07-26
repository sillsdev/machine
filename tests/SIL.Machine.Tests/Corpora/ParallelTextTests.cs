using System;
using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.Corpora
{
	[TestFixture]
	public class ParallelTextTests
	{
		[Test]
		public void Segments_NoSegments()
		{
			var sourceText = new MemoryText("text1", Enumerable.Empty<TextSegment>());
			var targetText = new MemoryText("text1", Enumerable.Empty<TextSegment>());
			var parallelText = new ParallelText(sourceText, targetText);
			Assert.That(parallelText.Segments, Is.Empty);
		}

		[Test]
		public void Segments_NoMissingSegments()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(1, "source segment 1 ."),
				Segment(2, "source segment 2 ."),
				Segment(3, "source segment 3 .")
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(1, "target segment 1 ."),
				Segment(2, "target segment 2 ."),
				Segment(3, "target segment 3 .")
			});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
			{
				Alignment(1, new AlignedWordPair(0, 0)),
				Alignment(2, new AlignedWordPair(1, 1)),
				Alignment(3, new AlignedWordPair(2, 2))
			});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(3));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 .".Split()));
			Assert.That(segments[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
			Assert.That(segments[2].SourceSegment, Is.EqualTo("source segment 3 .".Split()));
			Assert.That(segments[2].TargetSegment, Is.EqualTo("target segment 3 .".Split()));
			Assert.That(segments[2].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(2, 2) }));
		}

		[Test]
		public void Segments_MissingMiddleTargetSegment()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(1, "source segment 1 ."),
				Segment(2, "source segment 2 ."),
				Segment(3, "source segment 3 .")
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(1, "target segment 1 ."),
				Segment(3, "target segment 3 .")
			});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
			{
				Alignment(1, new AlignedWordPair(0, 0)),
				Alignment(3, new AlignedWordPair(2, 2))
			});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 .".Split()));
			Assert.That(segments[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 3 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 3 .".Split()));
			Assert.That(segments[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(2, 2) }));
		}

		[Test]
		public void Segments_MissingMiddleSourceSegment()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(1, "source segment 1 ."),
				Segment(3, "source segment 3 .")
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(1, "target segment 1 ."),
				Segment(2, "target segment 2 ."),
				Segment(3, "target segment 3 .")
			});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
			{
				Alignment(1, new AlignedWordPair(0, 0)),
				Alignment(3, new AlignedWordPair(2, 2))
			});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 .".Split()));
			Assert.That(segments[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 3 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 3 .".Split()));
			Assert.That(segments[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(2, 2) }));
		}

		[Test]
		public void Segments_MissingLastTargetSegment()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(1, "source segment 1 ."),
				Segment(2, "source segment 2 ."),
				Segment(3, "source segment 3 .")
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(1, "target segment 1 ."),
				Segment(2, "target segment 2 .")
			});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
			{
				Alignment(1, new AlignedWordPair(0, 0)),
				Alignment(2, new AlignedWordPair(1, 1))
			});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 .".Split()));
			Assert.That(segments[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
			Assert.That(segments[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(1, 1) }));
		}

		[Test]
		public void Segments_MissingLastSourceSegment()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(1, "source segment 1 ."),
				Segment(2, "source segment 2 .")
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(1, "target segment 1 ."),
				Segment(2, "target segment 2 ."),
				Segment(3, "target segment 3 .")
			});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
			{
				Alignment(1, new AlignedWordPair(0, 0)),
				Alignment(2, new AlignedWordPair(1, 1))
			});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 .".Split()));
			Assert.That(segments[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(0, 0) }));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
			Assert.That(segments[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(1, 1) }));
		}

		[Test]
		public void Segments_MissingFirstTargetSegment()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(1, "source segment 1 ."),
				Segment(2, "source segment 2 ."),
				Segment(3, "source segment 3 .")
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(2, "target segment 2 ."),
				Segment(3, "target segment 3 .")
			});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
			{
				Alignment(2, new AlignedWordPair(1, 1)),
				Alignment(3, new AlignedWordPair(2, 2))
			});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
			Assert.That(segments[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(1, 1) }));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 3 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 3 .".Split()));
			Assert.That(segments[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(2, 2) }));
		}

		[Test]
		public void Segments_MissingFirstSourceSegment()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(2, "source segment 2 ."),
				Segment(3, "source segment 3 .")
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(1, "target segment 1 ."),
				Segment(2, "target segment 2 ."),
				Segment(3, "target segment 3 .")
			});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
			{
				Alignment(2, new AlignedWordPair(1, 1)),
				Alignment(3, new AlignedWordPair(2, 2))
			});

			var parallelText = new ParallelText(sourceText, targetText, alignments);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
			Assert.That(segments[0].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(1, 1) }));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 3 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 3 .".Split()));
			Assert.That(segments[1].AlignedWordPairs, Is.EquivalentTo(new[] { new AlignedWordPair(2, 2) }));
		}

		[Test]
		public void Segments_Range()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(1, "source segment 1 ."),
				Segment(2, "source segment 2 . source segment 3 .", isInRange: true, isRangeStart: true),
				Segment(3, isInRange: true),
				Segment(4, "source segment 4 .")
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(1, "target segment 1 ."),
				Segment(2, "target segment 2 ."),
				Segment(3, "target segment 3 ."),
				Segment(4, "target segment 4 .")
			});

			var parallelText = new ParallelText(sourceText, targetText);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(3));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 2 . source segment 3 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 2 . target segment 3 .".Split()));
		}

		[Test]
		public void Segments_OverlappingRanges()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(1, "source segment 1 ."),
				Segment(2, "source segment 2 . source segment 3 .", isInRange: true, isRangeStart: true),
				Segment(3, isInRange: true)
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(1, "target segment 1 . target segment 2 .", isInRange: true, isRangeStart: true),
				Segment(2, isInRange: true),
				Segment(3, "target segment 3 .")
			});

			var parallelText = new ParallelText(sourceText, targetText);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(1));
			Assert.That(segments[0].SourceSegment,
				Is.EqualTo("source segment 1 . source segment 2 . source segment 3 .".Split()));
			Assert.That(segments[0].TargetSegment,
				Is.EqualTo("target segment 1 . target segment 2 . target segment 3 .".Split()));
		}

		[Test]
		public void Segments_AdjacentRangesSameText()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(1, "source segment 1 . source segment 2 .", isInRange: true, isRangeStart: true),
				Segment(2, isInRange: true),
				Segment(3, "source segment 3 . source segment 4 .", isInRange: true, isRangeStart: true),
				Segment(4, isInRange: true)
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(1, "target segment 1 ."),
				Segment(2, "target segment 2 ."),
				Segment(3, "target segment 3 ."),
				Segment(4, "target segment 4 .")
			});

			var parallelText = new ParallelText(sourceText, targetText);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 . source segment 2 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 . target segment 2 .".Split()));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 3 . source segment 4 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 3 . target segment 4 .".Split()));
		}

		[Test]
		public void Segments_AdjacentRangesDifferentTexts()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(1, "source segment 1 . source segment 2 .", isInRange: true, isRangeStart: true),
				Segment(2, isInRange: true),
				Segment(3, "source segment 3 ."),
				Segment(4, "source segment 4 .")
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(1, "target segment 1 ."),
				Segment(2, "target segment 2 ."),
				Segment(3, "target segment 3 . target segment 4 .", isInRange: true, isRangeStart: true),
				Segment(4, isInRange: true)
			});

			var parallelText = new ParallelText(sourceText, targetText);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 . source segment 2 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 . target segment 2 .".Split()));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 3 . source segment 4 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 3 . target segment 4 .".Split()));
		}

		[Test]
		public void GetSegments_RangeAllTargetSegments()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(1, "source segment 1 ."),
				Segment(2, "source segment 2 . source segment 3 .", isInRange: true, isRangeStart: true),
				Segment(3, isInRange: true),
				Segment(4, "source segment 4 .")
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(1, "target segment 1 ."),
				Segment(2, "target segment 2 ."),
				Segment(3, "target segment 3 ."),
				Segment(4, "target segment 4 .")
			});

			var parallelText = new ParallelText(sourceText, targetText);
			ParallelTextSegment[] segments = parallelText.GetSegments(allTargetSegments: true).ToArray();
			Assert.That(segments.Length, Is.EqualTo(4));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 2 . source segment 3 .".Split()));
			Assert.That(segments[1].IsSourceInRange, Is.True);
			Assert.That(segments[1].IsSourceRangeStart, Is.True);
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
			Assert.That(segments[2].SourceSegment, Is.EqualTo(Enumerable.Empty<string>()));
			Assert.That(segments[2].IsSourceInRange, Is.True);
			Assert.That(segments[2].IsSourceRangeStart, Is.False);
			Assert.That(segments[2].TargetSegment, Is.EqualTo("target segment 3 .".Split()));
		}

		[Test]
		public void Segments_SameRefMiddleManyToMany()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(1, "source segment 1 ."),
				Segment(2, "source segment 2-1 ."),
				Segment(2, "source segment 2-2 ."),
				Segment(3, "source segment 3 .")
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(1, "target segment 1 ."),
				Segment(2, "target segment 2-1 ."),
				Segment(2, "target segment 2-2 ."),
				Segment(3, "target segment 3 .")
			});

			var parallelText = new ParallelText(sourceText, targetText);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(6));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 2-1 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 2-1 .".Split()));
			Assert.That(segments[2].SourceSegment, Is.EqualTo("source segment 2-1 .".Split()));
			Assert.That(segments[2].TargetSegment, Is.EqualTo("target segment 2-2 .".Split()));
			Assert.That(segments[3].SourceSegment, Is.EqualTo("source segment 2-2 .".Split()));
			Assert.That(segments[3].TargetSegment, Is.EqualTo("target segment 2-1 .".Split()));
			Assert.That(segments[4].SourceSegment, Is.EqualTo("source segment 2-2 .".Split()));
			Assert.That(segments[4].TargetSegment, Is.EqualTo("target segment 2-2 .".Split()));
		}

		[Test]
		public void GetSegments_SameRefMiddleOneToMany()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(1, "source segment 1 ."),
				Segment(2, "source segment 2 ."),
				Segment(3, "source segment 3 .")
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(1, "target segment 1 ."),
				Segment(2, "target segment 2-1 ."),
				Segment(2, "target segment 2-2 ."),
				Segment(3, "target segment 3 .")
			});

			var parallelText = new ParallelText(sourceText, targetText);
			ParallelTextSegment[] segments = parallelText.GetSegments(allTargetSegments: true).ToArray();
			Assert.That(segments.Length, Is.EqualTo(4));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 2-1 .".Split()));
			Assert.That(segments[2].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
			Assert.That(segments[2].TargetSegment, Is.EqualTo("target segment 2-2 .".Split()));
		}

		[Test]
		public void GetSegments_SameRefMiddleManyToOne()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(1, "source segment 1 ."),
				Segment(2, "source segment 2-1 ."),
				Segment(2, "source segment 2-2 ."),
				Segment(3, "source segment 3 .")
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(1, "target segment 1 ."),
				Segment(2, "target segment 2 ."),
				Segment(3, "target segment 3 .")
			});

			var parallelText = new ParallelText(sourceText, targetText);
			ParallelTextSegment[] segments = parallelText.GetSegments(allSourceSegments: true).ToArray();
			Assert.That(segments.Length, Is.EqualTo(4));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 2-1 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
			Assert.That(segments[2].SourceSegment, Is.EqualTo("source segment 2-2 .".Split()));
			Assert.That(segments[2].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
		}

		[Test]
		public void GetSegments_SameRefLastOneToMany()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(1, "source segment 1 ."),
				Segment(2, "source segment 2 ."),
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(1, "target segment 1 ."),
				Segment(2, "target segment 2-1 ."),
				Segment(2, "target segment 2-2 ."),
				Segment(3, "target segment 3 .")
			});

			var parallelText = new ParallelText(sourceText, targetText);
			ParallelTextSegment[] segments = parallelText.GetSegments(allTargetSegments: true).ToArray();
			Assert.That(segments.Length, Is.EqualTo(4));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 2-1 .".Split()));
			Assert.That(segments[2].SourceSegment, Is.EqualTo("source segment 2 .".Split()));
			Assert.That(segments[2].TargetSegment, Is.EqualTo("target segment 2-2 .".Split()));
		}

		[Test]
		public void GetSegments_SameRefLastManyToOne()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				Segment(1, "source segment 1 ."),
				Segment(2, "source segment 2-1 ."),
				Segment(2, "source segment 2-2 ."),
				Segment(3, "source segment 3 .")
			});
			var targetText = new MemoryText("text1", new[]
			{
				Segment(1, "target segment 1 ."),
				Segment(2, "target segment 2 ."),
			});

			var parallelText = new ParallelText(sourceText, targetText);
			ParallelTextSegment[] segments = parallelText.GetSegments(allSourceSegments: true).ToArray();
			Assert.That(segments.Length, Is.EqualTo(4));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 2-1 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
			Assert.That(segments[2].SourceSegment, Is.EqualTo("source segment 2-2 .".Split()));
			Assert.That(segments[2].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
		}

		private static TextSegment Segment(int key, string text = "", bool isInRange = false, bool isRangeStart = false)
		{
			return new TextSegment("text1", new TextSegmentRef(key),
				text.Length == 0 ? Array.Empty<string>() : text.Split(), isSentenceStart: true, isInRange, isRangeStart,
				isEmpty: text.Length == 0);
		}

		private static TextAlignment Alignment(int key, params AlignedWordPair[] pairs)
		{
			return new TextAlignment("text1", new TextSegmentRef(key), pairs);
		}
	}
}
