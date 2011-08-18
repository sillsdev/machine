using System.Collections.Generic;

namespace SIL.APRE
{
	public class IdentityEqualityComparer<T> : IEqualityComparer<T>
	{
		public bool Equals(T x, T y)
		{
			return ReferenceEquals(x, y);
		}

		public int GetHashCode(T obj)
		{
			return obj.GetHashCode();
		}
	}
}
