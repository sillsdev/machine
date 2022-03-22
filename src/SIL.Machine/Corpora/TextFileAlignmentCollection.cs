using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SIL.Machine.Corpora
{
	public class TextFileAlignmentCollection : IAlignmentCollection
	{
		private readonly string _fileName;

		public TextFileAlignmentCollection(string id, string fileName)
		{
			Id = id;
			_fileName = fileName;
		}

		public string Id { get; }

		public string SortKey => Id;

		public IEnumerable<AlignmentRow> GetRows()
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
						var keys = new List<string>();
						if (Id != "*all*")
							keys.Add(Id);
						keys.Add(sectionNum.ToString(CultureInfo.InvariantCulture));
						keys.Add(segmentNum.ToString(CultureInfo.InvariantCulture));
						var rowRef = new RowRef(keys);
						yield return new AlignmentRow(Id, rowRef)
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
