using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class DictionaryTextCorpus : ITextCorpus
	{
		private readonly Dictionary<string, IText> _texts;

		public DictionaryTextCorpus(IEnumerable<IText> texts)
		{
			_texts = texts.ToDictionary(t => t.Id);
		}

		public IEnumerable<IText> Texts => _texts.Values;

		public bool TryGetText(string id, out IText text)
		{
			return _texts.TryGetValue(id, out text);
		}

		public IText GetText(string id)
		{
			return _texts[id];
		}
	}
}
