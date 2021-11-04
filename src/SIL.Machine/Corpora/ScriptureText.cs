using System.Collections.Generic;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public abstract class ScriptureText : TextBase
	{
		protected ScriptureText(ITokenizer<string, int, string> wordTokenizer, string id, ScrVers versification)
			: base(wordTokenizer, id, CorporaHelpers.GetScriptureTextSortKey(id))
		{
			Versification = versification ?? ScrVers.English;
		}

		public ScrVers Versification { get; }

		public override IEnumerable<TextSegment> GetSegments(bool includeText = true, IText basedOn = null)
		{
			ScrVers basedOnVers = null;
			if (basedOn is ScriptureText scriptureText && Versification != scriptureText.Versification)
				basedOnVers = scriptureText.Versification;
			var segList = new List<TextSegment>();
			bool outOfOrder = false;
			var prevVerseRef = new VerseRef();
			foreach (TextSegment seg in GetSegmentsInDocOrder(includeText))
			{
				var verseRef = (VerseRef)seg.SegmentRef;
				TextSegment newSeg = seg;
				if (basedOnVers != null)
				{
					verseRef.ChangeVersification(basedOnVers);
					newSeg = new TextSegment(seg.TextId, verseRef, seg.Segment, seg.IsSentenceStart, seg.IsInRange,
						seg.IsRangeStart, seg.IsEmpty);
				}
				segList.Add(newSeg);
				if (verseRef.CompareTo(prevVerseRef) < 0)
					outOfOrder = true;
				prevVerseRef = verseRef;
			}

			if (outOfOrder)
				segList.Sort((x, y) => ((VerseRef)x.SegmentRef).CompareTo(y.SegmentRef));

			return segList;
		}

		protected abstract IEnumerable<TextSegment> GetSegmentsInDocOrder(bool includeText);

		protected IEnumerable<TextSegment> CreateTextSegments(bool includeText, string chapter, string verse,
			string text, bool sentenceStart = true)
		{
			var verseRef = new VerseRef(Id, chapter, verse, Versification);
			if (verseRef.HasMultiple)
			{
				bool firstVerse = true;
				foreach (VerseRef vref in verseRef.AllVerses())
				{
					if (firstVerse)
					{
						yield return CreateTextSegment(includeText, text, vref, sentenceStart, isInRange: true,
							isRangeStart: true);
						firstVerse = false;
					}
					else
					{
						yield return CreateEmptyTextSegment(vref, isInRange: true);
					}
				}
			}
			else
			{
				yield return CreateTextSegment(includeText, text, verseRef, sentenceStart);
			}
		}
	}
}
