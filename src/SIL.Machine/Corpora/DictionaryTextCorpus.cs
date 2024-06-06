using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class DictionaryTextCorpus : ITextCorpus
    {
        public DictionaryTextCorpus(params IText[] texts)
            : this((IEnumerable<IText>)texts) { }

        public DictionaryTextCorpus(IEnumerable<IText> texts)
        {
            TextDictionary = texts.ToDictionary(t => t.Id);
        }

        public IEnumerable<IText> Texts => TextDictionary.Values.OrderBy(t => t.SortKey);

        protected Dictionary<string, IText> TextDictionary { get; }

        public IText this[string id] => TextDictionary[id];

        public bool IsTokenized { get; set; }

        public ScrVers Versification { get; set; }

        int ICorpus<TextRow>.Count(bool includeEmpty)
        {
            return Count(includeEmpty, null);
        }

        public int Count(bool includeEmpty = true, IEnumerable<string> textIds = null)
        {
            IEnumerable<IText> texts = Texts;
            if (textIds != null)
            {
                var textIdSet = new HashSet<string>(textIds);
                texts = texts.Where(t => textIds.Contains(t.Id));
            }
            return texts.Sum(t => t.Count(includeEmpty));
        }

        public bool TryGetText(string id, out IText text)
        {
            return TextDictionary.TryGetValue(id, out text);
        }

        protected void AddText(IText text)
        {
            TextDictionary[text.Id] = text;
        }

        public IEnumerable<TextRow> GetRows()
        {
            return GetRows(null);
        }

        public IEnumerable<TextRow> GetRows(IEnumerable<string> textIds)
        {
            IEnumerable<IText> texts = Texts;
            if (textIds != null)
            {
                var textIdSet = new HashSet<string>(textIds);
                texts = texts.Where(t => textIds.Contains(t.Id));
            }
            return texts.SelectMany(t => t.GetRows());
        }

        public IEnumerator<TextRow> GetEnumerator()
        {
            return GetRows().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
