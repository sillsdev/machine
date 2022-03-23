using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class DictionaryAlignmentCorpus : IAlignmentCorpus
	{
		private readonly Dictionary<string, IAlignmentCollection> _alignmentCollections;

		public DictionaryAlignmentCorpus(params IAlignmentCollection[] alignmentCollections)
			: this((IEnumerable<IAlignmentCollection>)alignmentCollections)
		{
		}

		public DictionaryAlignmentCorpus(IEnumerable<IAlignmentCollection> alignmentCollections)
		{
			_alignmentCollections = alignmentCollections.ToDictionary(ac => ac.Id);
		}

		public IEnumerable<IAlignmentCollection> AlignmentCollections => _alignmentCollections.Values
			.OrderBy(ac => ac.SortKey);


		public IAlignmentCollection this[string id] => _alignmentCollections[id];

		protected void AddAlignmentCollection(IAlignmentCollection alignments)
		{
			_alignmentCollections[alignments.Id] = alignments;
		}

		public bool TryGetAlignmentCollection(string id, out IAlignmentCollection alignmentCollection)
		{
			return _alignmentCollections.TryGetValue(id, out alignmentCollection);
		}

		public IEnumerator<AlignmentRow> GetEnumerator()
		{
			return GetRows().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		protected virtual IEnumerable<AlignmentRow> GetRows()
		{
			return AlignmentCollections.SelectMany(c => c.GetRows());
		}
	}
}
