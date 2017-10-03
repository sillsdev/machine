using SIL.Machine.Tokenization;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public abstract class TextBase : IText
	{
		protected TextBase(ITokenizer<string, int> wordTokenizer, string id)
		{
			WordTokenizer = wordTokenizer;
			Id = id;
		}

		public string Id { get; }

		protected ITokenizer<string, int> WordTokenizer { get; }

		public abstract IEnumerable<TextSegment> Segments { get; }

		protected TextSegment CreateTextSegment(int sectionNum, int segmentNum, string text)
		{
			string[] segment = WordTokenizer.TokenizeToStrings(text.Trim()).ToArray();
			return new TextSegment(new TextSegmentRef(sectionNum, segmentNum), segment);
		}
	}
}
