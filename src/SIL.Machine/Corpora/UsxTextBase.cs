﻿using System.Collections.Generic;
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

		public override IEnumerable<TextSegment> Segments
		{
			get
			{
				using (IStreamContainer streamContainer = CreateStreamContainer())
				using (Stream stream = streamContainer.OpenStream())
				{
					foreach (UsxVerse verse in _parser.Parse(stream))
					{
						foreach (TextSegment segment in CreateTextSegments(verse))
							yield return segment;
					}
				}
			}
		}

		private IEnumerable<TextSegment> CreateTextSegments(UsxVerse verse)
		{
			return CreateTextSegments(verse.Chapter, verse.Verse, verse.Text, verse.SentenceStart);
		}
	}
}
