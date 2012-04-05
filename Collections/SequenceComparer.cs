using System.Collections.Generic;

namespace SIL.Collections
{
	public static class SequenceComparer
	{
		public static SequenceComparer<T> Create<T>(IComparer<T> comparer)
		{
			return new SequenceComparer<T>(comparer);
		}
	}

	public class SequenceComparer<T> : Comparer<IEnumerable<T>>
	{
		private readonly IComparer<T> _comparer;

		public SequenceComparer()
			: this(Comparer<T>.Default)
		{
		}
 
		public SequenceComparer(IComparer<T> comparer)
		{
			_comparer = comparer;
		}

		public override int Compare(IEnumerable<T> x, IEnumerable<T> y)
		{
			return x.SequenceCompare(y, _comparer);
		}
	}
}
