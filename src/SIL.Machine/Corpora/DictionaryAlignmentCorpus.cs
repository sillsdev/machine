using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public class DictionaryAlignmentCorpus : AlignmentCorpusBase
    {
        private readonly Dictionary<string, IAlignmentCollection> _alignmentCollections;

        public DictionaryAlignmentCorpus(params IAlignmentCollection[] alignmentCollections)
            : this((IEnumerable<IAlignmentCollection>)alignmentCollections) { }

        public DictionaryAlignmentCorpus(IEnumerable<IAlignmentCollection> alignmentCollections)
        {
            _alignmentCollections = alignmentCollections.ToDictionary(ac => ac.Id);
        }

        public override IEnumerable<IAlignmentCollection> AlignmentCollections =>
            _alignmentCollections.Values.OrderBy(ac => ac.SortKey);

        public override int Count(bool includeEmpty = true, IEnumerable<string> textIds = null)
        {
            IEnumerable<IAlignmentCollection> alignmentCollections = AlignmentCollections;
            if (textIds != null)
            {
                var textIdSet = new HashSet<string>(textIds);
                alignmentCollections = alignmentCollections.Where(t => textIds.Contains(t.Id));
            }
            return alignmentCollections.Sum(t => t.Count(includeEmpty));
        }

        public IAlignmentCollection this[string id] => _alignmentCollections[id];

        protected void AddAlignmentCollection(IAlignmentCollection alignments)
        {
            _alignmentCollections[alignments.Id] = alignments;
        }

        public bool TryGetAlignmentCollection(string id, out IAlignmentCollection alignmentCollection)
        {
            return _alignmentCollections.TryGetValue(id, out alignmentCollection);
        }

        public override IEnumerable<AlignmentRow> GetRows(IEnumerable<string> textIds)
        {
            IEnumerable<IAlignmentCollection> alignmentCollections = AlignmentCollections;
            if (textIds != null)
            {
                var textIdSet = new HashSet<string>(textIds);
                alignmentCollections = alignmentCollections.Where(t => textIds.Contains(t.Id));
            }
            return alignmentCollections.SelectMany(c => c.GetRows());
        }
    }
}
