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

		public Range<int>[] Tokenize(string text, int index = 0, int length = -1)
		{
			return _tokenizer.Tokenize(text,
				Range<int>.Create(index, length == -1 ? text.Length : index + length)).ToArray();
		}

		public string[] TokenizeToStrings(string text, int index = 0, int length = -1)
		{
			return _tokenizer.TokenizeToStrings(text,
				Range<int>.Create(index, length == -1 ? text.Length : index + length)).ToArray();
		}
	}
}
