using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public abstract class TextBase : IText
    {
        protected TextBase(string id, string sortKey)
        {
            Id = id;
            SortKey = sortKey;
        }

        public string Id { get; }

        public string SortKey { get; }

        public virtual int Count(bool includeEmpty = true)
        {
            return includeEmpty ? GetRows().Count() : GetRows().Count(r => !r.IsEmpty);
        }

        public abstract IEnumerable<TextRow> GetRows();

        protected TextRow CreateRow(string text, object segRef, TextRowFlags flags = TextRowFlags.SentenceStart)
        {
            text = text.Trim();
            return new TextRow(Id, segRef)
            {
                Segment = text.Length == 0 ? Array.Empty<string>() : new[] { text },
                Flags = flags
            };
        }

        protected TextRow CreateEmptyRow(object segRef, TextRowFlags flags = TextRowFlags.None)
        {
            return new TextRow(Id, segRef) { Flags = flags };
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
