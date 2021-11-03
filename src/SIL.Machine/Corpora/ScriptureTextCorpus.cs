using System.Collections.Generic;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public abstract class ScriptureTextCorpus : DictionaryTextCorpus
	{
		protected ScriptureTextCorpus(ITokenizer<string, int, string> wordTokenizer)
		{
			WordTokenizer = wordTokenizer;
		}

		public ITokenizer<string, int, string> WordTokenizer { get; }

		public abstract ScrVers Versification { get; }

		public override IText CreateNullText(string id)
		{
			return new NullScriptureText(WordTokenizer, id, Versification);
		}
	}
}
