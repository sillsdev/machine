using System;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public class UsfmParsingException : Exception
    {
        public UsfmParsingException(UsfmParserState state, Exception exception)
            : base(
                $"Failed to parse at line {state.LineNumber} column {state.ColumnNumber} verse ref {state.VerseRef} with surrounding tokens [{string.Join(",", state.Tokens.ToList().GetRange(Math.Max(state.Index - 3, 0), Math.Min(7, state.Tokens.Count - (state.Index - 3))).Select(t => $"{t.Text} (TokenType={t.Type})"))}]",
                exception
            ) { }
    }
}
