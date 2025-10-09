using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora;

namespace SIL.Machine.PunctuationAnalysis
{
    public class TextSegment : IEquatable<TextSegment>
    {
        public string Text
        {
            get => _surrogatePairString.ToString();
            private set => _surrogatePairString = new SurrogatePairString(value);
        }
        public string Book { get; private set; }
        public int Chapter { get; private set; }
        public UsfmMarkerType ImmediatePrecedingMarker { get; private set; }
        public HashSet<UsfmMarkerType> MarkersInPrecedingContext { get; private set; }
        public TextSegment PreviousSegment { get; set; }
        public TextSegment NextSegment { get; set; }
        public int IndexInVerse { get; set; }
        public int NumSegmentsInVerse { get; set; }
        public UsfmToken UsfmToken { get; private set; }
        private SurrogatePairString _surrogatePairString;

        public TextSegment()
        {
            Text = "";
            ImmediatePrecedingMarker = UsfmMarkerType.NoMarker;
            MarkersInPrecedingContext = new HashSet<UsfmMarkerType>();
            PreviousSegment = null;
            NextSegment = null;
            IndexInVerse = 0;
            NumSegmentsInVerse = 0;
            UsfmToken = null;
        }

        public TextSegment(string text)
        {
            Text = text;
            ImmediatePrecedingMarker = UsfmMarkerType.NoMarker;
            MarkersInPrecedingContext = new HashSet<UsfmMarkerType>();
            PreviousSegment = null;
            NextSegment = null;
            IndexInVerse = 0;
            NumSegmentsInVerse = 0;
            UsfmToken = null;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TextSegment other))
            {
                return false;
            }
            return Equals(other);
        }

        public bool Equals(TextSegment other)
        {
            return Text.Equals(other.Text)
                && IndexInVerse.Equals(other.IndexInVerse)
                && NumSegmentsInVerse.Equals(other.NumSegmentsInVerse)
                && (
                    (UsfmToken == null && other.UsfmToken == null)
                    || (UsfmToken != null && other.UsfmToken != null && UsfmToken.Equals(other.UsfmToken))
                )
                && ImmediatePrecedingMarker.Equals(other.ImmediatePrecedingMarker);
        }

        public override int GetHashCode()
        {
            int hashCode = 23;
            hashCode = hashCode * 31 + Text.GetHashCode();
            hashCode = hashCode * 31 + IndexInVerse.GetHashCode();
            hashCode = hashCode * 31 + NumSegmentsInVerse.GetHashCode();
            hashCode = hashCode * 31 + UsfmToken.GetHashCode();
            return hashCode * 31 + ImmediatePrecedingMarker.GetHashCode();
        }

        public int Length => _surrogatePairString.Length;

        public string Substring(int startIndex, int length)
        {
            return _surrogatePairString.Substring(startIndex, length);
        }

        public string SubstringBefore(int index)
        {
            return Substring(0, index);
        }

        public string SubstringAfter(int index)
        {
            return Substring(index, Length - index);
        }

        public bool MarkerIsInPrecedingContext(UsfmMarkerType marker)
        {
            return MarkersInPrecedingContext.Contains(marker);
        }

        public bool IsFirstSegmentInVerse()
        {
            return IndexInVerse == 0;
        }

        public bool IsLastSegmentInVerse()
        {
            return IndexInVerse == NumSegmentsInVerse - 1;
        }

        public void ReplaceSubstring(int startIndex, int endIndex, string replacement)
        {
            Text = SubstringBefore(startIndex) + replacement + SubstringAfter(endIndex);
            if (UsfmToken != null)
            {
                UsfmToken.Text = Text;
            }
        }

        public class Builder
        {
            private readonly TextSegment _textSegment;

            public Builder()
            {
                _textSegment = new TextSegment();
            }

            public Builder SetPreviousSegment(TextSegment previousSegment)
            {
                _textSegment.PreviousSegment = previousSegment;
                return this;
            }

            public Builder AddPrecedingMarker(UsfmMarkerType marker)
            {
                _textSegment.ImmediatePrecedingMarker = marker;
                _textSegment.MarkersInPrecedingContext.Add(marker);
                return this;
            }

            public Builder SetBook(string code)
            {
                _textSegment.Book = code;
                return this;
            }

            public Builder SetChapter(int number)
            {
                _textSegment.Chapter = number;
                return this;
            }

            public Builder SetUsfmToken(UsfmToken token)
            {
                _textSegment.UsfmToken = token;
                return this;
            }

            public Builder SetText(string text)
            {
                _textSegment.Text = text;
                return this;
            }

            public TextSegment Build()
            {
                return _textSegment;
            }
        }
    }

    /// <summary>
    /// Class to handle indexing of strings by unicode code point, treating surrogate pairs as single characters.
    /// </summary>
    public class SurrogatePairString
    {
        public string String => _stringValue;
        public int Length => _stringIndexBySurrogatePairIndex.Count;

        private readonly string _stringValue;
        private readonly Dictionary<int, int> _surrogatePairIndexByStringIndex;
        private readonly Dictionary<int, int> _stringIndexBySurrogatePairIndex;

        public SurrogatePairString(string stringValue)
        {
            _stringValue = stringValue;
            IEnumerable<(int SurrogatePairIndex, int StringIndex)> indexPairs = _stringValue
                .Select((c, i) => (c, i))
                .Where(tup => !char.IsLowSurrogate(tup.c))
                .Select((tup, i) => (tup.i, i));
            _surrogatePairIndexByStringIndex = new Dictionary<int, int>();
            _stringIndexBySurrogatePairIndex = new Dictionary<int, int>();
            foreach ((int surrogatePairIndex, int stringIndex) in indexPairs)
            {
                _surrogatePairIndexByStringIndex[stringIndex] = surrogatePairIndex;
                _stringIndexBySurrogatePairIndex[surrogatePairIndex] = stringIndex;
            }
        }

        public override string ToString()
        {
            return _stringValue;
        }

        public string this[int surrogatePairIndex]
        {
            get
            {
                if (surrogatePairIndex < 0 || surrogatePairIndex > Length)
                {
                    throw new IndexOutOfRangeException(
                        $"Index {surrogatePairIndex} is out of bounds for SurrogatePairString with length {Length}."
                    );
                }
                int stringIndex = _stringIndexBySurrogatePairIndex[surrogatePairIndex];
                char characterAtStringIndex = _stringValue[stringIndex];
                if (
                    stringIndex < _stringValue.Length
                    && char.IsSurrogatePair(characterAtStringIndex, _stringValue[stringIndex + 1])
                )
                {
                    return _stringValue.Substring(stringIndex, 2);
                }
                return characterAtStringIndex.ToString();
            }
        }

        public int GetSurrogatePairIndexForStringIndex(int stringIndex)
        {
            if (stringIndex == _stringValue.Length)
            {
                return _surrogatePairIndexByStringIndex.Count;
            }
            if (!_surrogatePairIndexByStringIndex.TryGetValue(stringIndex, out int surrogatePairIndex))
            {
                throw new ArgumentException($"No non-surrogate code point begins at index {stringIndex}");
            }
            return surrogatePairIndex;
        }

        public string Substring(int startSurrogatePairIndex, int length)
        {
            int endSurrogatePairIndex = startSurrogatePairIndex + length;
            int startStringIndex = GetStringIndexForSurrogatePairIndex(startSurrogatePairIndex);
            int endStringIndex = GetStringIndexForSurrogatePairIndex(endSurrogatePairIndex);
            return _stringValue.Substring(startStringIndex, endStringIndex - startStringIndex);
        }

        public int GetStringIndexForSurrogatePairIndex(int surrogatePairIndex)
        {
            if (surrogatePairIndex == _surrogatePairIndexByStringIndex.Count)
            {
                return _stringValue.Length;
            }
            return _surrogatePairIndexByStringIndex[surrogatePairIndex];
        }
    }
}
