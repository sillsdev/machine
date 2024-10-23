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

            if (nRefs.SelectMany(r => r).Count() == 0)
                throw new ArgumentNullException("Either a source or target ref must be provided.");

            TextId = textId;
            NRefs = nRefs.ToList().ToReadOnlyList();
            N = NRefs.Count;
            Segments = Enumerable.Range(0, N).Select(_ => Array.Empty<string>()).ToImmutableArray();
            Flags = Enumerable.Range(0, N).Select(_ => TextRowFlags.SentenceStart).ToImmutableArray();
        }

        public string TextId { get; }

        public object Ref => NRefs.SelectMany(r => r).First();

        public IReadOnlyList<IReadOnlyList<object>> NRefs { get; }
        public int N { get; }

        public IReadOnlyList<IReadOnlyList<string>> Segments { get; set; }
        public IReadOnlyList<TextRowFlags> Flags { get; set; }

        public bool GetIsSentenceStart(int i) =>
            Flags.Count > i ? Flags[i].HasFlag(TextRowFlags.SentenceStart) : throw new ArgumentOutOfRangeException();

        public bool GetIsInRange(int i) =>
            Flags.Count > i ? Flags[i].HasFlag(TextRowFlags.InRange) : throw new ArgumentOutOfRangeException();

        public bool GetIsRangeStart(int i) =>
            Flags.Count > i ? Flags[i].HasFlag(TextRowFlags.RangeStart) : throw new ArgumentOutOfRangeException();

        public bool IsEmpty => Segments.Any(s => s.Count == 0);

        public string GetText(int i) => string.Join(" ", Segments[i]);

        public NParallelTextRow Invert()
        {
            return new NParallelTextRow(TextId, NRefs.Reverse()) { Flags = Flags.Reverse().ToImmutableArray(), };
        }
    }
}
