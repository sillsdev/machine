using System;
using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
    [Flags]
    public enum TextRowFlags
    {
        None = 0x0,
        SentenceStart = 0x1,
        InRange = 0x2,
        RangeStart = 0x4
    }

    public class TextRow : IRow
    {
        public TextRow(string textId, object rowRef)
        {
            TextId = textId;
            Ref = rowRef;
        }

        public string TextId { get; }

        public object Ref { get; }

        public bool IsEmpty => Segment.Count == 0;

        public TextRowFlags Flags { get; set; } = TextRowFlags.SentenceStart;

        public bool IsSentenceStart => Flags.HasFlag(TextRowFlags.SentenceStart);
        public bool IsInRange => Flags.HasFlag(TextRowFlags.InRange);
        public bool IsRangeStart => Flags.HasFlag(TextRowFlags.RangeStart);

        public IReadOnlyList<string> Segment { get; set; } = Array.Empty<string>();
        public string Text => string.Join(" ", Segment);

        public override string ToString()
        {
            string segment;
            if (IsEmpty)
                segment = IsInRange ? "<range>" : "EMPTY";
            else if (Segment.Count > 0)
                segment = Text;
            else
                segment = "NONEMPTY";
            return $"{Ref} - {segment}";
        }
    }
}
