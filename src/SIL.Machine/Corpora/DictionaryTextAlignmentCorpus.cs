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

		public ITextAlignmentCorpusView Source => this;

		public ITextAlignmentCollection this[string id] => _textAlignmentCollections[id];

		protected void AddTextAlignmentCollection(ITextAlignmentCollection alignments)
		{
			_textAlignmentCollections[alignments.Id] = alignments;
		}

		public bool TryGetTextAlignmentCollection(string id, out ITextAlignmentCollection textAlignmentCollection)
		{
			return _textAlignmentCollections.TryGetValue(id, out textAlignmentCollection);
		}

		public virtual IEnumerable<TextAlignmentCorpusRow> GetRows()
		{
			return TextAlignmentCollections.SelectMany(c => c.GetRows());
		}
	}
}
