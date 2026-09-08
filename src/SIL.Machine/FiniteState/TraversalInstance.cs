using System;
using System.Collections.Generic;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.ObjectModel;

namespace SIL.Machine.FiniteState
{
    internal abstract class TraversalInstance<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        private readonly Register<TOffset>[] _registers;
        private readonly List<int> _priorities;

        protected TraversalInstance(int registerCount, bool deterministic)
        {
            // Flat layout: index = RegisterArray.Idx(registerIndex, startOrEnd). A 1-D array is used instead of
            // Register<TOffset>[,] because multi-dimensional array allocation goes through the slow generic
            // Array.CreateInstance path, and this array is allocated/cloned on every traversal instance.
            _registers = new Register<TOffset>[registerCount * 2];
            if (!deterministic)
                _priorities = new List<int>();
        }

        public State<TData, TOffset> State { get; set; }
        public int AnnotationIndex { get; set; }
        public TData Output { get; set; }

        public IList<int> Priorities
        {
            get { return _priorities; }
        }

        public Register<TOffset>[] Registers
        {
            get { return _registers; }
        }

        public VariableBindings VariableBindings { get; set; }

        public virtual void CopyTo(TraversalInstance<TData, TOffset> other)
        {
            other.State = State;
            other.AnnotationIndex = AnnotationIndex;
            var cloneable = Output as ICloneable<TData>;
            other.Output = cloneable != null ? cloneable.Clone() : Output;
            if (_priorities != null)
                other.Priorities.AddRange(_priorities);
            Array.Copy(_registers, other.Registers, _registers.Length);
        }

        public virtual void Clear()
        {
            State = null;
            Output = default;
            _priorities?.Clear();
            VariableBindings = null;
        }
    }
}
