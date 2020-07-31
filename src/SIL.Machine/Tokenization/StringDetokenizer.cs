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
			object ctxt = CreateContext();
			DetokenizeOperation[] ops = tokenArray.Select(t => GetOperation(ctxt, t)).ToArray();
			var sb = new StringBuilder();
			for (int i = 0; i < tokenArray.Length; i++)
			{
				sb.Append(tokenArray[i]);

				bool isAppendSpace = true;
				if (i + 1 == ops.Length)
					isAppendSpace = false;
				else if (ops[i + 1] == DetokenizeOperation.MergeLeft || ops[i + 1] == DetokenizeOperation.MergeBoth)
					isAppendSpace = false;
				else if (ops[i] == DetokenizeOperation.MergeRight || ops[i] == DetokenizeOperation.MergeBoth)
					isAppendSpace = false;

				if (isAppendSpace)
					sb.Append(" ");
			}
			return sb.ToString();
		}

		protected virtual object CreateContext()
		{
			return null;
		}

		protected abstract DetokenizeOperation GetOperation(object ctxt, string token);
	}
}
