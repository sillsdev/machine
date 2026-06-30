using System;
using System.Threading;

namespace SIL.Machine.FiniteState
{
    /// <summary>
    /// Opt-in per-category allocation probe for FST traversal hot paths.
    /// Mirrors the pattern in MorpherStatistics (HermitCrab layer) but lives in SIL.Machine
    /// so it can instrument VariableBindings, Register clones, and traversal scaffolding
    /// directly — those callers can't reference the HC layer.
    ///
    /// Use alongside MorpherStatistics.AllocationProbe to decompose the "80% FST scaffolding":
    ///   VarBindingsBytes   : VariableBindings.Clone() calls (α-variable tracking)
    ///   RegisterCloneBytes : registers.Clone() in CheckAccepting (per accepted candidate)
    ///   ScaffoldBytes      : per-Transduce HashSet/Register[]/List allocations
    ///   TraversalMethodBytes: new traversal method + instance free-list (when pool is OFF)
    /// </summary>
    public static class FstStatistics
    {
        /// <summary>Turn collection on/off. Off by default (zero overhead).</summary>
        public static bool Enabled;

        /// <summary>
        /// Optional current-thread allocated-bytes probe (e.g. GC.GetAllocatedBytesForCurrentThread).
        /// Must be set for the byte counters to accumulate. Single-threaded use only.
        /// </summary>
        public static Func<long> AllocationProbe;

        private static long VarBindingsBytesSum;
        private static long RegisterCloneBytesSum;
        private static long ScaffoldBytesSum;
        private static long TraversalMethodBytesSum;
        private static long VarBindingsClonesCount;
        private static long RegisterClonesCount;
        private static long TransduceScaffoldsCount;
        private static long TraversalMethodCreatesCount;

        /// <summary>Bytes allocated inside VariableBindings.Clone().</summary>
        public static long VarBindingsBytes => Interlocked.Read(ref VarBindingsBytesSum);

        /// <summary>Bytes allocated inside registers.Clone() in CheckAccepting.</summary>
        public static long RegisterCloneBytes => Interlocked.Read(ref RegisterCloneBytesSum);

        /// <summary>Bytes allocated for per-Transduce HashSet/Register[]/List scaffolding.</summary>
        public static long ScaffoldBytes => Interlocked.Read(ref ScaffoldBytesSum);

        /// <summary>Bytes allocated when creating a new traversal method (pool OFF).</summary>
        public static long TraversalMethodBytes => Interlocked.Read(ref TraversalMethodBytesSum);

        public static long VarBindingsClones => Interlocked.Read(ref VarBindingsClonesCount);
        public static long RegisterClones => Interlocked.Read(ref RegisterClonesCount);
        public static long TransduceScaffolds => Interlocked.Read(ref TransduceScaffoldsCount);
        public static long TraversalMethodCreates => Interlocked.Read(ref TraversalMethodCreatesCount);

        internal static void AddVarBindingsBytes(long bytes)
        {
            Interlocked.Increment(ref VarBindingsClonesCount);
            Interlocked.Add(ref VarBindingsBytesSum, bytes);
        }

        internal static void AddRegisterCloneBytes(long bytes)
        {
            Interlocked.Increment(ref RegisterClonesCount);
            Interlocked.Add(ref RegisterCloneBytesSum, bytes);
        }

        internal static void AddScaffoldBytes(long bytes)
        {
            Interlocked.Increment(ref TransduceScaffoldsCount);
            Interlocked.Add(ref ScaffoldBytesSum, bytes);
        }

        internal static void AddTraversalMethodBytes(long bytes)
        {
            Interlocked.Increment(ref TraversalMethodCreatesCount);
            Interlocked.Add(ref TraversalMethodBytesSum, bytes);
        }

        /// <summary>Reset all counters to zero.</summary>
        public static void Reset()
        {
            Interlocked.Exchange(ref VarBindingsBytesSum, 0);
            Interlocked.Exchange(ref RegisterCloneBytesSum, 0);
            Interlocked.Exchange(ref ScaffoldBytesSum, 0);
            Interlocked.Exchange(ref TraversalMethodBytesSum, 0);
            Interlocked.Exchange(ref VarBindingsClonesCount, 0);
            Interlocked.Exchange(ref RegisterClonesCount, 0);
            Interlocked.Exchange(ref TransduceScaffoldsCount, 0);
            Interlocked.Exchange(ref TraversalMethodCreatesCount, 0);
        }
    }
}
