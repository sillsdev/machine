using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class ParallelText
	{
		private readonly IComparer<object> _segmentRefComparer;

		public ParallelText(IText sourceText, IText targetText, ITextAlignmentCollection textAlignmentCollection,
			IComparer<object> segmentRefComparer = null)
		{
			SourceText = sourceText;
			TargetText = targetText;
			TextAlignmentCollection = textAlignmentCollection;
			_segmentRefComparer = segmentRefComparer ?? new DefaultSegmentRefComparer();
		}

		public string Id => SourceText.Id;

		public string SortKey => SourceText.SortKey;

		public IText SourceText { get; }

		public IText TargetText { get; }

		public ITextAlignmentCollection TextAlignmentCollection { get; }

		public IEnumerable<ParallelTextSegment> Segments => GetSegments();

		public IEnumerable<ParallelTextSegment> GetSegments(bool allSourceSegments = false,
			bool allTargetSegments = false, bool includeText = true)
		{
			using (IEnumerator<TextSegment> srcEnumerator = SourceText.GetSegments(includeText).GetEnumerator())
			using (IEnumerator<TextSegment> trgEnumerator = TargetText.GetSegmentsBasedOn(SourceText, includeText)
				.GetEnumerator())
			using (IEnumerator<TextAlignment> alignmentEnumerator = TextAlignmentCollection.Alignments.GetEnumerator())
			{
				var rangeInfo = new RangeInfo(this);
				var sourceSameRefSegments = new List<TextSegment>();
				var targetSameRefSegments = new List<TextSegment>();

				bool srcCompleted = !srcEnumerator.MoveNext();
				bool trgCompleted = !trgEnumerator.MoveNext();
				while (!srcCompleted && !trgCompleted)
				{
					int compare1 = _segmentRefComparer.Compare(srcEnumerator.Current.SegmentRef,
						trgEnumerator.Current.SegmentRef);
					if (compare1 < 0)
					{
						foreach (ParallelTextSegment seg in CreateSourceTextSegments(rangeInfo,
							srcEnumerator.Current, targetSameRefSegments, allSourceSegments))
						{
							yield return seg;
						}

						sourceSameRefSegments.Add(srcEnumerator.Current);
						srcCompleted = !srcEnumerator.MoveNext();
					}
					else if (compare1 > 0)
					{
						foreach (ParallelTextSegment seg in CreateTargetTextSegments(rangeInfo,
							trgEnumerator.Current, sourceSameRefSegments, allTargetSegments))
						{
							yield return seg;
						}

						targetSameRefSegments.Add(trgEnumerator.Current);
						trgCompleted = !trgEnumerator.MoveNext();
					}
					else
					{
						int compare2;
						do
						{
							compare2 = alignmentEnumerator.MoveNext()
								? _segmentRefComparer.Compare(srcEnumerator.Current.SegmentRef,
									alignmentEnumerator.Current.SegmentRef)
								: 1;
						} while (compare2 < 0);

						if ((!allTargetSegments && srcEnumerator.Current.IsInRange)
							|| (!allSourceSegments && trgEnumerator.Current.IsInRange))
						{

							if (rangeInfo.IsInRange
								&& ((srcEnumerator.Current.IsInRange && !trgEnumerator.Current.IsInRange
									&& srcEnumerator.Current.Segment.Count > 0)
								|| (!srcEnumerator.Current.IsInRange && trgEnumerator.Current.IsInRange
									&& trgEnumerator.Current.Segment.Count > 0)
								|| (srcEnumerator.Current.IsInRange && trgEnumerator.Current.IsInRange
									&& srcEnumerator.Current.Segment.Count > 0 && trgEnumerator.Current.Segment.Count > 0)))
							{
								yield return rangeInfo.CreateTextSegment();
							}

							if (!rangeInfo.IsInRange)
								rangeInfo.SegmentRef = srcEnumerator.Current.SegmentRef;
							rangeInfo.SourceSegment.AddRange(srcEnumerator.Current.Segment);
							rangeInfo.TargetSegment.AddRange(trgEnumerator.Current.Segment);
							if (rangeInfo.IsSourceEmpty)
								rangeInfo.IsSourceEmpty = srcEnumerator.Current.IsEmpty;
							if (rangeInfo.IsTargetEmpty)
								rangeInfo.IsTargetEmpty = trgEnumerator.Current.IsEmpty;
						}
						else
						{
							if (CheckSameRefSegments(sourceSameRefSegments, trgEnumerator.Current))
							{
								foreach (TextSegment prevSourceSegment in sourceSameRefSegments)
								{
									foreach (ParallelTextSegment seg in CreateTextSegments(rangeInfo, prevSourceSegment,
										trgEnumerator.Current))
									{
										yield return seg;
									}
								}
							}

							if (CheckSameRefSegments(targetSameRefSegments, srcEnumerator.Current))
							{
								foreach (TextSegment prevTargetSegment in targetSameRefSegments)
								{
									foreach (ParallelTextSegment seg in CreateTextSegments(rangeInfo,
										srcEnumerator.Current, prevTargetSegment))
									{
										yield return seg;
									}
								}
							}

							foreach (ParallelTextSegment seg in CreateTextSegments(rangeInfo, srcEnumerator.Current,
								trgEnumerator.Current, compare2 == 0 ? alignmentEnumerator.Current.AlignedWordPairs : null))
							{
								yield return seg;
							}
						}

						sourceSameRefSegments.Add(srcEnumerator.Current);
						srcCompleted = !srcEnumerator.MoveNext();

						targetSameRefSegments.Add(trgEnumerator.Current);
						trgCompleted = !trgEnumerator.MoveNext();
					}
				}

				while (!srcCompleted)
				{
					foreach (ParallelTextSegment seg in CreateSourceTextSegments(rangeInfo, srcEnumerator.Current,
						targetSameRefSegments, allSourceSegments))
					{
						yield return seg;
					}
					srcCompleted = !srcEnumerator.MoveNext();
				}

				while (!trgCompleted)
				{
					foreach (ParallelTextSegment seg in CreateTargetTextSegments(rangeInfo, trgEnumerator.Current,
						sourceSameRefSegments, allTargetSegments))
					{
						yield return seg;
					}
					trgCompleted = !trgEnumerator.MoveNext();
				}

				if (rangeInfo.IsInRange)
					yield return rangeInfo.CreateTextSegment();
			}
		}

		public int GetCount(bool allSourceSegments = false, bool allTargetSegments = false, bool nonemptyOnly = false)
		{
			return GetSegments(allSourceSegments, allTargetSegments, includeText: false)
				.Count(s => !nonemptyOnly || !s.IsEmpty);
		}

		private IEnumerable<ParallelTextSegment> CreateTextSegments(RangeInfo rangeInfo, TextSegment srcSeg,
			TextSegment trgSeg, IReadOnlyCollection<AlignedWordPair> alignedWordPairs = null)
		{
			if (rangeInfo.IsInRange)
				yield return rangeInfo.CreateTextSegment();
			yield return new ParallelTextSegment(Id,
				srcSeg != null ? srcSeg.SegmentRef : trgSeg.SegmentRef,
				srcSeg != null ? srcSeg.Segment : Array.Empty<string>(),
				trgSeg != null ? trgSeg.Segment : Array.Empty<string>(),
				alignedWordPairs,
				srcSeg != null && srcSeg.IsInRange,
				srcSeg != null && srcSeg.IsRangeStart, trgSeg != null && trgSeg.IsInRange,
				trgSeg != null && trgSeg.IsRangeStart,
				srcSeg == null || srcSeg.IsEmpty || trgSeg == null || trgSeg.IsEmpty);
		}

		private bool CheckSameRefSegments(List<TextSegment> sameRefSegments, TextSegment otherSegment)
		{
			if (sameRefSegments.Count > 0
				&& _segmentRefComparer.Compare(sameRefSegments[0].SegmentRef, otherSegment.SegmentRef) != 0)
			{
				sameRefSegments.Clear();
			}

			return sameRefSegments.Count > 0;
		}

		private IEnumerable<ParallelTextSegment> CreateSourceTextSegments(RangeInfo rangeInfo,
			TextSegment sourceSegment, List<TextSegment> targetSameRefSegments, bool allSourceSegments)
		{
			if (CheckSameRefSegments(targetSameRefSegments, sourceSegment))
			{
				foreach (TextSegment targetSameRefSegment in targetSameRefSegments)
				{
					foreach (ParallelTextSegment seg in CreateTextSegments(rangeInfo, sourceSegment,
						targetSameRefSegment))
					{
						yield return seg;
					}
				}
			}
			else if (allSourceSegments)
			{
				foreach (ParallelTextSegment seg in CreateTextSegments(rangeInfo, sourceSegment, null))
					yield return seg;
			}
		}

		private IEnumerable<ParallelTextSegment> CreateTargetTextSegments(RangeInfo rangeInfo,
			TextSegment targetSegment, List<TextSegment> sourceSameRefSegments, bool allTargetSegments)
		{
			if (CheckSameRefSegments(sourceSameRefSegments, targetSegment))
			{
				foreach (TextSegment sourceSameRefSegment in sourceSameRefSegments)
				{
					foreach (ParallelTextSegment seg in CreateTextSegments(rangeInfo, sourceSameRefSegment,
						targetSegment))
					{
						yield return seg;
					}
				}
			}
			else if (allTargetSegments)
			{
				foreach (ParallelTextSegment seg in CreateTextSegments(rangeInfo, null, targetSegment))
					yield return seg;
			}
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
			public bool IsSourceEmpty { get; set; } = true;
			public bool IsTargetEmpty { get; set; } = true;

			public ParallelTextSegment CreateTextSegment()
			{
				var seg = new ParallelTextSegment(_text.Id, SegmentRef, SourceSegment.ToArray(),
					TargetSegment.ToArray(), alignedWordPairs: null, isSourceInRange: false, isSourceRangeStart: false,
					isTargetInRange: false, isTargetRangeStart: false, isEmpty: IsSourceEmpty || IsTargetEmpty);
				SegmentRef = null;
				SourceSegment.Clear();
				TargetSegment.Clear();
				return seg;
			}
		}

		private class DefaultSegmentRefComparer : IComparer<object>
		{
			private static readonly VerseRefComparer VerseRefComparer = new VerseRefComparer(compareSegments: false);

			public int Compare(object x, object y)
			{
				// Do not use the default comparer for VerseRef, since we want to compare all verses in a range or
				// sequence
				if (x is VerseRef vx && y is VerseRef vy)
					return VerseRefComparer.Compare(vx, vy);

				return Comparer<object>.Default.Compare(x, y);
			}
		}
	}
}
