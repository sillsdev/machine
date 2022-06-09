using System;

namespace SIL.Machine.Corpora
{
    public class TextFileRef : IEquatable<TextFileRef>, IComparable<TextFileRef>, IComparable
    {
        public TextFileRef(string textId, int lineNum)
        {
            TextId = textId;
            LineNum = lineNum;
        }

        public string TextId { get; }

        public int LineNum { get; }

        public int CompareTo(TextFileRef other)
        {
            int res = TextId.CompareTo(other.TextId);
            if (res != 0)
                return res;

            return LineNum.CompareTo(other.LineNum);
        }

        public int CompareTo(object obj)
        {
            if (obj is TextFileRef textFileRef)
                return CompareTo(textFileRef);
            throw new ArgumentException($"The specified object is not a {nameof(TextFileRef)}.", nameof(obj));
        }

        public bool Equals(TextFileRef other)
        {
            return TextId == other.TextId && LineNum == other.LineNum;
        }

        public override bool Equals(object obj)
        {
            if (obj is TextFileRef textFileRef)
                return Equals(textFileRef);
            return false;
        }

        public override int GetHashCode()
        {
            int code = 23;
            code = code * 31 + TextId.GetHashCode();
            code = code * 31 + LineNum.GetHashCode();
            return code;
        }

        public override string ToString()
        {
            return $"{TextId}:{LineNum}";
        }
    }
}
