using System;
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
						if (line.StartsWith("//"))
						{
							string sectionNumStr = line.Substring(2).Trim();
							if (!string.IsNullOrEmpty(sectionNumStr))
							{
								sectionNum = int.Parse(sectionNumStr, CultureInfo.InvariantCulture);
								segmentNum = 1;
							}
						}
						else
						{
							yield return new TextAlignment(new TextSegmentRef(sectionNum, segmentNum),
								ParseAlignments(line));
							segmentNum++;
						}
					}
				}
			}
		}

		private IEnumerable<AlignedWordPair> ParseAlignments(string alignments)
		{
			foreach (string token in alignments.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries))
			{
				int index = token.IndexOf('-');
				int i = int.Parse(token.Substring(0, index));
				int j = int.Parse(token.Substring(index + 1));
				yield return _invert ? new AlignedWordPair(j, i) : new AlignedWordPair(i, j);
			}
		}

		public ITextAlignmentCollection Invert()
		{
			return new TextFileTextAlignmentCollection(Id, _fileName, !_invert);
		}
	}
}
