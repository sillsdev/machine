using System;

namespace SIL.APRE
{
	public class SpanFactory<TOffset>
	{
		private readonly Func<TOffset, TOffset, int> _compare;
		private readonly Func<TOffset, TOffset, int> _calcLength;

		public SpanFactory(Func<TOffset, TOffset, int> compare, Func<TOffset, TOffset, int> calcLength)
		{
			_compare = compare;
			_calcLength = calcLength;
		}

		public bool IsValidSpan(TOffset start, TOffset end)
		{
			return _compare(start, end) <= 0;
		}

		public Span<TOffset> Create(TOffset start, TOffset end)
		{
			return new Span<TOffset>(_compare, _calcLength, start, end);
		}

		public Span<TOffset> Create(TOffset offset)
		{
			return new Span<TOffset>(_compare, _calcLength, offset, offset);
		}
	}
}
