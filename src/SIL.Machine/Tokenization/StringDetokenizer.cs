using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.Machine.Tokenization
{
    public enum DetokenizeOperation
    {
        NoOperation,
        MergeLeft,
        MergeRight,
        MergeBoth
    }

    public abstract class StringDetokenizer : IDetokenizer<string, string>
    {
        public string Detokenize(IEnumerable<string> tokens)
        {
            string[] tokenArray = tokens.ToArray();
            object context = CreateContext();
            DetokenizeOperation[] ops = tokenArray.Select(t => GetOperation(context, t)).ToArray();
            var sb = new StringBuilder();
            for (int i = 0; i < tokenArray.Length; i++)
            {
                sb.Append(TransformToken(tokenArray[i]));

                bool appendSeparator = true;
                if (i + 1 == ops.Length)
                    appendSeparator = false;
                else if (ops[i + 1] == DetokenizeOperation.MergeLeft || ops[i + 1] == DetokenizeOperation.MergeBoth)
                    appendSeparator = false;
                else if (ops[i] == DetokenizeOperation.MergeRight || ops[i] == DetokenizeOperation.MergeBoth)
                    appendSeparator = false;

                if (appendSeparator)
                    sb.Append(GetSeparator(tokenArray, ops, i));
            }
            return sb.ToString();
        }

        protected virtual object CreateContext()
        {
            return null;
        }

        protected abstract DetokenizeOperation GetOperation(object context, string token);

        protected virtual string GetSeparator(
            IReadOnlyList<string> tokens,
            IReadOnlyList<DetokenizeOperation> ops,
            int index
        )
        {
            return " ";
        }

        protected virtual string TransformToken(string token)
        {
            return token;
        }
    }
}
