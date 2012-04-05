using System;
using System.Collections.Generic;

namespace SIL.Collections
{
	public class FreezableEqualityComparer<T> : EqualityComparer<T> where T : IFreezable<T>
	{
		private static readonly FreezableEqualityComparer<T> Comparer = new FreezableEqualityComparer<T>(); 
		public static FreezableEqualityComparer<T> Instance
		{
			get { return Comparer; }
		}

		public override bool Equals(T x, T y)
		{
			if (x == null && y == null)
				return true;
			if (x == null || y == null)
				return false;
			return x.ValueEquals(y);
		}

		public override int GetHashCode(T obj)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");
			return obj.GetFrozenHashCode();
		}
	}
}
