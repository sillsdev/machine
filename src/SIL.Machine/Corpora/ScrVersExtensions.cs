using System.Collections.Generic;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public static class ScrVersExtensions
    {
        /// <summary>
        /// Gets a list of references (verse references) for the specified book.
        /// </summary>
        public static IEnumerable<VerseRef> GetReferencesForBook(this ScrVers scrVers, int bookNum)
        {
            List<VerseRef> references = new List<VerseRef>();
            int lastChapter = scrVers.GetLastChapter(bookNum);

            for (int chapterNum = 1; chapterNum <= lastChapter; chapterNum++)
            {
                int lastVerse = scrVers.GetLastVerse(bookNum, chapterNum);

                for (int verseNum = 1; verseNum <= lastVerse; verseNum++)
                {
                    int bbbcccvvv = VerseRef.GetBBBCCCVVV(bookNum, chapterNum, verseNum);
                    if (!scrVers.IsExcluded(bbbcccvvv))
                    {
                        references.Add(new VerseRef(bookNum, chapterNum, verseNum, scrVers));
                    }
                }
            }

            return references;
        }

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
    }
}
