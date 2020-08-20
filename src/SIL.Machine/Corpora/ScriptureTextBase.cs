using System.Collections.Generic;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public abstract class ScriptureTextBase : StreamTextBase
	{
		protected ScriptureTextBase(ITokenizer<string, int, string> wordTokenizer, string id, ScrVers versification)
			: base(wordTokenizer, id, CorporaHelpers.GetScriptureTextSortKey(id))
		{
			Versification = versification ?? ScrVers.English;
		}

		public ScrVers Versification { get; }

		protected IEnumerable<TextSegment> CreateTextSegments(string chapter, string verse, string text,
			bool sentenceStart = true)
		{
			var verseRef = new VerseRef(Id, chapter, verse, Versification);
			if (verseRef.HasMultiple)
			{
				bool firstVerse = true;
				foreach (VerseRef vref in verseRef.AllVerses())
				{
					if (firstVerse)
					{
						yield return CreateTextSegment(text, vref, true, sentenceStart);
						firstVerse = false;
					}
					else
					{
						yield return CreateTextSegment(vref, true);
					}
				}
			}
			else
			{
				yield return CreateTextSegment(text, verseRef, sentenceStart: sentenceStart);
			}
		}
	}
}
