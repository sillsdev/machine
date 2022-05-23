namespace SIL.Machine.DataStructures
{
    public class SkipListNode<T> : BidirListNode<SkipListNode<T>>
    {
        private readonly T _value;

        internal SkipListNode() { }

        public SkipListNode(T value)
        {
            _value = value;
        }

        public T Value
        {
            get { return _value; }
        }
    }
}
