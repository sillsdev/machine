using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace SIL.Machine.Morphology.HermitCrab
{
    public enum ParseExhaustionReason
    {
        None,
        StepBudget,
        Timeout,
    }

    /// <summary>
    /// Per-<see cref="Morpher.ParseWord(string, out object, bool, out ParseDiagnostics)"/> work budget,
    /// referenced from every <see cref="Word"/> produced during that parse (propagated through
    /// <see cref="Word"/>'s copy constructor exactly like <see cref="Word.CurrentTrace"/>). Compiled rule
    /// objects are shared across concurrent parses, so this state cannot live on the rules or the
    /// <see cref="Morpher"/> itself; it lives here instead and is threaded through the data.
    /// </summary>
    internal sealed class ParseContext
    {
        // Wall-clock is checked only every Nth step: Stopwatch reads are cheap but not free, and the
        // budget's steady-state cost on the happy path must stay close to a single Interlocked increment.
        private const int DeadlineCheckMask = 0xFF;

        private readonly int _maxSteps;
        private readonly long _timeoutTicks;
        private readonly long _startTimestamp;
        private readonly ConcurrentDictionary<IHCRule, int> _ruleCounters;
        private int _steps;
        private int _exhausted;
        private ParseExhaustionReason _reason;

        public ParseContext(int maxSteps, TimeSpan timeout, int surfaceLength, bool collectRuleCounters = false)
        {
            _maxSteps = maxSteps;
            _timeoutTicks = timeout > TimeSpan.Zero ? (long)(timeout.TotalSeconds * Stopwatch.Frequency) : -1;
            _startTimestamp = Stopwatch.GetTimestamp();
            SurfaceLength = surfaceLength;
            if (collectRuleCounters)
                _ruleCounters = new ConcurrentDictionary<IHCRule, int>();
        }

        /// <summary>Length (in segments) of the surface shape being parsed; carrier for Layer 2's shape-growth cap.</summary>
        public int SurfaceLength { get; }

        public bool Exhausted => Volatile.Read(ref _exhausted) != 0;

        public ParseExhaustionReason Reason => _reason;

        public int StepsUsed => Volatile.Read(ref _steps);

        public TimeSpan Elapsed =>
            TimeSpan.FromSeconds((double)(Stopwatch.GetTimestamp() - _startTimestamp) / Stopwatch.Frequency);

        public bool DiagnosticsEnabled => _ruleCounters != null;

        public IReadOnlyDictionary<IHCRule, int> RuleCounters => _ruleCounters;

        /// <summary>
        /// Records one rule-application attempt. Returns false once the budget is gone; callers must
        /// treat that as "no result" and unwind immediately (return <c>Enumerable.Empty&lt;Word&gt;()</c>),
        /// never throw.
        /// </summary>
        public bool Step(IHCRule rule = null)
        {
            if (Exhausted)
                return false;

            if (rule != null && _ruleCounters != null)
                _ruleCounters.AddOrUpdate(rule, 1, (_, count) => count + 1);

            if (_maxSteps <= 0 && _timeoutTicks < 0)
                return true;

            int steps = Interlocked.Increment(ref _steps);
            if (_maxSteps > 0 && steps >= _maxSteps)
            {
                MarkExhausted(ParseExhaustionReason.StepBudget);
                return false;
            }
            if (_timeoutTicks >= 0 && (steps & DeadlineCheckMask) == 0)
            {
                if (Stopwatch.GetTimestamp() - _startTimestamp >= _timeoutTicks)
                {
                    MarkExhausted(ParseExhaustionReason.Timeout);
                    return false;
                }
            }
            return true;
        }

        private void MarkExhausted(ParseExhaustionReason reason)
        {
            if (Interlocked.CompareExchange(ref _exhausted, 1, 0) == 0)
                _reason = reason;
        }
    }
}
