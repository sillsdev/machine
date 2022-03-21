using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SIL.Machine.Corpora
{
	public class TextFileTextAlignmentCollection : ITextAlignmentCollection
	{
		private readonly string _fileName;

		public TextFileTextAlignmentCollection(string id, string fileName)
		{
			Id = id;
			_fileName = fileName;
		}

		public string Id { get; }

		public string SortKey => Id;

		public IEnumerable<TextAlignmentCorpusRow> GetRows()
		{
			using (var reader = new StreamReader(_fileName))
			{
				int sectionNum = 1;
				int segmentNum = 1;
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					if (line.StartsWith("// section "))
					{
						string sectionNumStr = line.Substring(11).Trim();
						if (!string.IsNullOrEmpty(sectionNumStr))
						{
							sectionNum = int.Parse(sectionNumStr, CultureInfo.InvariantCulture);
							segmentNum = 1;
						}
					}
					else
					{
						var rowRef = new RowRef(Id, sectionNum.ToString(), segmentNum.ToString());
						yield return new TextAlignmentCorpusRow(Id, rowRef)
						{
							AlignedWordPairs = AlignedWordPair.Parse(line)
						};
						segmentNum++;
					}
				}
			}

		}
	}
}
