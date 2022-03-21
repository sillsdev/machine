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

		public ITextCorpusView Source => this;

		protected Dictionary<string, IText> TextDictionary { get; }


		public IText this[string id] => TextDictionary[id];

		public bool TryGetText(string id, out IText text)
		{
			return TextDictionary.TryGetValue(id, out text);
		}

		public virtual IEnumerable<TextCorpusRow> GetRows(ITextCorpusView basedOn = null)
		{
			return Texts.SelectMany(t => t.GetRows());
		}

		protected void AddText(IText text)
		{
			TextDictionary[text.Id] = text;
		}
	}
}
