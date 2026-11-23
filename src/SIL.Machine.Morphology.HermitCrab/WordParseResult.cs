using System;
using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab
{
    public sealed class WordParseResult
    {
        internal WordParseResult(string surfaceForm, IReadOnlyList<Word> analyses, object trace, Exception error)
        {
            SurfaceForm = surfaceForm;
            Analyses = analyses;
            Trace = trace;
            Error = error;
        }

        public string SurfaceForm { get; }

        public IReadOnlyList<Word> Analyses { get; }

        public object Trace { get; }

        public Exception Error { get; }
    }
}
