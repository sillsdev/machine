using System.Collections.Generic;

namespace SIL.Machine
{
	public class ReverseComparer<T> : IComparer<T>
	{
		private readonly IComparer<T> _comparer;
 
		public ReverseComparer(IComparer<T> comparer)
		{
			_comparer = comparer;
		}

		public int Compare(T x, T y)
		{
			return _comparer.Compare(y, x);
		}
	}
}
