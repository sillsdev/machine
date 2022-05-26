using System.Linq;
using SIL.Machine.DataStructures;
using SIL.ObjectModel;

namespace SIL.Machine.Annotations
{
    internal class ShapeRangeFactory : RangeFactory<ShapeNode>
    {
        public ShapeRangeFactory()
            : base(true, AnonymousComparer.Create<ShapeNode>(Compare), FreezableEqualityComparer<ShapeNode>.Default)
        {
            Null = new Range<ShapeNode>(null, null);
        }

        private static int Compare(ShapeNode x, ShapeNode y)
        {
            if (x == null)
                return y == null ? 0 : -1;

            if (y == null)
                return 1;

            return x.CompareTo(y);
        }

        public override bool IsEmptyRange(ShapeNode start, ShapeNode end)
        {
            return start == null || end == null;
        }

        public override int GetLength(ShapeNode start, ShapeNode end)
        {
            if (start == null || end == null)
                return 0;

            return start.GetNodes(end).Count();
        }

        public override Range<ShapeNode> Create(ShapeNode offset, Direction dir)
        {
            return Create(offset, offset, dir);
        }
    }
}
