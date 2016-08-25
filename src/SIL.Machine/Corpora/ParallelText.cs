using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public class ParallelText
	{
		public ParallelText(IText sourceText, IText targetText)
		{
			SourceText = sourceText;
			TargetText = targetText;
		}

		public string Id => SourceText.Id;

		public IText SourceText { get; }

		public IText TargetText { get; }

		public IEnumerable<ParallelTextSegment> Segments
		{
			get
			{
				IEnumerator<TextSegment> enumerator1 = SourceText.Segments.GetEnumerator();
				IEnumerator<TextSegment> enumerator2 = TargetText.Segments.GetEnumerator();

				bool completed = !enumerator1.MoveNext() || !enumerator2.MoveNext();
				while (!completed)
				{
					int compare = enumerator1.Current.SegmentRef.CompareTo(enumerator2.Current.SegmentRef);
					if (compare < 0)
					{
						completed = !enumerator1.MoveNext();
					}
					else if (compare > 0)
					{
						completed = !enumerator2.MoveNext();
					}
					else
					{
						yield return new ParallelTextSegment(enumerator1.Current.SegmentRef, enumerator1.Current.Value, enumerator2.Current.Value);
						completed = !enumerator1.MoveNext() || !enumerator2.MoveNext();
					}
				}
			}
		}
	}
}
