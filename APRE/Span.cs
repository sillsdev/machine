using System;

namespace SIL.APRE
{
	public class Span<TOffset> : IComparable<Span<TOffset>>, IComparable, IEquatable<Span<TOffset>>
	{
		private readonly TOffset _start;
		private readonly TOffset _end;
		private readonly Func<TOffset, TOffset, int> _compare;
		private readonly Func<TOffset, TOffset, int> _calcLength;
		private readonly bool _includeEndpoint;

		internal Span(Func<TOffset, TOffset, int> compare, Func<TOffset, TOffset, int> calcLength, bool includeEndpoint,
			TOffset start, TOffset end)
		{
			_compare = compare;
			_calcLength = calcLength;
			_includeEndpoint = includeEndpoint;
			_start = start;
			_end = end;
		}

		public Span(Span<TOffset> span)
			: this(span._compare, span._calcLength, span._includeEndpoint, span._start, span._end)
		{
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

		public Span<TOffset> StartSpan
		{
			get { return new Span<TOffset>(_compare, _calcLength, _includeEndpoint, _start, _start); }
		}

		public Span<TOffset> EndSpan
		{
			get { return new Span<TOffset>(_compare, _calcLength, _includeEndpoint, _end, _end); }
		}

		public int Length
		{
			get { return _calcLength(_start, _end); }
		}

		public TOffset GetStart(Direction dir)
		{
			return dir == Direction.LeftToRight ? _start : _end;
		}

		public TOffset GetEnd(Direction dir)
		{
			return dir == Direction.LeftToRight ? _end : _start;
		}

		public Span<TOffset> GetStartSpan(Direction dir)
		{
			return dir == Direction.LeftToRight ? StartSpan : EndSpan;
		}

		public Span<TOffset> GetEndSpan(Direction dir)
		{
			return dir == Direction.LeftToRight ? EndSpan : StartSpan;
		}

		public bool Overlaps(Span<TOffset> other)
		{
			if (other == null)
				return false;
			return _compare(_start, other._end) <= 0
				&& ( _includeEndpoint ? _compare(_end, other._start) >= 0 : _compare(_end, other._start) > 0);
		}

		public bool Contains(Span<TOffset> other)
		{
			if (other == null)
				return false;
			return _compare(_start, other._start) <= 0 && _compare(_end, other._end) >= 0;
		}

		public int CompareTo(Span<TOffset> other)
		{
			return CompareTo(other, Direction.LeftToRight);
		}

		public int CompareTo(Span<TOffset> other, Direction dir)
		{
			if (other == null)
				return 1;

			if (dir == Direction.LeftToRight)
			{
				if (_compare(_start, other._start) < 0)
					return -1;

				if (_compare(_start, other._start) > 0)
					return 1;

				if (_compare(_end, other._end) < 0)
					return -1;

				if (_compare(_end, other._end) > 0)
					return 1;
			}
			else
			{
				if (_compare(_end, other._end) > 0)
					return -1;

				if (_compare(_end, other._end) < 0)
					return 1;

				if (_compare(_start, other._start) > 0)
					return -1;

				if (_compare(_start, other._start) < 0)
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
			if (obj == null)
				return false;
			return Equals(obj as Span<TOffset>);
		}

		public bool Equals(Span<TOffset> other)
		{
			if (other == null)
				return false;
			return _start.Equals(other._start) && _end.Equals(other._end);
		}

		public override string ToString()
		{
			return string.Format("[{0}, {1}]", _start, _end);
		}
	}
}
