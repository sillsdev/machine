using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SIL.Machine.Corpora
{
	public class TextFileText : TextBase
	{
		public TextFileText(string id, string fileName)
			: base(id, id)
		{
			FileName = fileName;
		}

		public string FileName { get; }

		public override IEnumerable<TextCorpusRow> GetRows()
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
						yield return CreateRow(line, new RowRef(Id, sectionNum.ToString(), segmentNum.ToString()));
						segmentNum++;
					}
				}
			}
		}
	}
}
