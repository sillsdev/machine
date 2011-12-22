using System;
using System.Collections.Generic;

namespace SIL.Machine
{
	public static class ProjectionComparer
	{
		public static ProjectionComparer<TSource, TKey> Create<TSource, TKey>(Func<TSource, TKey> projection)
		{
			return new ProjectionComparer<TSource, TKey>(projection);
		}

		public static ProjectionComparer<TSource, TKey> Create<TSource, TKey>(TSource ignored, Func<TSource, TKey> projection)
		{
			return new ProjectionComparer<TSource, TKey>(projection);
		}
	}

	public static class ProjectionComparer<TSource>
	{
		public static ProjectionComparer<TSource, TKey> Create<TKey>(Func<TSource, TKey> projection)
		{
			return new ProjectionComparer<TSource, TKey>(projection);
		}
	}

	public class ProjectionComparer<TSource, TKey> : IComparer<TSource>
	{
		private readonly Func<TSource, TKey> _projection;
		private readonly IComparer<TKey> _comparer;

		public ProjectionComparer(Func<TSource, TKey> projection)
			: this(projection, null)
		{
		}

		public ProjectionComparer(Func<TSource, TKey> projection, IComparer<TKey> comparer)
		{
			_comparer = comparer ?? Comparer<TKey>.Default;
			_projection = projection;
		}

		public int Compare(TSource x, TSource y)
		{
			if (x == null && y == null)
				return 0;
			if (x == null)
				return -1;
			if (y == null)
				return 1;
			return _comparer.Compare(_projection(x), _projection(y));
		}
	}
}
