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
                VerseRef newVerseRef = verseRef.Clone();
                newVerseRef.Simplify();
                return verseRef;
            }
        }
    }
}
