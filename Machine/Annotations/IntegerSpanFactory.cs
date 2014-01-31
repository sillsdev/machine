using SIL.Collections;

namespace SIL.Machine.Annotations
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

		public override bool IsRange(int start, int end)
		{
			return start != end;
		}

		public override Span<int> Create(int offset, Direction dir)
		{
			return Create(offset, offset + (dir == Direction.LeftToRight ? 1 : -1), dir);
		}
	}
}
