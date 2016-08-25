using System.Collections.Generic;
using System.IO;

namespace SIL.Machine.Corpora
{
	public class TextFileText : IText
	{
		private readonly string _fileName;

		public TextFileText(string id, string fileName)
		{
			Id = id;
			_fileName = fileName;
		}

		public string Id { get; }

		public IEnumerable<TextSegment> Segments
		{
			get
			{
				using (var reader = new StreamReader(File.Open(_fileName, FileMode.Open)))
				{
					int lineNum = 1;
					string line;
					while ((line = reader.ReadLine()) != null)
						yield return new TextSegment(new TextSegmentRef(1, lineNum), line);
				}
			}
		}
	}
}
