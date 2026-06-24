using System;
using System.Diagnostics;
using System.Threading;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Opt-in, process-wide diagnostic counters for profiling the parser's hot paths
    /// (allocation pressure and per-phase timing). These exist to find small, targeted
    /// speedups — they are NOT a production feature.
    ///
    /// When <see cref="Enabled"/> is false (the default) the instrumentation is a single
    /// branch and adds no measurable overhead. Counters use <see cref="Interlocked"/> so
    /// they are safe to collect while parsing concurrently. Timings are stored as raw
    /// <see cref="Stopwatch"/> timestamp deltas and converted on read.
    /// </summary>
    public static class MorpherStatistics
    {
        /// <summary>Turn collection on/off. Off by default (zero overhead).</summary>
        public static bool Enabled;

        private static long _wordClones;
        private static long _analysisTimestamp;
        private static long _synthesisTimestamp;
        private static long _analysesProduced;
        private static long _wordsParsed;
        private static long _parallelSections;

        /// <summary>Number of <see cref="Word.Clone"/> calls (deep shape copies).</summary>
        public static long WordClones => Interlocked.Read(ref _wordClones);

        /// <summary>Number of candidate analyses produced by the unapplication search.</summary>
        public static long AnalysesProduced => Interlocked.Read(ref _analysesProduced);

        /// <summary>Number of words parsed (calls to ParseWord that reached the main path).</summary>
        public static long WordsParsed => Interlocked.Read(ref _wordsParsed);

        /// <summary>
        /// Number of times a within-word parallel code path was taken. Used to verify the
        /// single-threaded option: this must stay 0 when MaxDegreeOfParallelism == 1.
        /// </summary>
        public static long ParallelSectionsEntered => Interlocked.Read(ref _parallelSections);

        /// <summary>Total wall time spent in the analysis (unapplication) phase.</summary>
        public static TimeSpan AnalysisTime => ToTimeSpan(Interlocked.Read(ref _analysisTimestamp));

        /// <summary>Total wall time spent in the synthesis (re-application) phase.</summary>
        public static TimeSpan SynthesisTime => ToTimeSpan(Interlocked.Read(ref _synthesisTimestamp));

        internal static void CountWordClone()
        {
            if (Enabled)
                Interlocked.Increment(ref _wordClones);
        }

        internal static void EnterParallelSection()
        {
            if (Enabled)
                Interlocked.Increment(ref _parallelSections);
        }

        internal static void AddAnalysis(long stopwatchTicks, int analysesProduced)
        {
            if (!Enabled)
                return;
            Interlocked.Add(ref _analysisTimestamp, stopwatchTicks);
            Interlocked.Add(ref _analysesProduced, analysesProduced);
            Interlocked.Increment(ref _wordsParsed);
        }

        internal static void AddSynthesis(long stopwatchTicks)
        {
            if (Enabled)
                Interlocked.Add(ref _synthesisTimestamp, stopwatchTicks);
        }

        /// <summary>Reset all counters to zero.</summary>
        public static void Reset()
        {
            Interlocked.Exchange(ref _wordClones, 0);
            Interlocked.Exchange(ref _analysisTimestamp, 0);
            Interlocked.Exchange(ref _synthesisTimestamp, 0);
            Interlocked.Exchange(ref _analysesProduced, 0);
            Interlocked.Exchange(ref _wordsParsed, 0);
            Interlocked.Exchange(ref _parallelSections, 0);
        }

        private static TimeSpan ToTimeSpan(long stopwatchTicks)
        {
            return TimeSpan.FromSeconds((double)stopwatchTicks / Stopwatch.Frequency);
        }
    }
}
