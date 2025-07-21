using System.Collections.Generic;

namespace SIL.Machine.Corpora.PunctuationAnalysis
{
    public class Chapter
    {
        public Chapter(List<Verse> verses)
        {
            Verses = verses;
        }

        public List<Verse> Verses { get; set; }
    }
}
