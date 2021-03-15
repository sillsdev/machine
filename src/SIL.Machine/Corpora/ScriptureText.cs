using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public abstract class ScriptureText : StreamTextBase
	{
		protected ScriptureText(ITokenizer<string, int, string> wordTokenizer, string id, ScrVers versification)
			: base(wordTokenizer, id, CorporaHelpers.GetScriptureTextSortKey(id))
		{
			Versification = versification ?? ScrVers.English;
		}

		public ScrVers Versification { get; }

		protected IEnumerable<TextSegment> CreateTextSegments(ref VerseRef prevVerseRef, string chapter, string verse,
			string text, bool sentenceStart = true)
		{
			var verseRef = new VerseRef(Id, chapter, verse, Versification);
			if (verseRef.CompareTo(prevVerseRef) <= 0)
				return Enumerable.Empty<TextSegment>();

			prevVerseRef = verseRef;
			return CreateTextSegments(verseRef, text, sentenceStart);
		}

		private IEnumerable<TextSegment> CreateTextSegments(VerseRef verseRef, string text, bool sentenceStart)
		{
			if (verseRef.HasMultiple)
			{
				bool firstVerse = true;
				foreach (VerseRef vref in verseRef.AllVerses())
				{
					if (firstVerse)
					{
						yield return CreateTextSegment(text, vref, sentenceStart, inRange: true, rangeStart: true);
						firstVerse = false;
					}
					else
					{
						yield return CreateTextSegment(vref, inRange: true);
					}
				}
			}
			else
			{
				yield return CreateTextSegment(text, verseRef, sentenceStart);
			}
		}
	}
}
