using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public class UsfmUpdateBlockRow
    {
        public string Text { get; }
        public IReadOnlyDictionary<string, object> Metadata { get; }

        public UsfmUpdateBlockRow(string text, IReadOnlyDictionary<string, object> metadata = null)
        {
            Text = text;
            Metadata = metadata ?? new Dictionary<string, object>();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UsfmUpdateBlockRow other))
                return false;

            return string.Equals(Text, other.Text, StringComparison.Ordinal)
                && Metadata.Count == other.Metadata.Count
                && !Metadata.Except(other.Metadata).Any();
        }

        public override int GetHashCode()
        {
            return Text?.GetHashCode() ?? 0;
        }
    }

    public class UsfmUpdateBlock
    {
        public IReadOnlyList<ScriptureRef> Refs
        {
            get => _refs;
        }
        public IReadOnlyList<UsfmUpdateBlockElement> Elements
        {
            get => _elements;
        }

        // Several rows can match a verse range, and this block's text is their texts concatenated in order
        public IReadOnlyList<UsfmUpdateBlockRow> Rows
        {
            get => _rows;
        }

        private readonly List<ScriptureRef> _refs;
        private readonly List<UsfmUpdateBlockElement> _elements;
        private readonly List<UsfmUpdateBlockRow> _rows;

        public UsfmUpdateBlock(
            IEnumerable<ScriptureRef> refs = null,
            IEnumerable<UsfmUpdateBlockElement> elements = null,
            IEnumerable<UsfmUpdateBlockRow> rows = null
        )
        {
            _refs = refs?.ToList() ?? new List<ScriptureRef>();
            _elements = elements?.ToList() ?? new List<UsfmUpdateBlockElement>();
            _rows = rows?.ToList() ?? new List<UsfmUpdateBlockRow>();
        }

        public List<UsfmToken> GetTokens()
        {
            return _elements.SelectMany(e => e.GetTokens()).ToList();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UsfmUpdateBlock))
                return false;

            UsfmUpdateBlock other = (UsfmUpdateBlock)obj;

            return _refs.SequenceEqual(other._refs)
                && _elements.SequenceEqual(other._elements)
                && _rows.SequenceEqual(other._rows);
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 31 + _refs.GetHashCode();
            hash = hash * 31 + _elements.GetHashCode();
            return hash;
        }

        public UsfmUpdateBlock Clone()
        {
            return new UsfmUpdateBlock(_refs, _elements, _rows);
        }
    }
}
