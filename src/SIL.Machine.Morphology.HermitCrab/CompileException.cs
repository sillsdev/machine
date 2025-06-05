using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.Machine.Morphology.HermitCrab
{
    public class CompileException : Exception
    {
        public CompileException() { }

        public CompileException(string message)
            : base(message) { }

        public CompileException(string message, Exception inner)
            : base(message, inner) { }
    }
}
