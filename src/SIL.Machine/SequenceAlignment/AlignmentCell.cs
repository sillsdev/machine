using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.SequenceAlignment
{
    public class AlignmentCell<T> : IReadOnlyList<T>
    {
        private readonly T[] _items;

        public AlignmentCell(params T[] items)
        {
            if (items.Length > 0)
                _items = items;
        }

        public AlignmentCell(IEnumerable<T> items)
        {
            T[] itemArray = items.ToArray();
            if (itemArray.Length > 0)
                _items = itemArray;
        }

        public bool IsNull
        {
            get { return _items == null; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_items == null)
                yield break;
            foreach (T item in _items)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T First
        {
            get
            {
                if (_items == null || _items.Length == 0)
                    throw new InvalidOperationException("The alignment cell is empty.");
                return _items[0];
            }
        }

        public T Last
        {
            get
            {
                if (_items == null || _items.Length == 0)
                    throw new InvalidOperationException("The alignment cell is empty.");
                return _items[_items.Length - 1];
            }
        }

        public int Count
        {
            get
            {
                if (_items == null)
                    return 0;
                return _items.Length;
            }
        }

        public T this[int index]
        {
            get { return _items[index]; }
        }
    }
}
