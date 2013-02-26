using System;
using System.Collections.Generic;

namespace SIL.Collections
{
	public class ValueEqualityComparer<T> : IEqualityComparer<T> where T : IValueEquatable<T>
	{
		private static readonly ValueEqualityComparer<T> Comparer = new ValueEqualityComparer<T>(); 
		public static ValueEqualityComparer<T> Default
		{
			get { return Comparer; }
		}

		public bool Equals(T x, T y)
		{
			if (x == null && y == null)
				return true;
			if (x == null || y == null)
				return false;
			return x.ValueEquals(y);
		}

		public int GetHashCode(T obj)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");
			return obj.GetValueHashCode();
		}
	}
}
