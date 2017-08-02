using System;
using SIL.Machine.DataStructures;

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

		internal Span(SpanFactory<TOffset> spanFactory, TOffset start, TOffset end)
		{
			SpanFactory = spanFactory;
			Start = start;
			End = end;
		}

		public Span(Span<TOffset> span)
			: this(span.SpanFactory, span.Start, span.End)
		{
		}

		public SpanFactory<TOffset> SpanFactory { get; }

		public bool IsEmpty => SpanFactory.Empty == this;

		public TOffset Start { get; }

		public TOffset End { get; }

		public int Length => SpanFactory.CalcLength(Start, End);

		public TOffset GetStart(Direction dir)
		{
			return dir == Direction.LeftToRight ? Start : End;
		}

		public TOffset GetEnd(Direction dir)
		{
			return dir == Direction.LeftToRight ? End : Start;
		}

		public bool Overlaps(Span<TOffset> other)
		{
			if (SpanFactory.IncludeEndpoint)
			{
				return SpanFactory.Comparer.Compare(Start, other.End) <= 0
					&& SpanFactory.Comparer.Compare(End, other.Start) >= 0;
			}

			return SpanFactory.Comparer.Compare(Start, other.End) < 0
				&& SpanFactory.Comparer.Compare(End, other.Start) > 0;
		}

		public bool Overlaps(TOffset start, TOffset end)
		{
			return Overlaps(start, end, Direction.LeftToRight);
		}

		public bool Overlaps(TOffset start, TOffset end, Direction dir)
		{
			return Overlaps(SpanFactory.Create(start, end, dir));
		}

		public bool Contains(Span<TOffset> other)
		{
			return SpanFactory.Comparer.Compare(Start, other.Start) <= 0 && SpanFactory.Comparer.Compare(End, other.End) >= 0;
		}

		public bool Contains(TOffset offset)
		{
			return Contains(offset, Direction.LeftToRight);
		}

		public bool Contains(TOffset offset, Direction dir)
		{
			return Contains(SpanFactory.Create(offset, dir));
		}

		public bool Contains(TOffset start, TOffset end)
		{
			return Contains(start, end, Direction.LeftToRight);
		}

		public bool Contains(TOffset start, TOffset end, Direction dir)
		{
			return Contains(SpanFactory.Create(start, end, dir));
		}

		public int CompareTo(Span<TOffset> other)
		{
			int res = SpanFactory.Comparer.Compare(Start, other.Start);
			if (res == 0)
				res = -SpanFactory.Comparer.Compare(End, other.End);
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
			code = code * 31 + (Start == null ? 0 : SpanFactory.EqualityComparer.GetHashCode(Start));
			code = code * 31 + (End == null ? 0 : SpanFactory.EqualityComparer.GetHashCode(End));
			return code;
		}

		public override bool Equals(object obj)
		{
			return obj is Span<TOffset> && Equals((Span<TOffset>) obj);
		}

		public bool Equals(Span<TOffset> other)
		{
			return SpanFactory.EqualityComparer.Equals(Start, other.Start)
				&& SpanFactory.EqualityComparer.Equals(End, other.End);
		}

		public override string ToString()
		{
			return string.Format("[{0}, {1}]", Start, End);
		}
	}
}
