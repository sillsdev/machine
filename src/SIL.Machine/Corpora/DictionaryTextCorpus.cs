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

		public virtual IEnumerable<IText> Texts => TextDictionary.Values.OrderBy(t => t.Id);

		protected Dictionary<string, IText> TextDictionary { get; }

		public bool TryGetText(string id, out IText text)
		{
			return TextDictionary.TryGetValue(id, out text);
		}

		public IText GetText(string id)
		{
			return TextDictionary[id];
		}

		protected void AddText(IText text)
		{
			TextDictionary[text.Id] = text;
		}
	}
}
