using System.Collections.Generic;
using SIL.Machine.Corpora;

namespace SIL.Machine.PunctuationAnalysis
{
    public class TextSegment
    {
        public string Text { get; private set; }
        public UsfmMarkerType ImmediatePrecedingMarker { get; private set; }
        public HashSet<UsfmMarkerType> MarkersInPrecedingContext { get; private set; }
        public TextSegment PreviousSegment { get; set; }
        public TextSegment NextSegment { get; set; }
        public int IndexInVerse { get; set; }
        public int NumSegmentsInVerse { get; set; }
        public UsfmToken UsfmToken { get; private set; }

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
            if (!(obj is TextSegment t))
            {
                return false;
            }
            return Text.Equals(t.Text)
                && IndexInVerse.Equals(t.IndexInVerse)
                && NumSegmentsInVerse.Equals(t.NumSegmentsInVerse)
                && (
                    (UsfmToken == null && t.UsfmToken == null)
                    || (UsfmToken != null && t.UsfmToken != null && UsfmToken.Equals(t.UsfmToken))
                )
                && ImmediatePrecedingMarker.Equals(t.ImmediatePrecedingMarker);
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

        public int Length => Text.Length;

        public string SubstringBefore(int index)
        {
            return Text.Substring(0, index);
        }

        public string SubstringAfter(int index)
        {
            return Text.Substring(index);
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
}
