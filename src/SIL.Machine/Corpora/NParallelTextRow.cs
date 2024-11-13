using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SIL.Extensions;

namespace SIL.Machine.Corpora
{
    public class NParallelTextRow : IRow
    {
        public NParallelTextRow(string textId, IEnumerable<IReadOnlyList<object>> nRefs)
        {
            if (string.IsNullOrEmpty(textId))
                throw new ArgumentNullException(nameof(textId));

            if (nRefs == null || nRefs.Where(r => r != null).SelectMany(r => r).Count() == 0)
                throw new ArgumentNullException($"Refs must be provided but nRefs={nRefs}");

            TextId = textId;
            NRefs = nRefs.ToList().ToReadOnlyList();
            N = NRefs.Count;
            NSegments = Enumerable.Range(0, N).Select(_ => Array.Empty<string>()).ToImmutableArray();
            NFlags = Enumerable.Range(0, N).Select(_ => TextRowFlags.SentenceStart).ToImmutableArray();
        }

        public string TextId { get; }

        public object Ref => NRefs.SelectMany(r => r).First();

        public IReadOnlyList<IReadOnlyList<object>> NRefs { get; }
        public int N { get; }

        public IReadOnlyList<IReadOnlyList<string>> NSegments { get; set; }
        public IReadOnlyList<TextRowFlags> NFlags { get; set; }

        public bool IsSentenceStart(int i) =>
            NFlags.Count > i ? NFlags[i].HasFlag(TextRowFlags.SentenceStart) : throw new ArgumentOutOfRangeException();

        public bool IsInRange(int i) =>
            NFlags.Count > i ? NFlags[i].HasFlag(TextRowFlags.InRange) : throw new ArgumentOutOfRangeException();

        public bool IsRangeStart(int i) =>
            NFlags.Count > i ? NFlags[i].HasFlag(TextRowFlags.RangeStart) : throw new ArgumentOutOfRangeException();

        public bool IsEmpty => NSegments.All(s => s.Count == 0);

        public string Text(int i) => string.Join(" ", NSegments[i]);

        public IReadOnlyCollection<AlignedWordPair> AlignedWordPairs { get; set; }

        public NParallelTextRow Invert()
        {
            return new NParallelTextRow(TextId, NRefs.Reverse()) { NFlags = NFlags.Reverse().ToImmutableArray(), };
        }
    }
}
