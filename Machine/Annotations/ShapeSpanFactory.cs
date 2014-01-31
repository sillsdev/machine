using System.Collections.Generic;
using System.Linq;
using SIL.Collections;

namespace SIL.Machine.Annotations
{
	public class ShapeSpanFactory : SpanFactory<ShapeNode>
	{
		private readonly Span<ShapeNode> _empty; 

		public ShapeSpanFactory()
			: base(true, AnonymousComparer.Create<ShapeNode>(Compare), EqualityComparer<ShapeNode>.Default)
		{
			_empty = new Span<ShapeNode>(this, null, null);
		}

		public override Span<ShapeNode> Empty
		{
			get { return _empty; }
		}

		private static int Compare(ShapeNode x, ShapeNode y)
		{
			if (x == null)
				return y == null ? 0 : -1;

			if (y == null)
				return 1;

			return x.CompareTo(y);
		}

		public override int CalcLength(ShapeNode start, ShapeNode end)
		{
			if (start == null || end == null)
				return 0;

			return start.GetNodes(end).Count();
		}

		public override bool IsRange(ShapeNode start, ShapeNode end)
		{
			return start != null && end != null;
		}

		public override Span<ShapeNode> Create(ShapeNode offset, Direction dir)
		{
			return Create(offset, offset, dir);
		}
	}
}
