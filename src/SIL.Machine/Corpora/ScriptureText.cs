using System.Collections.Generic;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public abstract class ScriptureText : TextBase
	{
		protected ScriptureText(string id, ScrVers versification)
			: base(id, CorporaUtils.GetScriptureTextSortKey(id))
		{
			Versification = versification ?? ScrVers.English;
		}

		public ScrVers Versification { get; }

		public override IEnumerable<TextRow> GetRows()
		{
			var rowList = new List<TextRow>();
			bool outOfOrder = false;
			var prevVerseRef = new VerseRef();
			foreach (TextRow r in GetVersesInDocOrder())
			{
				TextRow row = r;
				var verseRef = (VerseRef)row.Ref;
				rowList.Add(row);
				if (!outOfOrder && verseRef.CompareTo(prevVerseRef) < 0)
					outOfOrder = true;
				prevVerseRef = verseRef;
			}

			if (outOfOrder)
				rowList.Sort((x, y) => ((VerseRef)x.Ref).CompareTo(y.Ref));
			return rowList;
		}

		protected abstract IEnumerable<TextRow> GetVersesInDocOrder();

		protected IEnumerable<TextRow> CreateRows(string chapter, string verse,
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
