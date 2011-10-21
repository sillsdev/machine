using System;

namespace SIL.APRE
{
	public class SpanFactory<TOffset>
	{
		private readonly Func<TOffset, TOffset, int> _compare;
		private readonly Func<TOffset, TOffset, int> _calcLength;
		private readonly bool _includeEndpoint;

		public SpanFactory(Func<TOffset, TOffset, int> compare, Func<TOffset, TOffset, int> calcLength, bool includeEndpoint)
		{
			_compare = compare;
			_calcLength = calcLength;
			_includeEndpoint = includeEndpoint;
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

			return _compare(actualStart, actualEnd) <= 0;
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

			return new Span<TOffset>(_compare, _calcLength, _includeEndpoint, actualStart, actualEnd);
		}

		public Span<TOffset> Create(TOffset offset)
		{
			return new Span<TOffset>(_compare, _calcLength, _includeEndpoint, offset, offset);
		}
	}
}
