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
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "source segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2), "source segment 1 2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "source segment 1 3 .".Split())
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "target segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2), "target segment 1 2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "target segment 1 3 .".Split())
			});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
			{
				new TextAlignment("text1", new TextSegmentRef(1, 1), new[] { new AlignedWordPair(0, 0) }),
				new TextAlignment("text1", new TextSegmentRef(1, 2), new[] { new AlignedWordPair(1, 1) }),
				new TextAlignment("text1", new TextSegmentRef(1, 3), new[] { new AlignedWordPair(2, 2) })
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
		public void Segments_MissingMiddleTargetSegment()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "source segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2), "source segment 1 2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "source segment 1 3 .".Split())
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "target segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "target segment 1 3 .".Split())
			});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
			{
				new TextAlignment("text1", new TextSegmentRef(1, 1), new[] { new AlignedWordPair(0, 0) }),
				new TextAlignment("text1", new TextSegmentRef(1, 3), new[] { new AlignedWordPair(2, 2) })
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
		public void Segments_MissingMiddleSourceSegment()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "source segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "source segment 1 3 .".Split())
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "target segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2), "target segment 1 2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "target segment 1 3 .".Split())
			});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
			{
				new TextAlignment("text1", new TextSegmentRef(1, 1), new[] { new AlignedWordPair(0, 0) }),
				new TextAlignment("text1", new TextSegmentRef(1, 3), new[] { new AlignedWordPair(2, 2) })
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
		public void Segments_MissingLastTargetSegment()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "source segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2), "source segment 1 2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "source segment 1 3 .".Split())
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "target segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2), "target segment 1 2 .".Split())
			});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
			{
				new TextAlignment("text1", new TextSegmentRef(1, 1), new[] { new AlignedWordPair(0, 0) }),
				new TextAlignment("text1", new TextSegmentRef(1, 2), new[] { new AlignedWordPair(1, 1) })
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
		public void Segments_MissingLastSourceSegment()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "source segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2), "source segment 1 2 .".Split())
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "target segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2), "target segment 1 2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "target segment 1 3 .".Split())
			});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
			{
				new TextAlignment("text1", new TextSegmentRef(1, 1), new[] { new AlignedWordPair(0, 0) }),
				new TextAlignment("text1", new TextSegmentRef(1, 2), new[] { new AlignedWordPair(1, 1) })
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
		public void Segments_MissingFirstTargetSegment()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "source segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2), "source segment 1 2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "source segment 1 3 .".Split())
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 2), "target segment 1 2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "target segment 1 3 .".Split())
			});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
			{
				new TextAlignment("text1", new TextSegmentRef(1, 2), new[] { new AlignedWordPair(1, 1) }),
				new TextAlignment("text1", new TextSegmentRef(1, 3), new[] { new AlignedWordPair(2, 2) })
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
		public void Segments_MissingFirstSourceSegment()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 2), "source segment 1 2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "source segment 1 3 .".Split())
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "target segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2), "target segment 1 2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "target segment 1 3 .".Split())
			});
			var alignments = new MemoryTextAlignmentCollection("text1", new[]
			{
				new TextAlignment("text1", new TextSegmentRef(1, 2), new[] { new AlignedWordPair(1, 1) }),
				new TextAlignment("text1", new TextSegmentRef(1, 3), new[] { new AlignedWordPair(2, 2) })
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
		public void Segments_Range()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "source segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2),
					"source segment 1 2 . source segment 1 3 .".Split(), inRange: true, rangeStart: true),
				TextSegment.CreateNoText("text1", new TextSegmentRef(1, 3), inRange: true),
				TextSegment.Create("text1", new TextSegmentRef(1, 4), "source segment 1 4 .".Split())
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "target segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2), "target segment 1 2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "target segment 1 3 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 4), "target segment 1 4 .".Split())
			});

			var parallelText = new ParallelText(sourceText, targetText);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(3));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 1 2 . source segment 1 3 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 1 2 . target segment 1 3 .".Split()));
		}

		[Test]
		public void Segments_OverlappingRanges()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "source segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2),
					"source segment 1 2 . source segment 1 3 .".Split(), inRange: true, rangeStart: true),
				TextSegment.CreateNoText("text1", new TextSegmentRef(1, 3), inRange: true)
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1),
					"target segment 1 1 . target segment 1 2 .".Split(), inRange: true, rangeStart: true),
				TextSegment.CreateNoText("text1", new TextSegmentRef(1, 2), inRange: true),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "target segment 1 3 .".Split())
			});

			var parallelText = new ParallelText(sourceText, targetText);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(1));
			Assert.That(segments[0].SourceSegment,
				Is.EqualTo("source segment 1 1 . source segment 1 2 . source segment 1 3 .".Split()));
			Assert.That(segments[0].TargetSegment,
				Is.EqualTo("target segment 1 1 . target segment 1 2 . target segment 1 3 .".Split()));
		}

		[Test]
		public void Segments_AdjacentRangesSameText()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1),
					"source segment 1 1 . source segment 1 2 .".Split(), inRange: true, rangeStart: true),
				TextSegment.CreateNoText("text1", new TextSegmentRef(1, 2), inRange: true),
				TextSegment.Create("text1", new TextSegmentRef(1, 3),
					"source segment 1 3 . source segment 1 4 .".Split(), inRange: true, rangeStart: true),
				TextSegment.CreateNoText("text1", new TextSegmentRef(1, 4), inRange: true)
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "target segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2), "target segment 1 2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "target segment 1 3 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 4), "target segment 1 4 .".Split())
			});

			var parallelText = new ParallelText(sourceText, targetText);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 1 . source segment 1 2 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 1 . target segment 1 2 .".Split()));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 1 3 . source segment 1 4 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 1 3 . target segment 1 4 .".Split()));
		}

		[Test]
		public void Segments_AdjacentRangesDifferentTexts()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1),
					"source segment 1 1 . source segment 1 2 .".Split(), inRange: true, rangeStart: true),
				TextSegment.CreateNoText("text1", new TextSegmentRef(1, 2), inRange: true),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "source segment 1 3 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 4), "source segment 1 4 .".Split())
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "target segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2), "target segment 1 2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 3),
					"target segment 1 3 . target segment 1 4 .".Split(), inRange: true, rangeStart: true),
				TextSegment.CreateNoText("text1", new TextSegmentRef(1, 4), inRange: true)
			});

			var parallelText = new ParallelText(sourceText, targetText);
			ParallelTextSegment[] segments = parallelText.Segments.ToArray();
			Assert.That(segments.Length, Is.EqualTo(2));
			Assert.That(segments[0].SourceSegment, Is.EqualTo("source segment 1 1 . source segment 1 2 .".Split()));
			Assert.That(segments[0].TargetSegment, Is.EqualTo("target segment 1 1 . target segment 1 2 .".Split()));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 1 3 . source segment 1 4 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 1 3 . target segment 1 4 .".Split()));
		}

		[Test]
		public void GetSegments_RangeAllTargetSegments()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "source segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2),
					"source segment 1 2 . source segment 1 3 .".Split(), inRange: true, rangeStart: true),
				TextSegment.CreateNoText("text1", new TextSegmentRef(1, 3), inRange: true),
				TextSegment.Create("text1", new TextSegmentRef(1, 4), "source segment 1 4 .".Split())
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1, 1), "target segment 1 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 2), "target segment 1 2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 3), "target segment 1 3 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(1, 4), "target segment 1 4 .".Split())
			});

			var parallelText = new ParallelText(sourceText, targetText);
			ParallelTextSegment[] segments = parallelText.GetSegments(allTargetSegments: true).ToArray();
			Assert.That(segments.Length, Is.EqualTo(4));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 1 2 . source segment 1 3 .".Split()));
			Assert.That(segments[1].IsSourceInRange, Is.True);
			Assert.That(segments[1].IsSourceRangeStart, Is.True);
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 1 2 .".Split()));
			Assert.That(segments[2].SourceSegment, Is.EqualTo(Enumerable.Empty<string>()));
			Assert.That(segments[2].IsSourceInRange, Is.True);
			Assert.That(segments[2].IsSourceRangeStart, Is.False);
			Assert.That(segments[2].TargetSegment, Is.EqualTo("target segment 1 3 .".Split()));
		}

		[Test]
		public void Segments_SameRefMiddleManyToMany()
		{
			var sourceText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1), "source segment 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(2), "source segment 2-1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(2), "source segment 2-2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(3), "source segment 3 .".Split())
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1), "target segment 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(2), "target segment 2-1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(2), "target segment 2-2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(3), "target segment 3 .".Split())
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
				TextSegment.Create("text1", new TextSegmentRef(1), "source segment 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(2), "source segment 2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(3), "source segment 3 .".Split())
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1), "target segment 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(2), "target segment 2-1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(2), "target segment 2-2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(3), "target segment 3 .".Split())
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
				TextSegment.Create("text1", new TextSegmentRef(1), "source segment 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(2), "source segment 2-1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(2), "source segment 2-2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(3), "source segment 3 .".Split())
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1), "target segment 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(2), "target segment 2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(3), "target segment 3 .".Split())
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
				TextSegment.Create("text1", new TextSegmentRef(1), "source segment 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(2), "source segment 2 .".Split()),
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1), "target segment 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(2), "target segment 2-1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(2), "target segment 2-2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(3), "target segment 3 .".Split())
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
				TextSegment.Create("text1", new TextSegmentRef(1), "source segment 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(2), "source segment 2-1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(2), "source segment 2-2 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(3), "source segment 3 .".Split())
			});
			var targetText = new MemoryText("text1", new[]
			{
				TextSegment.Create("text1", new TextSegmentRef(1), "target segment 1 .".Split()),
				TextSegment.Create("text1", new TextSegmentRef(2), "target segment 2 .".Split()),
			});

			var parallelText = new ParallelText(sourceText, targetText);
			ParallelTextSegment[] segments = parallelText.GetSegments(allSourceSegments: true).ToArray();
			Assert.That(segments.Length, Is.EqualTo(4));
			Assert.That(segments[1].SourceSegment, Is.EqualTo("source segment 2-1 .".Split()));
			Assert.That(segments[1].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
			Assert.That(segments[2].SourceSegment, Is.EqualTo("source segment 2-2 .".Split()));
			Assert.That(segments[2].TargetSegment, Is.EqualTo("target segment 2 .".Split()));
		}
	}
}
