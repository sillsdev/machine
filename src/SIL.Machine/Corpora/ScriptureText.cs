using System.Collections.Generic;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public abstract class ScriptureText : TextBase
	{
		protected ScriptureText(string id, ScrVers versification)
			: base(id, CorporaHelpers.GetScriptureTextSortKey(id))
		{
			Versification = versification ?? ScrVers.English;
		}

		public ScrVers Versification { get; }

		public override IEnumerable<TextCorpusRow> GetRows()
		{
			return GetRows();
		}

		public IEnumerable<TextCorpusRow> GetRows(ScrVers versification = null)
		{
			var rowList = new List<(VerseRef Ref, TextCorpusRow Row)>();
			bool outOfOrder = false;
			var prevVerseRef = new VerseRef();
			int rangeStartOffset = -1;
			foreach (TextCorpusRow r in GetVersesInDocOrder())
			{
				TextCorpusRow row = r;
				var verseRef = (VerseRef)row.Ref;
				if (versification != null && versification != Versification)
				{
					verseRef.ChangeVersification(versification);
					// convert on-to-many versification mapping to a verse range
					if (verseRef.Equals(prevVerseRef))
					{
						var (rangeStartVerseRef, rangeStartSeg) = rowList[rowList.Count + rangeStartOffset];
						bool isRangeStart = false;
						if (rangeStartOffset == -1)
							isRangeStart = !rangeStartSeg.IsInRange || rangeStartSeg.IsRangeStart;
						rowList[rowList.Count + rangeStartOffset] = (rangeStartVerseRef,
							new TextCorpusRow(Id, rangeStartSeg.Ref)
							{
								Segment = rangeStartSeg.Segment.Concat(row.Segment).ToArray(),
								IsSentenceStart = rangeStartSeg.IsSentenceStart,
								IsInRange = true,
								IsRangeStart = isRangeStart,
								IsEmpty = rangeStartSeg.IsEmpty && row.IsEmpty
							});
						row = CreateEmptyRow(row.Ref, isInRange: true);
						rangeStartOffset--;
					}
					else
					{
						rangeStartOffset = -1;
					}
				}
				rowList.Add((verseRef, row));
				if (!outOfOrder && verseRef.CompareTo(prevVerseRef) < 0)
					outOfOrder = true;
				prevVerseRef = verseRef;
			}

			if (outOfOrder)
				rowList.Sort((x, y) => x.Ref.CompareTo(y.Ref));

			return rowList.Select(t => t.Row);
		}

		protected abstract IEnumerable<TextCorpusRow> GetVersesInDocOrder();

		protected IEnumerable<TextCorpusRow> CreateRows(string chapter, string verse,
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
						yield return CreateRow(text, vref, sentenceStart, isInRange: true,
							isRangeStart: true);
						firstVerse = false;
					}
					else
					{
						yield return CreateEmptyRow(vref, isInRange: true);
					}
				}
			}
			else
			{
				yield return CreateRow(text, verseRef, sentenceStart);
			}
		}
	}
}
