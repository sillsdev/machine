using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.DataStructures;

namespace SIL.Machine.NgramModeling
{
    public class Ngram<TItem> : IReadOnlyList<TItem>, IStructuralEquatable, IEquatable<Ngram<TItem>>
    {
        public static implicit operator Ngram<TItem>(TItem item)
        {
            if (item == null)
                return new Ngram<TItem>();

            return new Ngram<TItem>(item);
        }

        private readonly TItem[] _items;
        private readonly int _hashCode;

        public Ngram(params TItem[] items)
            : this((IEnumerable<TItem>)items) { }

        public Ngram(IEnumerable<TItem> items)
            : this(items, Direction.LeftToRight) { }

        public Ngram(IEnumerable<TItem> items, Direction dir)
        {
            _items = (dir == Direction.LeftToRight ? items : items.Reverse()).ToArray();
            _hashCode = _items.GetSequenceHashCode();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
        {
            return ((IEnumerable<TItem>)_items).GetEnumerator();
        }

        int IReadOnlyCollection<TItem>.Count
        {
            get { return _items.Length; }
        }

        public int Length
        {
            get { return _items.Length; }
        }

        public TItem this[int index]
        {
            get { return _items[index]; }
        }

        public TItem First
        {
            get
            {
                if (_items.Length == 0)
                    throw new InvalidOperationException("The n-gram is empty.");
                return _items[0];
            }
        }

        public TItem GetFirst(Direction dir)
        {
            if (_items.Length == 0)
                throw new InvalidOperationException("The n-gram is empty.");
            return dir == Direction.LeftToRight ? First : Last;
        }

        public TItem Last
        {
            get
            {
                if (_items.Length == 0)
                    throw new InvalidOperationException("The n-gram is empty.");
                return _items[_items.Length - 1];
            }
        }

        public TItem GetLast(Direction dir)
        {
            if (_items.Length == 0)
                throw new InvalidOperationException("The n-gram is empty.");

            return dir == Direction.LeftToRight ? Last : First;
        }

        public Ngram<TItem> TakeAllExceptLast()
        {
            return TakeAllExceptLast(Direction.LeftToRight);
        }

        public Ngram<TItem> TakeAllExceptLast(Direction dir)
        {
            return new Ngram<TItem>(dir == Direction.LeftToRight ? _items.Take(_items.Length - 1) : _items.Skip(1));
        }

        public Ngram<TItem> SkipFirst()
        {
            return SkipFirst(Direction.LeftToRight);
        }

        public Ngram<TItem> SkipFirst(Direction dir)
        {
            return new Ngram<TItem>(dir == Direction.LeftToRight ? _items.Skip(1) : _items.Take(_items.Length - 1));
        }

        public Ngram<TItem> Concat(TItem item)
        {
            return Concat(item, Direction.LeftToRight);
        }

        public Ngram<TItem> Concat(TItem item, Direction dir)
        {
            return new Ngram<TItem>(
                dir == Direction.LeftToRight ? _items.Concat(item) : item.ToEnumerable().Concat(_items)
            );
        }

        public Ngram<TItem> Concat(Ngram<TItem> ngram)
        {
            return Concat(ngram, Direction.LeftToRight);
        }

        public Ngram<TItem> Concat(Ngram<TItem> ngram, Direction dir)
        {
            return new Ngram<TItem>(dir == Direction.LeftToRight ? _items.Concat(ngram) : ngram.Concat(_items));
        }

        public IEnumerable<TItem> GetItems(Direction dir)
        {
            return dir == Direction.LeftToRight ? this : this.Reverse();
        }

        public bool StartsWith(Ngram<TItem> items)
        {
            return StartsWith(items, Direction.LeftToRight);
        }

        public bool StartsWith(Ngram<TItem> items, Direction dir)
        {
            return StartsWith(items, dir, EqualityComparer<TItem>.Default);
        }

        public bool StartsWith(Ngram<TItem> items, Direction dir, IEqualityComparer<TItem> comparer)
        {
            if (items.Length > Length)
                return false;

            IEnumerable<TItem> x = items;
            IEnumerable<TItem> y = _items;
            if (dir == Direction.RightToLeft)
            {
                x = x.Reverse();
                y = y.Reverse();
            }

            foreach (Tuple<TItem, TItem> item in x.Zip(y))
            {
                if (!comparer.Equals(item.Item1, item.Item2))
                    return false;
            }
            return true;
        }

        public bool EndsWith(Ngram<TItem> items)
        {
            return EndsWith(items, Direction.LeftToRight);
        }

        public bool EndsWith(Ngram<TItem> items, Direction dir)
        {
            return EndsWith(items, dir, EqualityComparer<TItem>.Default);
        }

        public bool EndsWith(Ngram<TItem> items, Direction dir, IEqualityComparer<TItem> comparer)
        {
            if (items.Length > Length)
                return false;

            IEnumerable<TItem> x = items;
            IEnumerable<TItem> y = _items;
            if (dir == Direction.LeftToRight)
            {
                x = x.Reverse();
                y = y.Reverse();
            }

            foreach (Tuple<TItem, TItem> item in x.Zip(y))
            {
                if (!comparer.Equals(item.Item1, item.Item2))
                    return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is Ngram<TItem> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public bool Equals(Ngram<TItem> other)
        {
            return other != null && _hashCode == other._hashCode && _items.SequenceEqual(other._items);
        }

        bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
        {
            if (other == null)
                return false;

            return other is Ngram<TItem> ngram && _items.SequenceEqual(ngram._items, comparer.ToTypesafe<TItem>());
        }

        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            return _items.GetSequenceHashCode(comparer.ToTypesafe<TItem>());
        }

        public override string ToString()
        {
            if (_items.Length == 0)
                return "-";

            return string.Concat(_items.Select(item => item.ToString()));
        }
    }
}
