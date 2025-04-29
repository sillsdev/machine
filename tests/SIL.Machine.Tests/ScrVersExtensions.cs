using SIL.Scripture;

namespace SIL.Machine.Tests;

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
}
