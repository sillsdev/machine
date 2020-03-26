using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class DictionaryTextCorpus : ITextCorpus
	{

		public DictionaryTextCorpus(params IText[] texts)
			: this((IEnumerable<IText>) texts)
		{
		}

		public DictionaryTextCorpus(IEnumerable<IText> texts)
		{
			TextDictionary = texts.ToDictionary(t => t.Id);
		}

		public IEnumerable<IText> Texts => TextDictionary.Values.OrderBy(t => t.SortKey);

		protected Dictionary<string, IText> TextDictionary { get; }

		public bool TryGetText(string id, out IText text)
		{
			return TextDictionary.TryGetValue(id, out text);
		}

		public IText GetText(string id)
		{
			return TextDictionary[id];
		}

		public virtual string GetTextSortKey(string id)
		{
			return id;
		}

		protected void AddText(IText text)
		{
			TextDictionary[text.Id] = text;
		}
	}
}
