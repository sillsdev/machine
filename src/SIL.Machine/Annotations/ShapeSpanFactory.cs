using System.Linq;
using SIL.Machine.DataStructures;
using SIL.ObjectModel;

namespace SIL.Machine.Annotations
{
	internal class ShapeSpanFactory : SpanFactory<ShapeNode>
	{
		public ShapeSpanFactory()
			: base(true, AnonymousComparer.Create<ShapeNode>(Compare), FreezableEqualityComparer<ShapeNode>.Default)
		{
			Null = new Span<ShapeNode>(null, null);
		}

		private static int Compare(ShapeNode x, ShapeNode y)
		{
			if (x == null)
				return y == null ? 0 : -1;

			if (y == null)
				return 1;

			return x.CompareTo(y);
		}

		public override bool IsEmptySpan(ShapeNode start, ShapeNode end)
		{
			return start == null || end == null;
		}

		public override int GetLength(ShapeNode start, ShapeNode end)
		{
			if (start == null || end == null)
				return 0;

			return start.GetNodes(end).Count();
		}

		public override Span<ShapeNode> Create(ShapeNode offset, Direction dir)
		{
			return Create(offset, offset, dir);
		}
	}
}
