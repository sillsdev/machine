using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SIL.Machine.Corpora
{
	public class TextFileTextAlignmentCollection : ITextAlignmentCollection
	{
		private readonly string _fileName;
		private readonly bool _invert;

		public TextFileTextAlignmentCollection(string id, string fileName, bool invert = false)
		{
			Id = id;
			_fileName = fileName;
			_invert = invert;
		}

		public string Id { get; }

		public string SortKey => Id;

		public IEnumerable<TextAlignment> Alignments
		{
			get
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
							yield return new TextAlignment(new TextSegmentRef(sectionNum, segmentNum),
								AlignedWordPair.Parse(line, _invert));
							segmentNum++;
						}
					}
				}
			}
		}

		public ITextAlignmentCollection Invert()
		{
			return new TextFileTextAlignmentCollection(Id, _fileName, !_invert);
		}
	}
}
