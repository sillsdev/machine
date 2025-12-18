using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.PunctuationAnalysis
{
    public class Chapter
    {
        public Chapter(IEnumerable<Verse> verses, int chapterNumber = 0)
        {
            Verses = verses.ToList();
            ChapterNumber = chapterNumber;
        }

        public List<Verse> Verses { get; private set; }
        public int ChapterNumber { get; private set; }
    }
}
