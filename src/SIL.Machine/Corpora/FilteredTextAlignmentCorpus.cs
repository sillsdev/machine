using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class FilteredTextAlignmentCorpus : ITextAlignmentCorpus
	{
		private readonly ITextAlignmentCorpus _corpus;
		private readonly Func<ITextAlignmentCollection, bool> _filter;

		public FilteredTextAlignmentCorpus(ITextAlignmentCorpus corpus, Func<ITextAlignmentCollection, bool> filter)
		{
			_corpus = corpus;
			_filter = filter;
		}

		public IEnumerable<ITextAlignmentCollection> TextAlignmentCollections => _corpus.TextAlignmentCollections
			.Where(_filter);

		public ITextAlignmentCollection this[string id]
		{
			get
			{
				ITextAlignmentCollection collection = _corpus[id];
				if (_filter(collection))
					return collection;
				return CreateNullTextAlignmentCollection(id);
			}
		}

		public ITextAlignmentCorpus Invert()
		{
			return new FilteredTextAlignmentCorpus(_corpus.Invert(), _filter);
		}

		public ITextAlignmentCollection CreateNullTextAlignmentCollection(string id)
		{
			return _corpus.CreateNullTextAlignmentCollection(id);
		}
	}
}
