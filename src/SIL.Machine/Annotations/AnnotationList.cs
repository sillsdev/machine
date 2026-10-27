using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.ObjectModel;

namespace SIL.Machine.Annotations
{
    public class AnnotationList<TOffset>
        : BidirList<Annotation<TOffset>>,
            ICloneable<AnnotationList<TOffset>>,
            IFreezable,
            IValueEquatable<AnnotationList<TOffset>>
    {
        private int _currentID;
        private readonly Annotation<TOffset> _parent;
        private int _hashCode;

        public AnnotationList()
            : base(new AnnotationComparer(), begin => new Annotation<TOffset>(Range<TOffset>.Null)) { }

        protected AnnotationList(AnnotationList<TOffset> annList)
            : this()
        {
            AddRange(annList.Select(ann => ann.Clone()));
        }

        internal AnnotationList(Annotation<TOffset> parent)
            : this()
        {
            _parent = parent;
        }

        internal Annotation<TOffset> Parent
        {
            get { return _parent; }
        }

        public bool IsFrozen { get; private set; }

        public void Freeze()
        {
            if (IsFrozen)
                return;
            IsFrozen = true;
            _hashCode = 23;
            foreach (Annotation<TOffset> ann in this)
            {
                ann.Freeze();
                _hashCode = _hashCode * 31 + ann.GetFrozenHashCode();
            }
        }

        public bool ValueEquals(AnnotationList<TOffset> other)
        {
            if (other == null)
                return false;

            if (Count != other.Count)
                return false;

            return this.SequenceEqual(other, FreezableEqualityComparer<Annotation<TOffset>>.Default);
        }

        public int GetFrozenHashCode()
        {
            if (!IsFrozen)
            {
                throw new InvalidOperationException(
                    "The annotation list does not have a valid hash code, because it is mutable."
                );
            }
            return _hashCode;
        }

        private void CheckFrozen()
        {
            if (IsFrozen)
                throw new InvalidOperationException("The annotation list is immutable.");
        }

        public Annotation<TOffset> Add(Range<TOffset> range, FeatureStruct fs)
        {
            return Add(range, fs, false);
        }

        public Annotation<TOffset> Add(TOffset start, TOffset end, FeatureStruct fs)
        {
            return Add(start, end, fs, false);
        }

        public Annotation<TOffset> Add(TOffset offset, FeatureStruct fs)
        {
            return Add(Range<TOffset>.Create(offset), fs);
        }

        public Annotation<TOffset> Add(Range<TOffset> range, FeatureStruct fs, bool optional)
        {
            var ann = new Annotation<TOffset>(range, fs) { Optional = optional };
            Add(ann);
            return ann;
        }

        public Annotation<TOffset> Add(TOffset start, TOffset end, FeatureStruct fs, bool optional)
        {
            return Add(Range<TOffset>.Create(start, end), fs, optional);
        }

        public Annotation<TOffset> Add(TOffset offset, FeatureStruct fs, bool optional)
        {
            return Add(Range<TOffset>.Create(offset), fs, optional);
        }

        public override void Add(Annotation<TOffset> node)
        {
            Add(node, true);
        }

        public void Add(Annotation<TOffset> node, bool subsume)
        {
            CheckFrozen();
            if (_parent != null && !_parent.Range.Contains(node.Range))
            {
                throw new ArgumentException(
                    "The new annotation must be within the range of the parent annotation.",
                    nameof(node)
                );
            }

            node.Remove(false);
            if (subsume)
            {
                foreach (Annotation<TOffset> ann in GetNodes(node.Range).ToArray())
                    node.Children.Add(ann);
                base.Add(node);
                node.ListID = _currentID++;
                for (Annotation<TOffset> ann = node.Prev; ann != Begin; ann = ann.Prev)
                {
                    if (ann.Range.Contains(node.Range))
                    {
                        ann.Children.Add(node);
                        break;
                    }
                }
            }
            else
            {
                base.Add(node);
                node.ListID = _currentID++;
            }
        }

        public override bool Remove(Annotation<TOffset> node)
        {
            return Remove(node, true);
        }

        public bool Remove(Annotation<TOffset> node, bool preserveChildren)
        {
            CheckFrozen();
            if (base.Remove(node))
            {
                if (preserveChildren)
                {
                    foreach (Annotation<TOffset> ann in node.Children.ToArray())
                        Add(ann, false);
                }

                return true;
            }

            return false;
        }

        public bool Find(TOffset offset, out Annotation<TOffset> result)
        {
            return Find(offset, Direction.LeftToRight, out result);
        }

        public bool Find(TOffset offset, Direction dir, out Annotation<TOffset> result)
        {
            if (Count == 0)
            {
                result = null;
                return false;
            }

            TOffset lastOffset = GetLast(dir).Range.GetEnd(dir);
            if (
                !Range<TOffset>.IsValidRange(offset, lastOffset, dir)
                || Range<TOffset>.IsEmptyRange(offset, lastOffset, dir)
            )
            {
                result = GetLast(dir);
                return false;
            }

            if (dir == Direction.LeftToRight)
            {
                if (Find(new Annotation<TOffset>(Range<TOffset>.Create(offset, lastOffset)), out result))
                    return true;
            }
            else
            {
                Range<TOffset> offsetRange = Range<TOffset>.Create(offset, Direction.RightToLeft);
                if (Find(new Annotation<TOffset>(offsetRange), Direction.RightToLeft, out result))
                    return true;

                if (result != First && result.Prev.Range.Contains(offsetRange))
                    result = result.Prev;
            }

            if (!offset.Equals(result.Range.GetStart(dir)) && offset.Equals(result.GetNext(dir).Range.GetStart(dir)))
                result = result.GetNext(dir);
            return offset.Equals(result.Range.GetStart(dir));
        }

        public bool FindDepthFirst(TOffset offset, out Annotation<TOffset> result)
        {
            return FindDepthFirst(offset, Direction.LeftToRight, out result);
        }

        public bool FindDepthFirst(TOffset offset, Direction dir, out Annotation<TOffset> result)
        {
            if (Find(offset, dir, out result))
                return true;

            // Several containers can share the same range while only one of them owns the children that lead to the
            // offset, so every one of them has to be tried, not just the one Find happened to return.
            Annotation<TOffset> candidate = result;
            foreach (Annotation<TOffset> tied in GetSameRange(candidate))
            {
                if (
                    !tied.IsLeaf
                    && tied.Range.Contains(offset, dir)
                    && tied.Children.FindDepthFirst(offset, dir, out Annotation<TOffset> inner)
                )
                {
                    result = inner;
                    return true;
                }
            }

            if (!candidate.IsLeaf && candidate.Range.Contains(offset, dir))
                return candidate.Children.FindDepthFirst(offset, dir, out result);

            return false;
        }

        private IEnumerable<Annotation<TOffset>> GetSameRange(Annotation<TOffset> ann)
        {
            yield return ann;
            if (ann == Begin || ann == End)
                yield break;

            for (
                Annotation<TOffset> cur = ann.Prev;
                cur != Begin && cur.Range.CompareTo(ann.Range) == 0;
                cur = cur.Prev
            )
            {
                yield return cur;
            }

            for (Annotation<TOffset> cur = ann.Next; cur != End && cur.Range.CompareTo(ann.Range) == 0; cur = cur.Next)
            {
                yield return cur;
            }
        }

        public IEnumerable<Annotation<TOffset>> GetNodes(TOffset start, TOffset end)
        {
            return GetNodes(Range<TOffset>.Create(start, end));
        }

        public IEnumerable<Annotation<TOffset>> GetNodes(Range<TOffset> range)
        {
            return GetNodes(range, Direction.LeftToRight);
        }

        public IEnumerable<Annotation<TOffset>> GetNodes(TOffset start, TOffset end, Direction dir)
        {
            return GetNodes(Range<TOffset>.Create(start, end, dir), dir);
        }

        public IEnumerable<Annotation<TOffset>> GetNodes(Range<TOffset> range, Direction dir)
        {
            if (Count == 0)
                return Enumerable.Empty<Annotation<TOffset>>();

            Annotation<TOffset> startAnn;
            if (!Find(range.Start, Direction.LeftToRight, out startAnn))
                startAnn = startAnn.Next;

            if (startAnn == GetEnd(dir))
                return Enumerable.Empty<Annotation<TOffset>>();

            Annotation<TOffset> endAnn;
            if (!Find(range.End, Direction.RightToLeft, out endAnn))
                endAnn = endAnn.Prev;

            if (endAnn == GetBegin(dir))
                return Enumerable.Empty<Annotation<TOffset>>();

            if (startAnn.CompareTo(endAnn) > 0)
                return Enumerable.Empty<Annotation<TOffset>>();

            return this.GetNodes(
                    dir == Direction.LeftToRight ? startAnn : endAnn,
                    dir == Direction.LeftToRight ? endAnn : startAnn,
                    dir
                )
                .Where(ann => range.Contains(ann.Range));
        }

        public override void Clear()
        {
            CheckFrozen();
            base.Clear();
        }

        public AnnotationList<TOffset> Clone()
        {
            return new AnnotationList<TOffset>(this);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (Annotation<TOffset> ann in this)
            {
                if (!first)
                    sb.Append(", ");
                sb.Append(ann);
                first = false;
            }
            sb.Append("}");
            return sb.ToString();
        }

        private class AnnotationComparer : IComparer<Annotation<TOffset>>
        {
            public int Compare(Annotation<TOffset> x, Annotation<TOffset> y)
            {
                if (x == null)
                    return y == null ? 0 : -1;

                return x.CompareTo(y);
            }
        }
    }
}
