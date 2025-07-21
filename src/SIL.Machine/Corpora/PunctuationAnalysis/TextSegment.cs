using System.Collections.Generic;

namespace SIL.Machine.Corpora.PunctuationAnalysis
{
    public class TextSegment
    {
        private string _text;
        private UsfmMarkerType _immediatePrecedingMarker;
        private readonly HashSet<UsfmMarkerType> _markersInPrecedingContext;
        private TextSegment _previousSegment;
        private TextSegment _nextSegment;
        private int _indexInVerse;
        private int _numSegmentsInVerse;
        private UsfmToken _usfmToken;

        public TextSegment()
        {
            _text = "";
            _immediatePrecedingMarker = UsfmMarkerType.NoMarker;
            _markersInPrecedingContext = new HashSet<UsfmMarkerType>();
            _previousSegment = null;
            _nextSegment = null;
            _indexInVerse = 0;
            _numSegmentsInVerse = 0;
            _usfmToken = null;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TextSegment t))
            {
                return false;
            }
            return _text.Equals(t._text)
                && _indexInVerse.Equals(t._indexInVerse)
                && _numSegmentsInVerse.Equals(t._numSegmentsInVerse)
                && (
                    (_usfmToken == null && t._usfmToken == null)
                    || (_usfmToken != null && t._usfmToken != null && _usfmToken.Equals(t._usfmToken))
                )
                && _immediatePrecedingMarker.Equals(t._immediatePrecedingMarker);
        }

        public override int GetHashCode()
        {
            int hashCode = 23;
            hashCode = hashCode * 31 + _text.GetHashCode();
            hashCode = hashCode * 31 + _indexInVerse.GetHashCode();
            hashCode = hashCode * 31 + _numSegmentsInVerse.GetHashCode();
            hashCode = hashCode * 31 + _usfmToken.GetHashCode();
            return hashCode * 31 + _immediatePrecedingMarker.GetHashCode();
        }

        public string Text => _text;

        public TextSegment PreviousSegment => _previousSegment;

        public TextSegment NextSegment => _nextSegment;

        public int IndexInVerse => _indexInVerse;

        public int Length => _text.Length;

        public string SubstringBefore(int index)
        {
            return _text.Substring(0, index);
        }

        public string SubstringAfter(int index)
        {
            return _text.Substring(index);
        }

        public bool MarkerIsInPrecedingContext(UsfmMarkerType marker)
        {
            return _markersInPrecedingContext.Contains(marker);
        }

        public bool IsFirstSegmentInVerse()
        {
            return _indexInVerse == 0;
        }

        public bool IsLastSegmentInVerse()
        {
            return _indexInVerse == _numSegmentsInVerse - 1;
        }

        public void ReplaceSubstring(int startIndex, int endIndex, string replacement)
        {
            _text = SubstringBefore(startIndex) + replacement + SubstringAfter(endIndex);
            if (_usfmToken != null)
            {
                _usfmToken.Text = _text;
            }
        }

        public void SetPreviousSegment(TextSegment previousSegment)
        {
            _previousSegment = previousSegment;
        }

        public void SetNextSegment(TextSegment nextSegment)
        {
            _nextSegment = nextSegment;
        }

        public void SetIndexInVerse(int indexInVerse)
        {
            _indexInVerse = indexInVerse;
        }

        public void SetNumSegmentsInVerse(int numSegmentsInVerse)
        {
            _numSegmentsInVerse = numSegmentsInVerse;
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
                _textSegment._previousSegment = previousSegment;
                return this;
            }

            public Builder AddPrecedingMarker(UsfmMarkerType marker)
            {
                _textSegment._immediatePrecedingMarker = marker;
                _textSegment._markersInPrecedingContext.Add(marker);
                return this;
            }

            public Builder SetUsfmToken(UsfmToken token)
            {
                _textSegment._usfmToken = token;
                return this;
            }

            public Builder SetText(string text)
            {
                _textSegment._text = text;
                return this;
            }

            public TextSegment Build()
            {
                return _textSegment;
            }
        }
    }
}
