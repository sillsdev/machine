using System;

namespace SIL.Machine.Corpora
{
    public class CorpusAlignmentException : Exception
    {
        public CorpusAlignmentException(string sourceRef, string targetRef, Exception innerException = null)
            : base(
                $"Invalid format in {sourceRef} and {targetRef}. Mismatched key formats \"{sourceRef}\" and \"{targetRef}\". There may be an extraneous tab, missing ref, or inconsistent use of user-defined refs.",
                innerException
            ) { }

        public CorpusAlignmentException(string[] refs, Exception innerException = null)
            : base(
                $"Invalid format in {string.Join(", ", refs)}. Mismatched key formats. There may be an extraneous tab, missing ref, or inconsistent use of user-defined refs.",
                innerException
            ) { }
    }
}
