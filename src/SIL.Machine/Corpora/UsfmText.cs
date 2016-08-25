using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace SIL.Machine.Corpora
{
	public class UsfmText : IText
	{
		private readonly string _fileName;
		private readonly UsfmParser _parser;
		private readonly Encoding _encoding;

		public UsfmText(UsfmStylesheet stylesheet, Encoding encoding, string fileName)
		{
			_fileName = fileName;
			_parser = new UsfmParser(stylesheet);
			_encoding = encoding;
			string name = Path.GetFileNameWithoutExtension(fileName);
			Debug.Assert(name != null);
			Id = name.Substring(0, name.StartsWith("100") ? 6 : 5);
		}

		public string Id { get; }

		public IEnumerable<TextSegment> Segments
		{
			get
			{
				bool inVerse = false;
				string usfm = File.ReadAllText(_fileName, _encoding);
				var sb = new StringBuilder();
				int chapter = 0, verse = 0;
				foreach (UsfmToken token in _parser.Parse(usfm))
				{
					switch (token.Type)
					{
						case UsfmTokenType.Chapter:
							if (inVerse)
							{
								yield return new TextSegment(new TextSegmentRef(chapter, verse), sb.ToString().Trim());
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
								yield return new TextSegment(new TextSegmentRef(chapter, verse), sb.ToString().Trim());
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
					yield return new TextSegment(new TextSegmentRef(chapter, verse), sb.ToString().Trim());
			}
		}
	}
}
