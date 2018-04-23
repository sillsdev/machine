using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Corpora
{
	public abstract class UsfmTextBase : StreamTextBase
	{
		private readonly UsfmParser _parser;
		private readonly Encoding _encoding;

		protected UsfmTextBase(ITokenizer<string, int> wordTokenizer, string id, UsfmStylesheet stylesheet,
			Encoding encoding)
			: base(wordTokenizer, id)
		{
			_parser = new UsfmParser(stylesheet);
			_encoding = encoding;
		}

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
				int chapter = 0, verse = 0;
				foreach (UsfmToken token in _parser.Parse(usfm))
				{
					switch (token.Type)
					{
						case UsfmTokenType.Chapter:
							if (inVerse)
							{
								yield return CreateTextSegment(sb.ToString(), chapter, verse);
								sb.Clear();
								inVerse = false;
							}
							int nextChapter = int.Parse(token.Text, CultureInfo.InvariantCulture);
							if (nextChapter < chapter)
								yield break;
							chapter = nextChapter;
							verse = 0;
							break;

						case UsfmTokenType.Verse:
							if (inVerse)
							{
								yield return CreateTextSegment(sb.ToString(), chapter, verse);
								sb.Clear();
							}
							else
							{
								inVerse = true;
							}

							int nextVerse = int.Parse(token.Text, CultureInfo.InvariantCulture);
							if (nextVerse < verse)
								yield break;
							verse = nextVerse;
							break;

						case UsfmTokenType.Text:
							if (inVerse && !string.IsNullOrEmpty(token.Text))
								sb.Append(token.Text);
							break;
					}
				}

				if (inVerse)
					yield return CreateTextSegment(sb.ToString(), chapter, verse);
			}
		}
	}
}
