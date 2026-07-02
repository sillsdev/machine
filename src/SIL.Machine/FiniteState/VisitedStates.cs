using System;

namespace SIL.Machine.FiniteState
{
    /// <summary>
    /// A value-type set of FST state indices used by the nondeterministic traversal to avoid epsilon
    /// loops. States have a dense <see cref="State{TData,TOffset}.Index"/> (0..N-1), so membership is a
    /// bitset: states 0–63 live in an inline <c>ulong</c> field (zero heap allocation — the common case,
    /// HC rule FSTs have only a handful of states) and an overflow <c>ulong[]</c> is allocated lazily only
    /// for FSTs with 64+ states. RUSTIFY lever 1: replaces the per-instance <c>HashSet&lt;State&gt;</c>
    /// (~1.17M allocated per word on Sena) so creating a traversal instance no longer allocates a set.
    /// </summary>
    internal struct VisitedStates
    {
        private ulong _bits0; // states 0..63
        private ulong[] _overflow; // states 64.., word i covers states [64*(i+1) .. 64*(i+1)+63]

        public bool Contains(int index)
        {
            if (index < 64)
                return (_bits0 & (1UL << index)) != 0;
            int w = index / 64 - 1;
            return _overflow != null && w < _overflow.Length && (_overflow[w] & (1UL << (index & 63))) != 0;
        }

        public void Add(int index)
        {
            if (index < 64)
            {
                _bits0 |= 1UL << index;
                return;
            }
            int w = index / 64 - 1;
            if (_overflow == null || w >= _overflow.Length)
                Array.Resize(ref _overflow, w + 1);
            _overflow[w] |= 1UL << (index & 63);
        }

        public void Clear()
        {
            _bits0 = 0;
            if (_overflow != null)
                Array.Clear(_overflow, 0, _overflow.Length);
        }

        public void UnionWith(in VisitedStates other)
        {
            _bits0 |= other._bits0;
            if (other._overflow != null)
            {
                if (_overflow == null || _overflow.Length < other._overflow.Length)
                    Array.Resize(ref _overflow, other._overflow.Length);
                for (int i = 0; i < other._overflow.Length; i++)
                    _overflow[i] |= other._overflow[i];
            }
        }
    }
}
