using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.PunctuationAnalysis
{
    public class Verse
    {
        public List<TextSegment> TextSegments { get; private set; }

        public Verse(List<TextSegment> textSegments)
        {
            TextSegments = textSegments;
            IndexTextSegments();
        }

        private void IndexTextSegments()
        {
            foreach ((int index, TextSegment textSegment) in TextSegments.Select((t, i) => (i, t)))
            {
                textSegment.IndexInVerse = index;
                textSegment.NumSegmentsInVerse = TextSegments.Count;
            }
        }
    }
}
