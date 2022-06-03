using System.Collections.Generic;

namespace SIL.Machine.Tokenization.SentencePiece
{
    public class SentencePieceDetokenizer : IDetokenizer<string, string>
    {
        public string Detokenize(IEnumerable<string> tokens)
        {
            return string.Join("", tokens).Replace("▁", " ").TrimStart();
        }
    }
}
