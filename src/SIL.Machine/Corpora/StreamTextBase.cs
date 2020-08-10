using SIL.Machine.Tokenization;

namespace SIL.Machine.Corpora
{
	public abstract class StreamTextBase : TextBase
	{
		protected StreamTextBase(ITokenizer<string, int, string> wordTokenizer, string id, string sortKey)
			: base(wordTokenizer, id, sortKey)
		{
		}

		protected abstract IStreamContainer CreateStreamContainer();
	}
}
