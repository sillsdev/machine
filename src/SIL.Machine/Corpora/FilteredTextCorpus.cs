using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class FilteredTextCorpus : ITextCorpus
	{
		private readonly ITextCorpus _corpus;
		private readonly Func<IText, bool> _filter;

		public FilteredTextCorpus(ITextCorpus corpus, Func<IText, bool> filter)
		{
			_corpus = corpus;
			_filter = filter;
		}

		public IEnumerable<IText> Texts => _corpus.Texts.Where(_filter);

		public IText GetText(string id)
		{
			IText text = _corpus.GetText(id);
			if (_filter(text))
				return text;
			throw new KeyNotFoundException();
		}

		public bool TryGetText(string id, out IText text)
		{
			if (_corpus.TryGetText(id, out text))
			{
				if (_filter(text))
					return true;

				text = null;
			}

			return false;
		}

		public string GetTextSortKey(string id)
		{
			return _corpus.GetTextSortKey(id);
		}
	}
}
