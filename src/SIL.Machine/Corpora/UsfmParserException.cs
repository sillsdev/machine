using System;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class UsfmParserException : Exception
    {
        public UsfmParserException()
            : base() { }

        public UsfmParserException(string message)
            : base(message) { }

        public UsfmParserException(string message, Exception innerException)
            : base(message, innerException) { }

        public UsfmParserException(string message, Exception innerException, VerseRef verseRef, int verseOffset)
            : base(message, innerException)
        {
            VerseRef = verseRef;
            VerseOffset = verseOffset;
        }

        public VerseRef VerseRef { get; }
        public int VerseOffset { get; }
    }
}
