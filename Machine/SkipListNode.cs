namespace SIL.Machine
{
	public class SkipListNode<T> : BidirListNode<SkipListNode<T>>
	{
		private readonly T _value;

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
