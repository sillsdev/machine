using SIL.Machine.DataStructures;

namespace SIL.Machine.Annotations
{
	internal class IntegerSpanFactory : SpanFactory<int>
	{
		public IntegerSpanFactory()
		{
			Null = new Span<int>(-1, -1);
		}

		public override int GetLength(int start, int end)
		{
			return end - start;
		}

		public override Span<int> Create(int offset, Direction dir)
		{
			return Create(offset, offset + (dir == Direction.LeftToRight ? 1 : -1), dir);
		}
	}
}
