using System;
using System.Collections.Concurrent;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// A thread-safe pool of <see cref="Morpher"/> instances for the FST verify / engine-backstop
    /// paths. Verification pins the engine's <see cref="Morpher.LexEntrySelector"/>/
    /// <see cref="Morpher.RuleSelector"/> per candidate, which is mutable instance state — so a single
    /// shared <see cref="Morpher"/> cannot be used from multiple threads (the selectors would race).
    /// Each parse instead <see cref="Rent"/>s its own Morpher and <see cref="Return"/>s it (selectors
    /// reset) when done; the Morpher's own internal parallelism is safe because the rented instance has
    /// a single owner for the duration of the call. Morphers are built once (compiling the grammar is
    /// expensive) and reused across words.
    /// </summary>
    public sealed class MorpherPool
    {
        private readonly Func<Morpher> _factory;
        private readonly ConcurrentBag<Morpher> _available = new ConcurrentBag<Morpher>();

        /// <param name="factory">Creates a fresh <see cref="Morpher"/> (each must be independent — its
        /// own <see cref="TraceManager"/> — so pooled instances never share mutable state).</param>
        public MorpherPool(Func<Morpher> factory)
        {
            _factory = factory;
        }

        /// <summary>Borrow a Morpher with default (unrestricted) selectors. Always pair with <see cref="Return"/>.</summary>
        public Morpher Rent()
        {
            return _available.TryTake(out Morpher morpher) ? morpher : _factory();
        }

        /// <summary>Reset the selectors and return the Morpher to the pool for reuse.</summary>
        public void Return(Morpher morpher)
        {
            morpher.LexEntrySelector = _ => true;
            morpher.RuleSelector = _ => true;
            _available.Add(morpher);
        }
    }
}
