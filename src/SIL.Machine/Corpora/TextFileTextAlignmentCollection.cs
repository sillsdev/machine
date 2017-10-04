using System;
using System.Collections.Generic;
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

		public IEnumerable<TextAlignment> Alignments
		{
			get
			{
				using (var reader = new StreamReader(_fileName))
				{
					int lineNum = 1;
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						yield return new TextAlignment(new TextSegmentRef(1, lineNum), ParseAlignments(line));
						lineNum++;
					}
				}
			}
		}

		private IEnumerable<(int, int)> ParseAlignments(string alignments)
		{
			foreach (string token in alignments.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries))
			{
				int index = token.IndexOf('-');
				int i = int.Parse(token.Substring(0, index));
				int j = int.Parse(token.Substring(index + 1));
				yield return _invert ? (j, i) : (i, j);
			}
		}

		public ITextAlignmentCollection Invert()
		{
			return new TextFileTextAlignmentCollection(Id, _fileName, !_invert);
		}
	}
}
