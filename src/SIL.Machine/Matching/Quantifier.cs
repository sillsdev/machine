using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FiniteState;
using SIL.ObjectModel;

namespace SIL.Machine.Matching
{
    /// <summary>
    /// This class represents a nested phonetic pattern within another phonetic pattern.
    /// </summary>
    public class Quantifier<TData, TOffset>
        : PatternNode<TData, TOffset>,
            ICloneable<Quantifier<TData, TOffset>>,
            IValueEquatable<Quantifier<TData, TOffset>>
        where TData : IAnnotatedData<TOffset>
    {
        public const int Infinite = -1;

        private int _minOccur;
        private int _maxOccur;
        private bool _greedy;

        public Quantifier()
            : this(0, Infinite) { }

        public Quantifier(PatternNode<TData, TOffset> node)
            : this(0, Infinite, node) { }

        public Quantifier(int minOccur, int maxOccur)
            : this(minOccur, maxOccur, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Quantifier{TData, TOffset}"/> class.
        /// </summary>
        /// <param name="minOccur">The minimum number of occurrences.</param>
        /// <param name="maxOccur">The maximum number of occurrences.</param>

        /// <param name="node">The pattern node.</param>
        public Quantifier(int minOccur, int maxOccur, PatternNode<TData, TOffset> node)
            : base(node == null ? Enumerable.Empty<PatternNode<TData, TOffset>>() : node.ToEnumerable())
        {
            _greedy = true;
            _minOccur = minOccur;
            _maxOccur = maxOccur;
        }

        protected Quantifier(Quantifier<TData, TOffset> quantifier)
            : base(quantifier)
        {
            _minOccur = quantifier._minOccur;
            _maxOccur = quantifier._maxOccur;
            _greedy = quantifier._greedy;
        }

        /// <summary>
        /// Gets the minimum number of occurrences of this pattern.
        /// </summary>
        /// <value>The minimum number of occurrences.</value>
        public int MinOccur
        {
            get { return _minOccur; }
            set
            {
                CheckFrozen();
                _minOccur = value;
            }
        }

        /// <summary>
        /// Gets the maximum number of occurrences of this pattern.
        /// </summary>
        /// <value>The maximum number of occurrences.</value>
        public int MaxOccur
        {
            get { return _maxOccur; }
            set
            {
                CheckFrozen();
                _maxOccur = value;
            }
        }

        public bool IsGreedy
        {
            get { return _greedy; }
            set
            {
                CheckFrozen();
                _greedy = value;
            }
        }

        protected override bool CanAdd(PatternNode<TData, TOffset> child)
        {
            if (!base.CanAdd(child) || child is Pattern<TData, TOffset>)
                return false;
            return true;
        }

        internal override State<TData, TOffset> GenerateNfa(
            Fst<TData, TOffset> fsa,
            State<TData, TOffset> startState,
            out bool hasVariables
        )
        {
            hasVariables = false;
            ArcPriorityType priorityType = IsGreedy ? ArcPriorityType.High : ArcPriorityType.Low;
            State<TData, TOffset> endState;
            State<TData, TOffset> currentState = startState;
            var startStates = new List<State<TData, TOffset>>();
            if (MinOccur == 0)
            {
                endState = startState.Arcs.Add(fsa.CreateState(), priorityType);
                endState = base.GenerateNfa(fsa, endState, out hasVariables);
                startStates.Add(currentState);
            }
            else
            {
                endState = startState;
                for (int i = 0; i < MinOccur; i++)
                {
                    currentState = endState;
                    endState = base.GenerateNfa(fsa, currentState, out hasVariables);
                }
            }

            if (MaxOccur == Infinite)
            {
                endState.Arcs.Add(currentState, priorityType);
                if (MinOccur == 0)
                    endState = fsa.CreateState();
            }
            else
            {
                int numCopies = MaxOccur - MinOccur;
                if (MinOccur == 0)
                    numCopies--;
                for (int i = 1; i <= numCopies; i++)
                {
                    startStates.Add(endState);
                    endState = endState.Arcs.Add(fsa.CreateState(), priorityType);
                    endState = base.GenerateNfa(fsa, endState, out hasVariables);
                }
            }
            foreach (State<TData, TOffset> state in startStates)
                state.Arcs.Add(endState);

            return endState;
        }

        protected override int FreezeImpl()
        {
            int code = base.FreezeImpl();
            code = code * 31 + _minOccur.GetHashCode();
            code = code * 31 + _maxOccur.GetHashCode();
            code = code * 31 + _greedy.GetHashCode();
            return code;
        }

        public new Quantifier<TData, TOffset> Clone()
        {
            return new Quantifier<TData, TOffset>(this);
        }

        protected override PatternNode<TData, TOffset> CloneImpl()
        {
            return Clone();
        }

        public override bool ValueEquals(PatternNode<TData, TOffset> other)
        {
            var otherQuant = other as Quantifier<TData, TOffset>;
            return otherQuant != null && ValueEquals(otherQuant);
        }

        public bool ValueEquals(Quantifier<TData, TOffset> other)
        {
            return MinOccur == other.MinOccur
                && MaxOccur == other.MaxOccur
                && IsGreedy == other.IsGreedy
                && base.ValueEquals(other);
        }

        public override string ToString()
        {
            string quantifierStr;
            if (MinOccur == 0 && MaxOccur == Infinite)
                quantifierStr = "*";
            else if (MinOccur == 1 && MaxOccur == Infinite)
                quantifierStr = "+";
            else if (MinOccur == 0 && MaxOccur == 1)
                quantifierStr = "?";
            else if (MaxOccur == Infinite)
                quantifierStr = string.Format("[{0},]", MinOccur);
            else
                quantifierStr = string.Format("[{0},{1}]", MinOccur, MaxOccur);
            return string.Concat(Children) + quantifierStr;
        }
    }
}
