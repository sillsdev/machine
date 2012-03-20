using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SIL.Collections
{
	public class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
	{
		public bool Equals(T x, T y)
		{
			return ReferenceEquals(x, y);
		}

		public int GetHashCode(T obj)
		{
			return RuntimeHelpers.GetHashCode(obj);
		}
	}
}
