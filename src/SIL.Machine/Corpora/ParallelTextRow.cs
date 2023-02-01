using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public class ParallelTextRow : IRow
    {
        public ParallelTextRow(string textId, IReadOnlyList<object> sourceRefs, IReadOnlyList<object> targetRefs)
        {
            if (sourceRefs.Count == 0 && targetRefs.Count == 0)
                throw new ArgumentNullException("Either a source or target ref must be provided.");

            TextId = textId;
            SourceRefs = sourceRefs;
            TargetRefs = targetRefs;
        }

        public string TextId { get; }

        public object Ref => SourceRefs.Count > 0 ? SourceRefs[0] : TargetRefs[0];

        public IReadOnlyList<object> Refs => SourceRefs.Count > 0 ? SourceRefs : TargetRefs;

        public IReadOnlyList<object> SourceRefs { get; }
        public IReadOnlyList<object> TargetRefs { get; }

        public IReadOnlyList<string> SourceSegment { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> TargetSegment { get; set; } = Array.Empty<string>();

        public IReadOnlyCollection<AlignedWordPair> AlignedWordPairs { get; set; }

        public TextRowFlags SourceFlags { get; set; } = TextRowFlags.SentenceStart;
        public TextRowFlags TargetFlags { get; set; } = TextRowFlags.SentenceStart;

        public bool IsSourceSentenceStart => SourceFlags.HasFlag(TextRowFlags.SentenceStart);
        public bool IsSourceInRange => SourceFlags.HasFlag(TextRowFlags.InRange);
        public bool IsSourceRangeStart => SourceFlags.HasFlag(TextRowFlags.RangeStart);
        public bool IsTargetSentenceStart => TargetFlags.HasFlag(TextRowFlags.SentenceStart);
        public bool IsTargetInRange => TargetFlags.HasFlag(TextRowFlags.InRange);
        public bool IsTargetRangeStart => TargetFlags.HasFlag(TextRowFlags.RangeStart);

        public bool IsEmpty => SourceSegment.Count == 0 || TargetSegment.Count == 0;

        public string SourceText => string.Join(" ", SourceSegment);
        public string TargetText => string.Join(" ", TargetSegment);

        public ParallelTextRow Invert()
        {
            return new ParallelTextRow(TextId, TargetRefs, SourceRefs)
            {
                SourceSegment = TargetSegment,
                TargetSegment = SourceSegment,
                AlignedWordPairs =
                    AlignedWordPairs == null
                        ? null
                        : new HashSet<AlignedWordPair>(AlignedWordPairs.Select(wp => wp.Invert())),
                SourceFlags = TargetFlags,
                TargetFlags = SourceFlags
            };
        }
    }
}
