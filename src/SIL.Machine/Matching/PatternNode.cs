using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FiniteState;
using SIL.ObjectModel;

namespace SIL.Machine.Matching
{
    /// <summary>
    /// This is the abstract class that all phonetic pattern nodes extend.
    /// </summary>
    public abstract class PatternNode<TData, TOffset>
        : OrderedBidirTreeNode<PatternNode<TData, TOffset>>,
            ICloneable<PatternNode<TData, TOffset>>,
            IFreezable,
            IValueEquatable<PatternNode<TData, TOffset>>
        where TData : IAnnotatedData<TOffset>
    {
        private int _hashCode;

        protected PatternNode()
            : base(begin => new Margin()) { }

        protected PatternNode(IEnumerable<PatternNode<TData, TOffset>> children)
            : this()
        {
            foreach (PatternNode<TData, TOffset> child in children)
                Children.Add(child);
        }

        protected PatternNode(PatternNode<TData, TOffset> node)
            : this(node.Children.CloneItems())
        {
            Tag = node.Tag;
        }

        protected Pattern<TData, TOffset> Pattern
        {
            get { return Root as Pattern<TData, TOffset>; }
        }

        internal virtual State<TData, TOffset> GenerateNfa(
            Fst<TData, TOffset> fsa,
            State<TData, TOffset> startState,
            out bool hasVariables
        )
        {
            hasVariables = false;
            if (IsLeaf)
                return startState;

            foreach (PatternNode<TData, TOffset> child in Children.GetNodes(fsa.Direction))
            {
                bool childHasVariables;
                startState = child.GenerateNfa(fsa, startState, out childHasVariables);
                if (childHasVariables)
                    hasVariables = true;
            }

            return startState;
        }

        public PatternNode<TData, TOffset> Clone()
        {
            return CloneImpl();
        }

        public object Tag { get; set; }

        public bool IsFrozen { get; private set; }

        public void Freeze()
        {
            if (IsFrozen)
                return;
            IsFrozen = true;
            _hashCode = FreezeImpl();
        }

        public virtual bool ValueEquals(PatternNode<TData, TOffset> other)
        {
            if (IsLeaf && other.IsLeaf)
                return true;

            if (!IsLeaf && !other.IsLeaf && Children.Count == other.Children.Count)
                return Children.Zip(other.Children).All(tuple => tuple.Item1.ValueEquals(tuple.Item2));
            return false;
        }

        public int GetFrozenHashCode()
        {
            if (!IsFrozen)
            {
                throw new InvalidOperationException(
                    "The pattern node does not have a valid hash code, because it is mutable."
                );
            }

            return _hashCode;
        }

        protected void CheckFrozen()
        {
            if (IsFrozen)
                throw new InvalidOperationException("The pattern is immutable.");
        }

        protected virtual int FreezeImpl()
        {
            int code = 23;
            if (!IsLeaf)
            {
                foreach (PatternNode<TData, TOffset> child in Children)
                {
                    child.Freeze();
                    code = code * 31 + child.GetFrozenHashCode();
                }
            }
            return code;
        }

        protected override bool CanAdd(PatternNode<TData, TOffset> child)
        {
            return !IsFrozen;
        }

        protected override bool CanRemove(PatternNode<TData, TOffset> child)
        {
            return !IsFrozen;
        }

        protected override bool CanClear()
        {
            return !IsFrozen;
        }

        protected abstract PatternNode<TData, TOffset> CloneImpl();

        private class Margin : PatternNode<TData, TOffset>
        {
            protected override PatternNode<TData, TOffset> CloneImpl()
            {
                return new Margin();
            }

            protected override bool CanAdd(PatternNode<TData, TOffset> child)
            {
                return false;
            }
        }
    }
}
