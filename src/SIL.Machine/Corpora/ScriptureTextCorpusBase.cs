namespace SIL.Machine.Corpora
{
	public abstract class ScriptureTextCorpusBase : DictionaryTextCorpus
	{
		public override string GetTextSortKey(string id)
		{
			return CorporaHelpers.GetScriptureTextSortKey(id);
		}
	}
}
