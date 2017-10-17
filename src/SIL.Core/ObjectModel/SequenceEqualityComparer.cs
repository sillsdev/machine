using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;

namespace SIL.ObjectModel
{
	public static class SequenceEqualityComparer
	{
		public static SequenceEqualityComparer<T> Create<T>(IEqualityComparer<T> itemComparer)
		{
			return new SequenceEqualityComparer<T>(itemComparer);
		}
	}

	public class SequenceEqualityComparer<T> : IEqualityComparer<IEnumerable<T>>
	{
		public static SequenceEqualityComparer<T> Default { get; } = new SequenceEqualityComparer<T>();

		private readonly IEqualityComparer<T> _itemComparer; 

		public SequenceEqualityComparer()
			: this(EqualityComparer<T>.Default)
		{
		}

		public SequenceEqualityComparer(IEqualityComparer<T> itemComparer)
		{
			_itemComparer = itemComparer;
		}

		public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
		{
			return x.SequenceEqual(y, _itemComparer);
		}

		public int GetHashCode(IEnumerable<T> obj)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");

			return obj.GetSequenceHashCode(_itemComparer);
		}
	}
}
