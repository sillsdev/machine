using SIL.Machine.Tokenization;

namespace SIL.Machine.Corpora
{
	public abstract class StreamTextBase : TextBase
	{
		protected StreamTextBase(ITokenizer<string, int> wordTokenizer, string id)
			: base(wordTokenizer, id)
		{
		}

		protected abstract IStreamContainer CreateStreamContainer();
	}
}
