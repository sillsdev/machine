using System;
using SIL.Collections;

namespace SIL.Machine.Annotations
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

		public SpanFactory<TOffset> SpanFactory
		{
			get { return _spanFactory; }
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

		public bool IsRange
		{
			get { return _spanFactory.IsRange(_start, _end); }
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
			return (_spanFactory.IncludeEndpoint ? _spanFactory.Comparer.Compare(_start, other._end) <= 0 : _spanFactory.Comparer.Compare(_start, other._end) < 0)
				&& (_spanFactory.IncludeEndpoint ? _spanFactory.Comparer.Compare(_end, other._start) >= 0 : _spanFactory.Comparer.Compare(_end, other._start) > 0);
		}

		public bool Overlaps(TOffset start, TOffset end)
		{
			return Overlaps(start, end, Direction.LeftToRight);
		}

		public bool Overlaps(TOffset start, TOffset end, Direction dir)
		{
			return Overlaps(_spanFactory.Create(start, end, dir));
		}

		public bool Contains(Span<TOffset> other)
		{
			return _spanFactory.Comparer.Compare(_start, other._start) <= 0 && _spanFactory.Comparer.Compare(_end, other._end) >= 0;
		}

		public bool Contains(TOffset offset)
		{
			return Contains(offset, Direction.LeftToRight);
		}

		public bool Contains(TOffset offset, Direction dir)
		{
			return Contains(_spanFactory.Create(offset, dir));
		}

		public bool Contains(TOffset start, TOffset end)
		{
			return Contains(start, end, Direction.LeftToRight);
		}

		public bool Contains(TOffset start, TOffset end, Direction dir)
		{
			return Contains(_spanFactory.Create(start, end, dir));
		}

		public int CompareTo(Span<TOffset> other)
		{
			int res = _spanFactory.Comparer.Compare(_start, other._start);
			if (res == 0)
				res = -_spanFactory.Comparer.Compare(_end, other._end);
			return res;
		}

		public int CompareTo(object other)
		{
			if (!(other is Span<TOffset>))
				throw new ArgumentException();
			return CompareTo((Span<TOffset>) other);
		}

		public override int GetHashCode()
		{
			int code = 23;
			code = code * 31 + (_start == null ? 0 : _spanFactory.EqualityComparer.GetHashCode(_start));
			code = code * 31 + (_end == null ? 0 : _spanFactory.EqualityComparer.GetHashCode(_end));
			return code;
		}

		public override bool Equals(object obj)
		{
			return obj is Span<TOffset> && Equals((Span<TOffset>) obj);
		}

		public bool Equals(Span<TOffset> other)
		{
			return _spanFactory.EqualityComparer.Equals(_start, other._start) && _spanFactory.EqualityComparer.Equals(_end, other._end);
		}

		public override string ToString()
		{
			return string.Format("[{0}, {1}]", _start, _end);
		}
	}
}
