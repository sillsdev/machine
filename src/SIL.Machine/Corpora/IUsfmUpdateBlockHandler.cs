using System;

namespace SIL.Machine.Corpora
{
    public interface IUsfmUpdateBlockHandler
    {
        UsfmUpdateBlock ProcessBlock(UsfmUpdateBlock block);
    }

    public class UsfmUpdateBlockHandlerException : Exception
    {
        public UsfmUpdateBlock Block { get; }

        public UsfmUpdateBlockHandlerException(string message, UsfmUpdateBlock block)
            : base(message)
        {
            Block = block;
        }

        public UsfmUpdateBlockHandlerException(string message, Exception exception, UsfmUpdateBlock block)
            : base(message, exception)
        {
            Block = block;
        }
    }
}
