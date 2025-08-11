using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.PunctuationAnalysis
{
    public class Chapter
    {
        public Chapter(IEnumerable<Verse> verses)
        {
            Verses = verses.ToList();
        }

        public List<Verse> Verses { get; set; }
    }
}
