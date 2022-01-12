using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Corpora
{
	public class TextFileText : TextBase
	{
		public TextFileText(ITokenizer<string, int, string> wordTokenizer, string id, string fileName)
			: base(wordTokenizer, id, id)
		{
			FileName = fileName;
		}

		public string FileName { get; }

		public override IEnumerable<TextSegment> GetSegments(bool includeText = true, IText sortBasedOn = null)
		{
			using (var reader = new StreamReader(FileName))
			{
				int sectionNum = 1;
				int segmentNum = 1;
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					if (line.StartsWith("// section ", StringComparison.InvariantCultureIgnoreCase))
					{
						string sectionNumStr = line.Substring(11).Trim();
						if (!string.IsNullOrEmpty(sectionNumStr))
						{
							if (int.TryParse(sectionNumStr, NumberStyles.Integer, CultureInfo.InvariantCulture,
								out int num))
							{
								sectionNum = num;
								segmentNum = 1;
							}
						}
					}
					else
					{
						yield return CreateTextSegment(includeText, line, new TextSegmentRef(sectionNum, segmentNum));
						segmentNum++;
					}
				}
			}
		}
	}
}
