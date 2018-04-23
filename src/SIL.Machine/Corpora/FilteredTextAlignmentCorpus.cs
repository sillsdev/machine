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

		public ITextAlignmentCollection GetTextAlignmentCollection(string id)
		{
			ITextAlignmentCollection alignments = _corpus.GetTextAlignmentCollection(id);
			if (_filter(alignments))
				return alignments;

			throw new KeyNotFoundException();
		}

		public ITextAlignmentCorpus Invert()
		{
			return new FilteredTextAlignmentCorpus(_corpus.Invert(), _filter);
		}

		public bool TryGetTextAlignmentCollection(string id, out ITextAlignmentCollection alignments)
		{
			if (_corpus.TryGetTextAlignmentCollection(id, out alignments))
			{
				if (_filter(alignments))
					return true;

				alignments = null;
			}

			return false;
		}
	}
}
