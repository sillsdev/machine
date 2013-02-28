using System;
using System.Collections.Generic;
using SIL.Collections;

namespace SIL.Machine
{
	public abstract class SpanFactory<TOffset>
	{
		private readonly bool _includeEndpoint;
		private readonly IComparer<TOffset> _comparer;
		private readonly IEqualityComparer<TOffset> _equalityComparer; 

		protected SpanFactory()
			: this(false)
		{
		}

		protected SpanFactory(bool includeEndpoint)
			: this(includeEndpoint, Comparer<TOffset>.Default, EqualityComparer<TOffset>.Default)
		{
		}

		protected SpanFactory(bool includeEndpoint, IComparer<TOffset> comparer, IEqualityComparer<TOffset> equalityComparer)
		{
			_includeEndpoint = includeEndpoint;
			_comparer = comparer;
			_equalityComparer = equalityComparer;
		}

		public abstract Span<TOffset> Empty { get; }

		public bool IncludeEndpoint
		{
			get { return _includeEndpoint; }
		}

		public IComparer<TOffset> Comparer
		{
			get { return _comparer; }
		}

		public IEqualityComparer<TOffset> EqualityComparer
		{
			get { return _equalityComparer; }
		}

		public abstract int CalcLength(TOffset start, TOffset end);

		public int CalcLength(TOffset start, TOffset end, Direction dir)
		{
			TOffset actualStart;
			TOffset actualEnd;
			if (dir == Direction.LeftToRight)
			{
				actualStart = start;
				actualEnd = end;
			}
			else
			{
				actualStart = end;
				actualEnd = start;
			}

			return CalcLength(actualStart, actualEnd);
		}

		public bool IsValidSpan(TOffset start, TOffset end)
		{
			return IsValidSpan(start, end, Direction.LeftToRight);
		}

		public bool IsValidSpan(TOffset start, TOffset end, Direction dir)
		{
			TOffset actualStart;
			TOffset actualEnd;
			if (dir == Direction.LeftToRight)
			{
				actualStart = start;
				actualEnd = end;
			}
			else
			{
				actualStart = end;
				actualEnd = start;
			}

			int compare = _comparer.Compare(actualStart, actualEnd);
			return _includeEndpoint ? compare <= 0 : compare < 0;
		}

		public abstract bool IsRange(TOffset start, TOffset end);

		public bool IsRange(TOffset start, TOffset end, Direction dir)
		{
			TOffset actualStart;
			TOffset actualEnd;
			if (dir == Direction.LeftToRight)
			{
				actualStart = start;
				actualEnd = end;
			}
			else
			{
				actualStart = end;
				actualEnd = start;
			}

			return IsRange(actualStart, actualEnd);
		}

		public Span<TOffset> Create(TOffset start, TOffset end)
		{
			return Create(start, end, Direction.LeftToRight);
		}

		public Span<TOffset> Create(TOffset start, TOffset end, Direction dir)
		{
			TOffset actualStart;
			TOffset actualEnd;
			if (dir == Direction.LeftToRight)
			{
				actualStart = start;
				actualEnd = end;
			}
			else
			{
				actualStart = end;
				actualEnd = start;
			}

			if (!IsValidSpan(actualStart, actualEnd))
				throw new ArgumentException("The start offset is greater than the end offset.", "start");

			return new Span<TOffset>(this, actualStart, actualEnd);
		}

		public Span<TOffset> Create(TOffset offset)
		{
			return Create(offset, Direction.LeftToRight);
		}

		public abstract Span<TOffset> Create(TOffset offset, Direction dir);
	}
}
