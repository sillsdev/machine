using System.Collections.Generic;

namespace SIL.Machine.FiniteState
{
    // Registers is a flat Register<TOffset>[] of length 2*registerCount (see RUSTIFY MD-array note
    // on TraversalInstance/Fst.Transduce): index i's pair lives at [2*i] (start) / [2*i+1] (end).
    internal class RegistersEqualityComparer<TOffset> : IEqualityComparer<Register<TOffset>[]>
    {
        private readonly IEqualityComparer<TOffset> _offsetEqualityComparer;

        // Devirtualizes the common case: EqualityComparer<TOffset>.Default.Equals/GetHashCode are
        // JIT-inlined for a value-type TOffset (HermitCrab's int), so skip the interface-dispatch
        // field entirely when the caller passed the default comparer.
        private readonly bool _isDefault;

        public RegistersEqualityComparer(IEqualityComparer<TOffset> offsetEqualityComparer)
        {
            _offsetEqualityComparer = offsetEqualityComparer;
            _isDefault = ReferenceEquals(offsetEqualityComparer, EqualityComparer<TOffset>.Default);
        }

        public bool Equals(Register<TOffset>[] x, Register<TOffset>[] y)
        {
            for (int i = 0; i < x.Length; i++)
            {
                if (!RegisterEquals(x[i], y[i]))
                    return false;
            }
            return true;
        }

        private bool RegisterEquals(Register<TOffset> x, Register<TOffset> y)
        {
            return _isDefault
                ? x.ValueEquals(y, EqualityComparer<TOffset>.Default)
                : x.ValueEquals(y, _offsetEqualityComparer);
        }

        public int GetHashCode(Register<TOffset>[] obj)
        {
            int code = 23;
            for (int i = 0; i < obj.Length; i++)
            {
                if (obj[i].HasOffset)
                {
                    code =
                        code * 31
                        + (
                            _isDefault
                                ? EqualityComparer<TOffset>.Default.GetHashCode(obj[i].Offset)
                                : _offsetEqualityComparer.GetHashCode(obj[i].Offset)
                        );
                    code = code * 31 + obj[i].IsStart.GetHashCode();
                }
                else
                {
                    code = code * 31 + 0;
                }
            }
            return code;
        }
    }
}
