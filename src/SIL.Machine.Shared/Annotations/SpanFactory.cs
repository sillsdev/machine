using System;
using System.Collections.Generic;
using SIL.Machine.DataStructures;

namespace SIL.Machine.Annotations
{
	internal abstract class SpanFactory<TOffset>
	{
		protected SpanFactory()
			: this(false)
		{
		}

		protected SpanFactory(bool includeEndpoint)
			: this(includeEndpoint, Comparer<TOffset>.Default, EqualityComparer<TOffset>.Default)
		{
		}

		protected SpanFactory(bool includeEndpoint, IComparer<TOffset> comparer,
			IEqualityComparer<TOffset> equalityComparer)
		{
			IncludeEndpoint = includeEndpoint;
			Comparer = comparer;
			EqualityComparer = equalityComparer;
		}

		public Span<TOffset> Null { get; protected set; }

		public bool IncludeEndpoint { get; }

		public IComparer<TOffset> Comparer { get; }

		public IEqualityComparer<TOffset> EqualityComparer { get; }

		public abstract int GetLength(TOffset start, TOffset end);

		public bool IsValidSpan(TOffset start, TOffset end)
		{
			int compare = Comparer.Compare(start, end);
			return compare <= 0;
		}

		public virtual bool IsEmptySpan(TOffset start, TOffset end)
		{
			return GetLength(start, end) == 0;
		}

		public virtual Span<TOffset> Create(TOffset start, TOffset end, Direction dir)
		{
			if (dir == Direction.RightToLeft)
			{
				TOffset temp = start;
				start = end;
				end = temp;
			}

			if (!IsValidSpan(start, end))
				throw new ArgumentException("The start offset is greater than the end offset.", nameof(start));

			return new Span<TOffset>(start, end);
		}

		public virtual Span<TOffset> Create(TOffset offset, Direction dir)
		{
			return Create(offset, offset, dir);
		}
	}
}
