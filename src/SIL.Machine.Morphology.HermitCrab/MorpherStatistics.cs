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

        /// <summary>
        /// Optional current-thread allocated-bytes probe (e.g. GC.GetAllocatedBytesForCurrentThread,
        /// which is not in netstandard2.0). When set AND <see cref="Enabled"/>, Word.Clone attributes
        /// its allocation to <see cref="CloneBytes"/>. Single-threaded use only (per-thread counter).
        /// </summary>
        public static Func<long> AllocationProbe;

        private static long _cloneBytes;
        private static long _wordClones;
        private static long _analysisTimestamp;
        private static long _synthesisTimestamp;
        private static long _analysesProduced;
        private static long _wordsParsed;
        private static long _parallelSections;
        private static long _wordCtorBytes;
        private static long _wordCtorCount;
        private static long _markMorphBytes;
        private static long _segmentBytes;
        private static long _analysisCascadeBytes;

        /// <summary>Number of <see cref="Word.Clone"/> calls (deep shape copies).</summary>
        public static long WordClones => Interlocked.Read(ref _wordClones);

        /// <summary>Bytes allocated inside Word.Clone (only meaningful when AllocationProbe is set).</summary>
        public static long CloneBytes => Interlocked.Read(ref _cloneBytes);

        internal static void AddCloneBytes(long bytes) => Interlocked.Add(ref _cloneBytes, bytes);

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

        /// <summary>Number of 'new Word(stratum, shape)' calls (includes cascade-created Words).</summary>
        public static long WordCtorCount => Interlocked.Read(ref _wordCtorCount);

        /// <summary>Bytes allocated in 'new Word(stratum, shape)' initial construction (not clone).</summary>
        public static long WordCtorBytes => Interlocked.Read(ref _wordCtorBytes);

        /// <summary>Bytes allocated in Word.MarkMorph (annotation + morph bookkeeping).</summary>
        public static long MarkMorphBytes => Interlocked.Read(ref _markMorphBytes);

        internal static void CountWordClone()
        {
            if (Enabled)
                Interlocked.Increment(ref _wordClones);
        }

        /// <summary>Bytes allocated in CharacterDefinitionTable.Segment (initial Shape/ShapeNode creation).</summary>
        public static long SegmentBytes => Interlocked.Read(ref _segmentBytes);

        /// <summary>Bytes allocated during the analysis cascade (Apply + ToList, EXCLUDING Word.Clone which is tracked separately).</summary>
        public static long AnalysisCascadeBytes => Interlocked.Read(ref _analysisCascadeBytes);

        internal static void AddWordCtorBytes(long bytes)
        {
            Interlocked.Increment(ref _wordCtorCount);
            Interlocked.Add(ref _wordCtorBytes, bytes);
        }

        internal static void AddMarkMorphBytes(long bytes) => Interlocked.Add(ref _markMorphBytes, bytes);

        internal static void AddSegmentBytes(long bytes) => Interlocked.Add(ref _segmentBytes, bytes);

        internal static void AddAnalysisCascadeBytes(long bytes) => Interlocked.Add(ref _analysisCascadeBytes, bytes);

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
            Interlocked.Exchange(ref _cloneBytes, 0);
            Interlocked.Exchange(ref _analysisTimestamp, 0);
            Interlocked.Exchange(ref _synthesisTimestamp, 0);
            Interlocked.Exchange(ref _analysesProduced, 0);
            Interlocked.Exchange(ref _wordsParsed, 0);
            Interlocked.Exchange(ref _parallelSections, 0);
            Interlocked.Exchange(ref _wordCtorBytes, 0);
            Interlocked.Exchange(ref _wordCtorCount, 0);
            Interlocked.Exchange(ref _markMorphBytes, 0);
            Interlocked.Exchange(ref _segmentBytes, 0);
            Interlocked.Exchange(ref _analysisCascadeBytes, 0);
        }

        private static TimeSpan ToTimeSpan(long stopwatchTicks)
        {
            return TimeSpan.FromSeconds((double)stopwatchTicks / Stopwatch.Frequency);
        }
    }
}
