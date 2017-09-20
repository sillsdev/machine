using System;
using SIL.Machine.DataStructures;

namespace SIL.Machine.Annotations
{
	public struct Span<TOffset> : IComparable<Span<TOffset>>, IComparable, IEquatable<Span<TOffset>>
	{
		private static readonly SpanFactory<TOffset> Factory = CreateFactory();

		private static SpanFactory<TOffset> CreateFactory()
		{
			Type type = typeof(TOffset);
			if (type == typeof(int))
				return new IntegerSpanFactory() as SpanFactory<TOffset>;
#if !BRIDGE_NET
			if (type == typeof(ShapeNode))
				return new ShapeSpanFactory() as SpanFactory<TOffset>;
#endif

			throw new NotSupportedException();
		}

		public static Span<TOffset> Create(TOffset start, TOffset end, Direction dir = Direction.LeftToRight)
		{
			return Factory.Create(start, end, dir);
		}

		public static Span<TOffset> Create(TOffset offset, Direction dir = Direction.LeftToRight)
		{
			return Factory.Create(offset, dir);
		}

		public static bool IsValidSpan(TOffset start, TOffset end, Direction dir = Direction.LeftToRight)
		{
			if (dir == Direction.RightToLeft)
			{
				TOffset temp = start;
				start = end;
				end = temp;
			}

			return Factory.IsValidSpan(start, end);
		}

		public static bool IsEmptySpan(TOffset start, TOffset end, Direction dir = Direction.LeftToRight)
		{
			if (dir == Direction.RightToLeft)
			{
				TOffset temp = start;
				start = end;
				end = temp;
			}

			return Factory.IsEmptySpan(start, end);
		}

		public static int GetLength(TOffset start, TOffset end, Direction dir = Direction.LeftToRight)
		{
			if (dir == Direction.RightToLeft)
			{
				TOffset temp = start;
				start = end;
				end = temp;
			}

			return Factory.GetLength(start, end);
		}

		public static Span<TOffset> Null => Factory.Null;

		public static bool operator ==(Span<TOffset> x, Span<TOffset> y)
		{
			return x.Equals(y);
		}
		public static bool operator !=(Span<TOffset> x, Span<TOffset> y)
		{
			return !(x == y);
		}

		internal Span(TOffset start, TOffset end)
		{
			Start = start;
			End = end;
		}

		public Span(Span<TOffset> span)
			: this(span.Start, span.End)
		{
		}

		public TOffset Start { get; }

		public TOffset End { get; }

		public int Length => Factory.GetLength(Start, End);

		public bool IsEmpty => Factory.IsEmptySpan(Start, End);

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
			if (this == Null)
				return other == Null;
			if (other == Null)
				return false;

			if (Factory.IncludeEndpoint)
			{
				return Factory.Comparer.Compare(Start, other.End) <= 0
					&& Factory.Comparer.Compare(End, other.Start) >= 0;
			}

			return Factory.Comparer.Compare(Start, other.End) < 0
				&& Factory.Comparer.Compare(End, other.Start) > 0;
		}

		public bool Overlaps(TOffset start, TOffset end)
		{
			return Overlaps(start, end, Direction.LeftToRight);
		}

		public bool Overlaps(TOffset start, TOffset end, Direction dir)
		{
			return Overlaps(Factory.Create(start, end, dir));
		}

		public bool Contains(Span<TOffset> other)
		{
			if (this == Null)
				return other == Null;
			if (other == Null)
				return false;

			return Factory.Comparer.Compare(Start, other.Start) <= 0
				&& Factory.Comparer.Compare(End, other.End) >= 0;
		}

		public bool Contains(TOffset offset)
		{
			return Contains(offset, Direction.LeftToRight);
		}

		public bool Contains(TOffset offset, Direction dir)
		{
			return Contains(Factory.Create(offset, dir));
		}

		public bool Contains(TOffset start, TOffset end)
		{
			return Contains(start, end, Direction.LeftToRight);
		}

		public bool Contains(TOffset start, TOffset end, Direction dir)
		{
			return Contains(Factory.Create(start, end, dir));
		}

		public int CompareTo(Span<TOffset> other)
		{
			if (this == Null)
				return other == Null ? 0 : -1;
			if (other == Null)
				return 1;

			int res = Factory.Comparer.Compare(Start, other.Start);
			if (res == 0)
				res = -Factory.Comparer.Compare(End, other.End);
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
			code = code * 31 + (Start == null ? 0 : Factory.EqualityComparer.GetHashCode(Start));
			code = code * 31 + (End == null ? 0 : Factory.EqualityComparer.GetHashCode(End));
			return code;
		}

		public override bool Equals(object obj)
		{
			return obj is Span<TOffset> && Equals((Span<TOffset>) obj);
		}

		public bool Equals(Span<TOffset> other)
		{
			return Factory.EqualityComparer.Equals(Start, other.Start)
				&& Factory.EqualityComparer.Equals(End, other.End);
		}

		public override string ToString()
		{
			return string.Format("[{0}, {1}]", Start, End);
		}
	}
}
