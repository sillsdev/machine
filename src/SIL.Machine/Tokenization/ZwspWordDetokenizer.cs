using System.Collections.Generic;

namespace SIL.Machine.Tokenization
{
    public class ZwspWordDetokenizer : LatinWordDetokenizer
    {
        protected override DetokenizeOperation GetOperation(object context, string token)
        {
            if (char.IsWhiteSpace(token[0]))
                return DetokenizeOperation.MergeBoth;
            return base.GetOperation(context, token);
        }

        protected override string GetSeparator(
            IReadOnlyList<string> tokens,
            IReadOnlyList<DetokenizeOperation> ops,
            int index
        )
        {
            if (
                index < tokens.Count - 1
                && ops[index + 1] == DetokenizeOperation.MergeRight
                && char.IsPunctuation(tokens[index + 1][0])
            )
            {
                return " ";
            }
            else if (ops[index] == DetokenizeOperation.MergeLeft && char.IsPunctuation(tokens[index][0]))
                return " ";
            return "\u200b";
        }
    }
}
