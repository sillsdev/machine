using SIL.Collections;

namespace SIL.Machine
{
	public class IntegerSpanFactory : SpanFactory<int>
	{
		private readonly Span<int> _empty;

		public IntegerSpanFactory()
		{
			_empty = new Span<int>(this, -1, -1);
		}

		public override Span<int> Empty
		{
			get { return _empty; }
		}

		public override int CalcLength(int start, int end)
		{
			return end - start;
		}

		public override Span<int> Create(int offset, Direction dir)
		{
			return Create(offset, offset + (dir == Direction.LeftToRight ? 1 : -1), dir);
		}
	}
}
