using System.Collections.Generic;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public static class VerseRefExtensions
    {
        public static VerseRef RemoveSegments(this VerseRef verseRef)
        {
            if (string.IsNullOrEmpty(verseRef.Segment()))
            {
                return verseRef;
            }
            try
            {
                return new VerseRef(
                    $"{verseRef.Book} {verseRef.ChapterNum}:{string.Join(",", verseRef.AllVerses().Select(vr => vr.VerseNum).ToArray())}",
                    verseRef.Versification
                );
            }
            catch (VerseRefException)
            {
                VerseRef newVerseRef = verseRef;
                newVerseRef.Simplify();
                return newVerseRef;
            }
        }

        public static VerseRef ChangeVersificationWithSegments(this VerseRef verseRef, ScrVers versification)
        {
            VerseRef vr = verseRef;
            vr.ChangeVersification(versification);
            if (string.IsNullOrEmpty(vr.Segment()))
                return vr;
            VerseRef verseRefWithoutSegments = verseRef.RemoveSegments();
            verseRefWithoutSegments.ChangeVersification(versification);
            if (!verseRefWithoutSegments.Equals(vr.RemoveSegments()))
            {
                IEnumerable<string> verses = verseRef
                    .AllVerses()
                    .Zip(
                        verseRefWithoutSegments.AllVerses(),
                        (verseWithSegments, verseWithCorrectNumber) => (verseWithSegments, verseWithCorrectNumber)
                    )
                    .Select(
                        (verseTuple) => verseTuple.verseWithCorrectNumber.Verse + verseTuple.verseWithSegments.Segment()
                    );
                return new VerseRef(
                    $"{verseRefWithoutSegments.Book} {verseRefWithoutSegments.ChapterNum}:{string.Join(",", verses)}",
                    versification
                );
            }
            return vr;
        }
    }
}
