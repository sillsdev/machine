using System;

namespace SIL.Machine
{
	public class DefaultSpanFactory<TOffset> : SpanFactory<TOffset>
	{
		private readonly Func<TOffset, TOffset, int> _compare;
		private readonly Func<TOffset, TOffset, int> _calcLength;
		private readonly Span<TOffset> _empty; 

		public DefaultSpanFactory(Func<TOffset, TOffset, int> compare, Func<TOffset, TOffset, int> calcLength, bool includeEndpoint)
			: base(includeEndpoint)
		{
			_compare = compare;
			_calcLength = calcLength;
			_empty = new Span<TOffset>(this, default(TOffset), default(TOffset));
		}

		public override Span<TOffset> Empty
		{
			get { return _empty; }
		}

		public override int Compare(TOffset x, TOffset y)
		{
			return _compare(x, y);
		}

		public override int CalcLength(TOffset start, TOffset end)
		{
			return _calcLength(start, end);
		}
	}
}
