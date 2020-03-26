using SIL.Machine.Tokenization;

namespace SIL.Machine.Corpora
{
	public abstract class ScriptureTextBase : StreamTextBase
	{
		protected ScriptureTextBase(ITokenizer<string, int> wordTokenizer, string id)
			: base(wordTokenizer, id, CorporaHelpers.GetScriptureTextSortKey(id))
		{
		}
	}
}
