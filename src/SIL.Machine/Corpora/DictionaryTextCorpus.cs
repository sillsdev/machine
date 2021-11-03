using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class DictionaryTextCorpus : ITextCorpus
	{

		public DictionaryTextCorpus(params IText[] texts)
			: this((IEnumerable<IText>)texts)
		{
		}

		public DictionaryTextCorpus(IEnumerable<IText> texts)
		{
			TextDictionary = texts.ToDictionary(t => t.Id);
		}

		public IEnumerable<IText> Texts => TextDictionary.Values.OrderBy(t => t.SortKey);

		protected Dictionary<string, IText> TextDictionary { get; }

		public IText this[string id]
		{
			get
			{
				if (TextDictionary.TryGetValue(id, out IText text))
					return text;
				return CreateNullText(id);
			}
		}

		public virtual IText CreateNullText(string id)
		{
			return new NullText(id, id);
		}

		protected void AddText(IText text)
		{
			TextDictionary[text.Id] = text;
		}
	}
}
