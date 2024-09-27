using System;
using System.Globalization;
using SIL.Extensions;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.ObjectModel;

namespace SIL.Machine.Annotations
{
    public class ShapeNode
        : OrderedBidirListNode<ShapeNode>,
            IComparable<ShapeNode>,
            IComparable,
            ICloneable<ShapeNode>,
            IValueEquatable<ShapeNode>,
            IFreezable
    {
        private readonly Annotation<ShapeNode> _ann;
        private int _tag;

        public ShapeNode(FeatureStruct fs)
        {
            _ann = new Annotation<ShapeNode>(Range<ShapeNode>.Create(this), fs);
            _tag = int.MinValue;
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

        /// <summary>
        /// Whether this is an iterative node in a lexical pattern.
        /// </summary>
        public bool Iterative
        {
            get { return Annotation.Data != null;  }
            set
            {
                if (value)
                    Annotation.Data = value;
                else
                    Annotation.Data = null;
            }
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

        public bool IsFrozen { get; private set; }

        public void Freeze()
        {
            if (IsFrozen)
                return;
            IsFrozen = true;
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
