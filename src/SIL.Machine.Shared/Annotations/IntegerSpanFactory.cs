using SIL.Machine.DataStructures;

namespace SIL.Machine.Annotations
{
	public class IntegerSpanFactory : SpanFactory<int>
	{
		public IntegerSpanFactory()
		{
			Empty = new Span<int>(this, -1, -1);
		}

		protected internal override int CalcLength(int start, int end)
		{
			return end - start;
		}

		public override Span<int> Create(int offset, Direction dir)
		{
			return Create(offset, offset + (dir == Direction.LeftToRight ? 1 : -1), dir);
		}
	}
}
