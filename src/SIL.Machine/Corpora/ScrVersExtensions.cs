using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public static class ScrVersExtensions
    {
        public static IEnumerable<VerseRef> AllIncludedVerses(
            this ScrVers scrVers,
            Dictionary<int, HashSet<int>> onlyChapters = null
        )
        {
            for (int book = 1; book <= scrVers.GetLastBook(); book++)
            {
                if (!Canon.IsCanonical(book) || (book > 86 && book < 93))
                    continue;

                for (int chapter = 1; chapter <= scrVers.GetLastChapter(book); chapter++)
                {
                    VerseRef? firstVerse = scrVers.FirstIncludedVerse(book, chapter);
                    bool yieldedFirstVerse = false;
                    if (
                        onlyChapters != null
                        && (
                            !onlyChapters.TryGetValue(book, out HashSet<int> chapters)
                            || (chapters != null && !chapters.Contains(chapter))
                        )
                    )
                    {
                        continue;
                    }
                    for (int verseNumber = 2; verseNumber <= scrVers.GetLastVerse(book, chapter); verseNumber++)
                    {
                        VerseRef verse = new VerseRef(book, chapter, verseNumber, scrVers);
                        if (scrVers.IsExcluded(verse.BBBCCCVVV))
                            continue;
                        if (!yieldedFirstVerse && firstVerse != null)
                        {
                            yield return (VerseRef)firstVerse;
                            yieldedFirstVerse = true;
                        }
                        yield return verse;
                    }
                }
            }
        }

        public static bool HasCrossBookMappings(this ScrVers scrVers, ScrVers referenceVersification = null)
        {
            if (referenceVersification == null)
                referenceVersification = ScrVers.Original;
            foreach (VerseRef verseRef in scrVers.AllIncludedVerses())
            {
                VerseRef standardRef = verseRef;
                standardRef.ChangeVersification(referenceVersification);
                if (verseRef.BookNum != standardRef.BookNum)
                    return true;
            }
            return false;
        }

        public static bool IsEquivalentTo(this ScrVers scrVers, ScrVers other)
        {
            if (scrVers.Equals(other))
                return true;

            // If all verses in the versifications are 1) equal (accounts for mapping)
            // and 2) graphically identical in regard to book, chapter, and verse, then the versifications are equivalent
            foreach (
                (VerseRef thisVerse, VerseRef otherVerse) in scrVers
                    .AllIncludedVerses()
                    .Zip(other.AllIncludedVerses())
                    .Select(tup => (tup.Item1, tup.Item2))
            )
            {
                if (
                    !(
                        thisVerse
                            .ChangeVersificationWithSegments(ScrVers.Original)
                            .Equals(otherVerse.ChangeVersificationWithSegments(ScrVers.Original))
                        && thisVerse.BBBCCCVVVS == otherVerse.BBBCCCVVVS
                    )
                )
                {
                    return false;
                }
            }
            return true;
        }
    }
}
