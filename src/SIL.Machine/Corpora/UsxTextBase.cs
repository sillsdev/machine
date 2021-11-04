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

		protected override IEnumerable<TextSegment> GetSegmentsInDocOrder(bool includeText = true)
		{
			using (IStreamContainer streamContainer = CreateStreamContainer())
			using (Stream stream = streamContainer.OpenStream())
			{
				foreach (UsxVerse verse in _parser.Parse(stream))
				{
					foreach (TextSegment segment in CreateTextSegments(includeText, verse.Chapter, verse.Verse,
						verse.Text, verse.IsSentenceStart))
					{
						yield return segment;
					}
				}
			}
		}

		protected abstract IStreamContainer CreateStreamContainer();
	}
}
