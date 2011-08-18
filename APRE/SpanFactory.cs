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
			return _compare(start, end) <= 0;
		}

		public Span<TOffset> Create(TOffset start, TOffset end)
		{
			return new Span<TOffset>(_compare, _calcLength, _includeEndpoint, start, end);
		}

		public Span<TOffset> Create(TOffset offset)
		{
			return new Span<TOffset>(_compare, _calcLength, _includeEndpoint, offset, offset);
		}
	}
}
