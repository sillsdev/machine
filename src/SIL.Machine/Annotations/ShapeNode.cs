using System;
using System.Globalization;
using SIL.Extensions;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.ObjectModel;

namespace SIL.Machine.Annotations
{
    /// <summary>
    /// A node in a <see cref="Shape"/>. As of the RUSTIFY flat-shape rework (Phase 3b-impl, Stage 1)
    /// this is a <em>handle</em> into its owning <see cref="Shape"/>'s flat backing arrays rather than a
    /// self-contained doubly-linked-list node: the prev/next links and the frozen flag live in the owner
    /// arrays addressed by <see cref="Index"/>. The handle object added to a shape is stored as the
    /// canonical one-per-slot handle, so reference identity (and therefore <c>==</c>, dictionary keys and
    /// <see cref="Range{TOffset}"/> endpoint identity) is preserved exactly as before. <see cref="Tag"/>
    /// stays on the node so it survives a node being moved between shapes.
    /// </summary>
    public class ShapeNode
        : IOrderedBidirListNode<ShapeNode>,
            IComparable<ShapeNode>,
            IComparable,
            ICloneable<ShapeNode>,
            IValueEquatable<ShapeNode>,
            IFreezable
    {
        private readonly Annotation<ShapeNode> _ann;
        private int _tag;
        private bool _detachedFrozen;

        // The owning shape, or null when this node is detached (created but not yet added, or removed).
        internal Shape Owner { get; set; }

        // Slot index into the owner's flat arrays; -1 when detached.
        internal int Index { get; set; }

        public ShapeNode(FeatureStruct fs)
        {
            _ann = new Annotation<ShapeNode>(Range<ShapeNode>.Create(this), fs);
            _tag = int.MinValue;
            Index = -1;
        }

        protected ShapeNode(ShapeNode node)
            : this(node.Annotation.FeatureStruct.Clone())
        {
            _ann.Optional = node.Annotation.Optional;
        }

        public int Tag
        {
            get { return _tag; }
            internal set
            {
                CheckFrozen();
                _tag = value;
            }
        }

        public Annotation<ShapeNode> Annotation
        {
            get { return _ann; }
        }

        public IBidirList<ShapeNode> List
        {
            get { return Owner; }
        }

        public ShapeNode Next
        {
            get { return Owner?.GetNextLink(Index); }
        }

        public ShapeNode Prev
        {
            get { return Owner?.GetPrevLink(Index); }
        }

        public ShapeNode GetNext(Direction dir)
        {
            if (Owner == null)
                return null;
            return Owner.GetNext(this, dir);
        }

        public ShapeNode GetPrev(Direction dir)
        {
            if (Owner == null)
                return null;
            return Owner.GetPrev(this, dir);
        }

        public bool Remove()
        {
            if (Owner == null)
                return false;
            return Owner.Remove(this);
        }

        public void AddAfter(ShapeNode newNode, Direction dir)
        {
            if (Owner == null)
                return;
            Owner.AddAfter(this, newNode, dir);
        }

        public void AddAfter(ShapeNode newNode)
        {
            AddAfter(newNode, Direction.LeftToRight);
        }

        public int CompareTo(ShapeNode other)
        {
            if (other.List != List)
                throw new ArgumentException("Only nodes from the same list can be compared.", "other");
            return Tag.CompareTo(other.Tag);
        }

        public int CompareTo(ShapeNode other, Direction dir)
        {
            if (other.List != List)
                throw new ArgumentException("Only nodes from the same list can be compared.", "other");

            int res = Tag.CompareTo(other.Tag);
            return dir == Direction.LeftToRight ? res : -res;
        }

        public int CompareTo(object other)
        {
            if (!(other is ShapeNode))
                throw new ArgumentException("other is not an instance of a ShapeNode.", "other");
            return CompareTo((ShapeNode)other);
        }

        public ShapeNode Clone()
        {
            return new ShapeNode(this);
        }

        public bool ValueEquals(ShapeNode other)
        {
            if (other == null)
                return false;

            if (this == other)
                return true;

            if (IsFrozen)
                return _tag == other._tag;

            return List.IndexOf(this) == other.List.IndexOf(other);
        }

        public override string ToString()
        {
            if (List != null)
            {
                if (List.Begin == this)
                    return "B";
                if (List.End == this)
                    return "E";
                int i = 0;
                foreach (ShapeNode node in List)
                {
                    if (node == this)
                        return i.ToString(CultureInfo.InvariantCulture);
                    i++;
                }
            }

            return base.ToString();
        }

        private void CheckFrozen()
        {
            if (IsFrozen)
                throw new InvalidOperationException("The shape node is immutable.");
        }

        public bool IsFrozen
        {
            get { return Owner != null ? Owner.IsNodeFrozen(Index) : _detachedFrozen; }
        }

        public void Freeze()
        {
            if (IsFrozen)
                return;
            if (Owner != null)
                Owner.SetNodeFrozen(Index);
            else
                _detachedFrozen = true;
        }

        public int GetFrozenHashCode()
        {
            if (!IsFrozen)
            {
                throw new InvalidOperationException(
                    "The shape node does not have a valid hash code, because it is mutable."
                );
            }

            return _tag;
        }
    }
}
