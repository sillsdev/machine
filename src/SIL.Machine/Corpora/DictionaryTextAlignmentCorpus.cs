using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class DictionaryTextAlignmentCorpus : ITextAlignmentCorpus
	{
		private readonly Dictionary<string, ITextAlignmentCollection> _textAlignmentCollections;

		public DictionaryTextAlignmentCorpus(params ITextAlignmentCollection[] textAlignmentCollections)
			: this((IEnumerable<ITextAlignmentCollection>)textAlignmentCollections)
		{
		}

		public DictionaryTextAlignmentCorpus(IEnumerable<ITextAlignmentCollection> textAlignmentCollections)
		{
			_textAlignmentCollections = textAlignmentCollections.ToDictionary(tac => tac.Id);
		}

		public IEnumerable<ITextAlignmentCollection> TextAlignmentCollections => _textAlignmentCollections.Values
			.OrderBy(ac => ac.SortKey);

		public ITextAlignmentCollection this[string id]
		{
			get
			{
				if (_textAlignmentCollections.TryGetValue(id, out ITextAlignmentCollection collection))
					return collection;
				return CreateNullTextAlignmentCollection(id);
			}
		}

		public ITextAlignmentCorpus Invert()
		{
			return new DictionaryTextAlignmentCorpus(_textAlignmentCollections.Values.Select(tac => tac.Invert()));
		}

		public virtual ITextAlignmentCollection CreateNullTextAlignmentCollection(string id)
		{
			return new NullTextAlignmentCollection(id, id);
		}

		protected void AddTextAlignmentCollection(ITextAlignmentCollection alignments)
		{
			_textAlignmentCollections[alignments.Id] = alignments;
		}
	}
}
