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
				var sourceSameRefSegments = new List<TextSegment>();
				var targetSameRefSegments = new List<TextSegment>();

				bool sourceCompleted = !enumerator1.MoveNext();
				bool targetCompleted = !enumerator2.MoveNext();
				while (!sourceCompleted && !targetCompleted)
				{
					int compare1 = _segmentRefComparer.Compare(enumerator1.Current.SegmentRef,
						enumerator2.Current.SegmentRef);
					if (compare1 < 0)
					{
						foreach (ParallelTextSegment seg in CreateSourceTextSegments(rangeInfo,
							enumerator1.Current, targetSameRefSegments, allSourceSegments))
						{
							yield return seg;
						}

						sourceSameRefSegments.Add(enumerator1.Current);
						sourceCompleted = !enumerator1.MoveNext();
					}
					else if (compare1 > 0)
					{
						foreach (ParallelTextSegment seg in CreateTargetTextSegments(rangeInfo,
							enumerator2.Current, sourceSameRefSegments, allTargetSegments))
						{
							yield return seg;
						}

						targetSameRefSegments.Add(enumerator2.Current);
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
							if (CheckSameRefSegments(sourceSameRefSegments, enumerator2.Current))
							{
								foreach (TextSegment prevSourceSegment in sourceSameRefSegments)
								{
									foreach (ParallelTextSegment seg in CreateTextSegments(rangeInfo, prevSourceSegment,
										enumerator2.Current))
									{
										yield return seg;
									}
								}
							}

							if (CheckSameRefSegments(targetSameRefSegments, enumerator1.Current))
							{
								foreach (TextSegment prevTargetSegment in targetSameRefSegments)
								{
									foreach (ParallelTextSegment seg in CreateTextSegments(rangeInfo,
										enumerator1.Current, prevTargetSegment))
									{
										yield return seg;
									}
								}
							}

							foreach (ParallelTextSegment seg in CreateTextSegments(rangeInfo, enumerator1.Current,
								enumerator2.Current, compare2 == 0 ? enumerator3.Current.AlignedWordPairs : null))
							{
								yield return seg;
							}
						}

						sourceSameRefSegments.Add(enumerator1.Current);
						sourceCompleted = !enumerator1.MoveNext();

						targetSameRefSegments.Add(enumerator2.Current);
						targetCompleted = !enumerator2.MoveNext();
					}
				}

				while (!sourceCompleted)
				{
					foreach (ParallelTextSegment seg in CreateSourceTextSegments(rangeInfo, enumerator1.Current,
						targetSameRefSegments, allSourceSegments))
					{
						yield return seg;
					}
					sourceCompleted = !enumerator1.MoveNext();
				}

				while (!targetCompleted)
				{
					foreach (ParallelTextSegment seg in CreateTargetTextSegments(rangeInfo, enumerator2.Current,
						sourceSameRefSegments, allTargetSegments))
					{
						yield return seg;
					}
					targetCompleted = !enumerator2.MoveNext();
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
				alignedWordPairs, srcSeg != null && srcSeg.IsInRange, srcSeg != null && srcSeg.IsRangeStart,
				trgSeg != null && trgSeg.IsInRange, trgSeg != null && trgSeg.IsRangeStart);
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
