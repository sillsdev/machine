using System.Collections.Generic;
using System.IO;
using System.Text;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public abstract class UsfmTextBase : StreamTextBase
	{
		private readonly UsfmParser _parser;
		private readonly Encoding _encoding;

		protected UsfmTextBase(ITokenizer<string, int> wordTokenizer, string id, UsfmStylesheet stylesheet,
			Encoding encoding, ScrVers versification)
			: base(wordTokenizer, id)
		{
			_parser = new UsfmParser(stylesheet);
			_encoding = encoding;
			Versification = versification ?? ScrVers.English;
		}

		public ScrVers Versification { get; }

		public override IEnumerable<TextSegment> Segments
		{
			get
			{
				string usfm;
				using (IStreamContainer streamContainer = CreateStreamContainer())
				using (var reader = new StreamReader(streamContainer.OpenStream(), _encoding))
				{
					usfm = reader.ReadToEnd();
				}
				bool inVerse = false;
				var sb = new StringBuilder();
				string chapter = null, verse = null;
				foreach (UsfmToken token in _parser.Parse(usfm))
				{
					switch (token.Type)
					{
						case UsfmTokenType.Chapter:
							if (inVerse)
							{
								yield return CreateTextSegment(sb.ToString(),
									new VerseRef(Id, chapter, verse, Versification));
								sb.Clear();
								inVerse = false;
							}
							chapter = token.Text;
							verse = null;
							break;

						case UsfmTokenType.Verse:
							if (inVerse)
							{
								yield return CreateTextSegment(sb.ToString(),
									new VerseRef(Id, chapter, verse, Versification));
								sb.Clear();
							}
							else
							{
								inVerse = true;
							}
							verse = token.Text;
							break;

						case UsfmTokenType.Text:
							if (inVerse && !string.IsNullOrEmpty(token.Text))
								sb.Append(token.Text);
							break;
					}
				}

				if (inVerse)
					yield return CreateTextSegment(sb.ToString(), new VerseRef(Id, chapter, verse, Versification));
			}
		}
	}
}
