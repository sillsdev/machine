using System;
using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
    public class FstResult<TData, TOffset> : IEquatable<FstResult<TData, TOffset>>
    {
        private readonly IEqualityComparer<TOffset> _offsetEqualityComparer;
        private readonly Register<TOffset>[,] _registers;
        private readonly TData _output;
        private readonly VariableBindings _varBindings;
        private readonly string _id;
        private readonly int _priority;
        private readonly bool _isLazy;
        private readonly Annotation<TOffset> _nextAnn;
        private readonly int[] _priorities;
        private readonly int _order;

        internal FstResult(
            IEqualityComparer<TOffset> offsetEqualityComparer,
            string id,
            Register<TOffset>[,] registers,
            TData output,
            VariableBindings varBindings,
            int priority,
            bool isLazy,
            Annotation<TOffset> nextAnn,
            int[] priorities,
            int order
        )
        {
            _offsetEqualityComparer = offsetEqualityComparer;
            _id = id;
            _registers = registers;
            _output = output;
            _varBindings = varBindings;
            _priority = priority;
            _isLazy = isLazy;
            _nextAnn = nextAnn;
            _priorities = priorities;
            _order = order;
        }

        public string ID
        {
            get { return _id; }
        }

        public Register<TOffset>[,] Registers
        {
            get { return _registers; }
        }

        public TData Output
        {
            get { return _output; }
        }

        public VariableBindings VariableBindings
        {
            get { return _varBindings; }
        }

        public int Priority
        {
            get { return _priority; }
        }

        public Annotation<TOffset> NextAnnotation
        {
            get { return _nextAnn; }
        }

        internal bool IsLazy
        {
            get { return _isLazy; }
        }

        internal int[] Priorities
        {
            get { return _priorities; }
        }

        internal int Order
        {
            get { return _order; }
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as FstResult<TData, TOffset>);
        }

        public bool Equals(FstResult<TData, TOffset> other)
        {
            if (other == null)
                return false;

            if (_id != other._id)
                return false;

            if (!RegistersEqual(_registers, other._registers, _offsetEqualityComparer))
                return false;

            return EqualityComparer<TData>.Default.Equals(_output, other._output);
        }

        public override int GetHashCode()
        {
            int code = 23;
            code = code * 31 + (_id == null ? 0 : _id.GetHashCode());
            code = code * 31 + RegistersGetHashCode(_registers, _offsetEqualityComparer);
            code = code * 31 * EqualityComparer<TData>.Default.GetHashCode(_output);
            return code;
        }

        // The public Registers property keeps the Register<TOffset>[,] shape for API compatibility, so equality
        // is compared inline here rather than via the internal (flat-array) RegistersEqualityComparer.
        private static bool RegistersEqual(
            Register<TOffset>[,] x,
            Register<TOffset>[,] y,
            IEqualityComparer<TOffset> offsetEqualityComparer
        )
        {
            for (int i = 0; i < x.GetLength(0); i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (!x[i, j].ValueEquals(y[i, j], offsetEqualityComparer))
                        return false;
                }
            }
            return true;
        }

        private static int RegistersGetHashCode(
            Register<TOffset>[,] registers,
            IEqualityComparer<TOffset> offsetEqualityComparer
        )
        {
            int code = 23;
            for (int i = 0; i < registers.GetLength(0); i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (registers[i, j].HasOffset)
                    {
                        code = code * 31 + offsetEqualityComparer.GetHashCode(registers[i, j].Offset);
                        code = code * 31 + registers[i, j].IsStart.GetHashCode();
                    }
                    else
                    {
                        code = code * 31 + 0;
                    }
                }
            }
            return code;
        }
    }
}
