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
				var rangeInfo = new RangeInfo(this);

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

							foreach (ParallelTextSegment seg in CreateTextSegments(rangeInfo, enumerator1.Current,
								null))
							{
								yield return seg;
							}
						}
						sourceCompleted = !enumerator1.MoveNext();
					}
					else if (compare1 > 0)
					{
						if (allTargetSegments)
						{
							foreach (ParallelTextSegment seg in CreateTextSegments(rangeInfo, null,
								enumerator2.Current))
							{
								yield return seg;
							}
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

						if ((!allTargetSegments && enumerator1.Current.IsInRange)
							|| (!allSourceSegments && enumerator2.Current.IsInRange))
						{

							if (rangeInfo.IsInRange
								&& ((enumerator1.Current.IsInRange && !enumerator2.Current.IsInRange
									&& enumerator1.Current.Segment.Count > 0)
								|| (!enumerator1.Current.IsInRange && enumerator2.Current.IsInRange
									&& enumerator2.Current.Segment.Count > 0)
								|| (enumerator1.Current.IsInRange && enumerator2.Current.IsInRange
									&& enumerator1.Current.Segment.Count > 0 && enumerator2.Current.Segment.Count > 0)))
							{
								yield return rangeInfo.CreateTextSegment();
							}

							if (!rangeInfo.IsInRange)
								rangeInfo.SegmentRef = enumerator1.Current.SegmentRef;
							rangeInfo.SourceSegment.AddRange(enumerator1.Current.Segment);
							rangeInfo.TargetSegment.AddRange(enumerator2.Current.Segment);
						}
						else
						{
							foreach (ParallelTextSegment seg in CreateTextSegments(rangeInfo, enumerator1.Current,
								enumerator2.Current, compare2 == 0 ? enumerator3.Current.AlignedWordPairs : null))
							{
								yield return seg;
							}
						}
						sourceCompleted = !enumerator1.MoveNext();
						targetCompleted = !enumerator2.MoveNext();
					}
				}

				if (allSourceSegments && !sourceCompleted)
				{
					do
					{
						foreach (ParallelTextSegment seg in CreateTextSegments(rangeInfo, enumerator1.Current, null))
							yield return seg;
					} while (enumerator1.MoveNext());
				}

				if (allTargetSegments && !targetCompleted)
				{
					do
					{
						foreach (ParallelTextSegment seg in CreateTextSegments(rangeInfo, null, enumerator2.Current))
							yield return seg;
					}
					while (enumerator2.MoveNext());
				}

				if (rangeInfo.IsInRange)
					yield return rangeInfo.CreateTextSegment();
			}
		}

		private IEnumerable<ParallelTextSegment> CreateTextSegments(RangeInfo rangeInfo, TextSegment srcSeg,
			TextSegment trgSeg, IEnumerable<AlignedWordPair> alignedWordPairs = null)
		{
			if (rangeInfo.IsInRange)
				yield return rangeInfo.CreateTextSegment();
			yield return new ParallelTextSegment(this, srcSeg != null ? srcSeg.SegmentRef : trgSeg.SegmentRef,
				srcSeg != null ? srcSeg.Segment : Array.Empty<string>(),
				trgSeg != null ? trgSeg.Segment : Array.Empty<string>(),
				alignedWordPairs, srcSeg != null && srcSeg.IsInRange, trgSeg != null && trgSeg.IsInRange);
		}

		private class RangeInfo
		{
			private readonly ParallelText _text;

			public RangeInfo(ParallelText text)
			{
				_text = text;
			}

			public object SegmentRef { get; set; }
			public List<string> SourceSegment { get; } = new List<string>();
			public List<string> TargetSegment { get; } = new List<string>();

			public bool IsInRange => SegmentRef != null;

			public ParallelTextSegment CreateTextSegment()
			{
				var seg = new ParallelTextSegment(_text, SegmentRef, SourceSegment.ToArray(), TargetSegment.ToArray());
				SegmentRef = null;
				SourceSegment.Clear();
				TargetSegment.Clear();
				return seg;
			}
		}
	}
}
