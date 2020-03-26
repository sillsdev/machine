using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class ParallelText
	{
		private readonly IComparer<object> _segmentRefComparer;

		public ParallelText(IText sourceText, IText targetText, ITextAlignmentCollection textAlignmentCollection = null,
			IComparer<object> segmentRefComparer = null)
		{
			SourceText = sourceText;
			TargetText = targetText;
			TextAlignmentCollection = textAlignmentCollection;
			_segmentRefComparer = segmentRefComparer ?? Comparer<object>.Default;
		}

		public string Id => SourceText.Id;

		public string SortKey => SourceText.SortKey;

		public IText SourceText { get; }

		public IText TargetText { get; }

		public ITextAlignmentCollection TextAlignmentCollection { get; }

		public IEnumerable<ParallelTextSegment> Segments => GetSegments();

		public IEnumerable<ParallelTextSegment> GetSegments(bool allSourceSegments = false,
			bool allTargetSegments = false)
		{
			IEnumerable<TextAlignment> alignments = TextAlignmentCollection?.Alignments
				?? Enumerable.Empty<TextAlignment>();

			using (IEnumerator<TextSegment> enumerator1 = SourceText.Segments.GetEnumerator())
			using (IEnumerator<TextSegment> enumerator2 = TargetText.Segments.GetEnumerator())
			using (IEnumerator<TextAlignment> enumerator3 = alignments.GetEnumerator())
			{
				bool sourceCompleted = !enumerator1.MoveNext();
				bool targetCompleted = !enumerator2.MoveNext();
				while (!sourceCompleted && !targetCompleted)
				{
					int compare1 = _segmentRefComparer.Compare(enumerator1.Current.SegmentRef,
						enumerator2.Current.SegmentRef);
					if (compare1 < 0)
					{
						if (allSourceSegments)
						{
							yield return new ParallelTextSegment(this, enumerator1.Current.SegmentRef,
								enumerator1.Current.Segment, Array.Empty<string>());
						}
						sourceCompleted = !enumerator1.MoveNext();
					}
					else if (compare1 > 0)
					{
						if (allTargetSegments)
						{
							yield return new ParallelTextSegment(this, enumerator2.Current.SegmentRef,
								Array.Empty<string>(), enumerator2.Current.Segment);
						}
						targetCompleted = !enumerator2.MoveNext();
					}
					else
					{
						int compare2;
						do
						{
							compare2 = enumerator3.MoveNext()
								? _segmentRefComparer.Compare(enumerator1.Current.SegmentRef,
									enumerator3.Current.SegmentRef)
								: 1;
						} while (compare2 < 0);

						yield return new ParallelTextSegment(this, enumerator1.Current.SegmentRef,
							enumerator1.Current.Segment, enumerator2.Current.Segment,
							compare2 == 0 ? enumerator3.Current.AlignedWordPairs : null);
						sourceCompleted = !enumerator1.MoveNext();
						targetCompleted = !enumerator2.MoveNext();
					}
				}

				if (allSourceSegments && !sourceCompleted)
				{
					do
					{
						yield return new ParallelTextSegment(this, enumerator1.Current.SegmentRef,
							enumerator1.Current.Segment, Array.Empty<string>());
					} while (enumerator1.MoveNext());
				}

				if (allTargetSegments && !targetCompleted)
				{
					do
					{
						yield return new ParallelTextSegment(this, enumerator2.Current.SegmentRef,
							Array.Empty<string>(), enumerator2.Current.Segment);
					}
					while (enumerator2.MoveNext());
				}
			}
		}
	}
}
