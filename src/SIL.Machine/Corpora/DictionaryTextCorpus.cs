using System.Collections;
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

		public IText this[string id] => TextDictionary[id];

		public bool TryGetText(string id, out IText text)
		{
			return TextDictionary.TryGetValue(id, out text);
		}

		protected void AddText(IText text)
		{
			TextDictionary[text.Id] = text;
		}

		public IEnumerator<TextRow> GetEnumerator()
		{
			return GetRows().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}


		public IEnumerable<TextRow> GetRows(IEnumerable<string> textIds = null)
		{
			var textIdSet = new HashSet<string>(textIds ?? TextDictionary.Keys);
			return Texts.Where(t => textIdSet.Contains(t.Id)).SelectMany(t => t.GetRows());
		}
	}
}
