using SIL.Machine.Annotations;
using SIL.Machine.WebApi;
using System.Linq;

namespace SIL.Machine.Tokenization
{
	public class SegmentTokenizer
	{
		private readonly StringTokenizer _tokenizer;

		public SegmentTokenizer(string segmentType)
		{
			_tokenizer = WebApiUtils.CreateSegmentTokenizer(segmentType);
		}

		public Range<int>[] Tokenize(string text)
		{
			return _tokenizer.Tokenize(text).ToArray();
		}

		public string[] TokenizeToStrings(string text)
		{
			return _tokenizer.TokenizeToStrings(text).ToArray();
		}
	}
}
