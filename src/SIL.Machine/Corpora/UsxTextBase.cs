using System.Collections.Generic;
using System.IO;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public abstract class UsxTextBase : ScriptureText
	{
		private readonly UsxVerseParser _parser;

		protected UsxTextBase(ITokenizer<string, int, string> wordTokenizer, string id, ScrVers versification)
			: base(wordTokenizer, id, versification)
		{
			_parser = new UsxVerseParser();
		}

		public override IEnumerable<TextSegment> GetSegments(bool includeText = true)
		{
			using (IStreamContainer streamContainer = CreateStreamContainer())
			using (Stream stream = streamContainer.OpenStream())
			{
				var prevVerseRef = new VerseRef();
				foreach (UsxVerse verse in _parser.Parse(stream))
				{
					foreach (TextSegment segment in CreateTextSegments(includeText, ref prevVerseRef, verse))
						yield return segment;
				}
			}
		}

		private IEnumerable<TextSegment> CreateTextSegments(bool includeText, ref VerseRef prevVerseRef, UsxVerse verse)
		{
			return CreateTextSegments(includeText, ref prevVerseRef, verse.Chapter, verse.Verse, verse.Text,
				verse.IsSentenceStart);
		}
	}
}
