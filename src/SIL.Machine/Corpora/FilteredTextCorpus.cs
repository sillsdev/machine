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

		public IText this[string id]
		{
			get
			{
				IText text = _corpus[id];
				if (_filter(text))
					return text;
				return CreateNullText(id);
			}
		}

		public IText CreateNullText(string id)
		{
			return _corpus.CreateNullText(id);
		}
	}
}
