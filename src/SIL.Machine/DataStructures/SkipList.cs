using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.DataStructures
{
    public class SkipList<T> : BidirList<SkipListNode<T>>, ICollection<T>
    {
        public SkipList() : this(Comparer<T>.Default) { }

        public SkipList(IComparer<T> comparer)
            : base(
                new ProjectionComparer<SkipListNode<T>, T>(node => node.Value, comparer),
                begin => new SkipListNode<T>()
            ) { }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (SkipListNode<T> node in (IEnumerable<SkipListNode<T>>)this)
                yield return node.Value;
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public SkipListNode<T> Add(T item)
        {
            var node = new SkipListNode<T>(item);
            Add(node);
            return node;
        }

        public bool Find(T item, out SkipListNode<T> result)
        {
            var node = new SkipListNode<T>(item);
            return Find(node, out result);
        }

        public bool Contains(T item)
        {
            return Find(item, out _);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (T value in this)
                array[arrayIndex++] = value;
        }

        public bool Remove(T item)
        {
            if (Find(item, out SkipListNode<T> result))
                return Remove(result);
            return false;
        }

        bool ICollection<T>.IsReadOnly => false;
    }
}
