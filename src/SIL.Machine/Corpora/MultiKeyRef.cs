using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;

namespace SIL.Machine.Corpora
{
    public class MultiKeyRef : IEquatable<MultiKeyRef>, IComparable<MultiKeyRef>, IComparable
    {
        public MultiKeyRef(string textId, params object[] keys) : this(textId, (IEnumerable<object>)keys) { }

        public MultiKeyRef(string textId, IEnumerable<object> keys)
        {
            TextId = textId;
            Keys = keys.ToList();
        }

        public string TextId { get; }

        public IReadOnlyList<object> Keys { get; }

        public int CompareTo(MultiKeyRef other)
        {
            int res = TextId.CompareTo(other.TextId);
            if (res != 0)
                return res;

            for (int i = 0; i < Keys.Count && i < other.Keys.Count; i++)
            {
                object key = Keys[i];
                object otherKey = other.Keys[i];
                res = Comparer<object>.Default.Compare(key, otherKey);
                if (res != 0)
                    return res;
            }
            return Keys.Count.CompareTo(other.Keys.Count);
        }

        public int CompareTo(object obj)
        {
            if (obj is MultiKeyRef multiKeyRef)
                return CompareTo(multiKeyRef);
            throw new ArgumentException($"The specified object is not a {nameof(MultiKeyRef)}.", nameof(obj));
        }

        public bool Equals(MultiKeyRef other)
        {
            return TextId == other.TextId && Keys.SequenceEqual(other.Keys);
        }

        public override bool Equals(object obj)
        {
            if (obj is MultiKeyRef multiKeyRef)
                return Equals(multiKeyRef);
            return false;
        }

        public override int GetHashCode()
        {
            int code = 23;
            code = code * 31 + TextId.GetHashCode();
            code = code * 31 + Keys.GetSequenceHashCode();
            return code;
        }

        public override string ToString()
        {
            string keys = string.Join("-", Keys.Select(k => k.ToString()));
            return $"{TextId}:{keys}";
        }
    }
}
