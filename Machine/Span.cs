using System;

namespace SIL.Machine
{
	public struct Span<TOffset> : IComparable<Span<TOffset>>, IComparable, IEquatable<Span<TOffset>>
	{
		public static bool operator ==(Span<TOffset> x, Span<TOffset> y)
		{
			return x.Equals(y);
		}
		public static bool operator !=(Span<TOffset> x, Span<TOffset> y)
		{
			return !(x == y);
		}

		private readonly SpanFactory<TOffset> _spanFactory; 
		private readonly TOffset _start;
		private readonly TOffset _end;

		internal Span(SpanFactory<TOffset> spanFactory, TOffset start, TOffset end)
		{
			_spanFactory = spanFactory;
			_start = start;
			_end = end;
		}

		public Span(Span<TOffset> span)
			: this(span._spanFactory, span._start, span._end)
		{
		}

		public bool IsEmpty
		{
			get { return _spanFactory.Empty == this; }
		}

		public TOffset Start
		{
			get
			{
				return _start;
			}
		}

		public TOffset End
		{
			get
			{
				return _end;
			}
		}

		public int Length
		{
			get { return _spanFactory.CalcLength(_start, _end); }
		}

		public TOffset GetStart(Direction dir)
		{
			return dir == Direction.LeftToRight ? _start : _end;
		}

		public TOffset GetEnd(Direction dir)
		{
			return dir == Direction.LeftToRight ? _end : _start;
		}

		public bool Overlaps(Span<TOffset> other)
		{
			return _spanFactory.Compare(_start, other._end) <= 0
				&& ( _spanFactory.IncludeEndpoint ? _spanFactory.Compare(_end, other._start) >= 0 : _spanFactory.Compare(_end, other._start) > 0);
		}

		public bool Contains(Span<TOffset> other)
		{
			return _spanFactory.Compare(_start, other._start) <= 0 && _spanFactory.Compare(_end, other._end) >= 0;
		}

		public bool Contains(TOffset offset)
		{
			return _spanFactory.Compare(_start, offset) <= 0 && _spanFactory.Compare(_end, offset) >= 0;
		}

		public int CompareTo(Span<TOffset> other)
		{
			return CompareTo(other, Direction.LeftToRight);
		}

		public int CompareTo(Span<TOffset> other, Direction dir)
		{
			if (dir == Direction.LeftToRight)
			{
				if (_spanFactory.Compare(_start, other._start) < 0)
					return -1;

				if (_spanFactory.Compare(_start, other._start) > 0)
					return 1;

				if (_spanFactory.Compare(_end, other._end) > 0)
					return -1;

				if (_spanFactory.Compare(_end, other._end) < 0)
					return 1;
			}
			else
			{
				if (_spanFactory.Compare(_end, other._end) > 0)
					return -1;

				if (_spanFactory.Compare(_end, other._end) < 0)
					return 1;

				if (_spanFactory.Compare(_start, other._start) < 0)
					return -1;

				if (_spanFactory.Compare(_start, other._start) > 0)
					return 1;
			}

			return 0;
		}

		public int CompareTo(object other)
		{
			if (!(other is Span<TOffset>))
				throw new ArgumentException();
			return CompareTo((Span<TOffset>) other);
		}

		public override int GetHashCode()
		{
			return _start.GetHashCode() ^ _end.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return obj is Span<TOffset> && Equals((Span<TOffset>) obj);
		}

		public bool Equals(Span<TOffset> other)
		{
			return _start.Equals(other._start) && _end.Equals(other._end);
		}

		public override string ToString()
		{
			return string.Format("[{0}, {1}]", _start, _end);
		}
	}
}
