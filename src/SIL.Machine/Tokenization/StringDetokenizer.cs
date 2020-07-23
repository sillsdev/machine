using System.Collections.Generic;
using System.Text;

namespace SIL.Machine.Tokenization
{
	public enum DetokenizeOperation
	{
		NoOperation,
		MergeLeft,
		MergeRight,
		MergeRightFirstLeftSecond
	}

	public abstract class StringDetokenizer : IDetokenizer<string, string>
	{
		public string Detokenize(IEnumerable<string> tokens)
		{
			object ctxt = CreateContext();
			var currentRightLeftTokens = new HashSet<string>();
			var sb = new StringBuilder();
			bool nextMergeLeft = true;
			foreach (string token in tokens)
			{
				bool mergeRight = false;
				switch (GetOperation(ctxt, token))
				{
					case DetokenizeOperation.MergeLeft:
						nextMergeLeft = true;
						break;

					case DetokenizeOperation.MergeRight:
						mergeRight = true;
						break;

					case DetokenizeOperation.MergeRightFirstLeftSecond:
						if (currentRightLeftTokens.Contains(token))
						{
							nextMergeLeft = true;
							currentRightLeftTokens.Remove(token);
						}
						else
						{
							mergeRight = true;
							currentRightLeftTokens.Add(token);
						}
						break;

					case DetokenizeOperation.NoOperation:
						break;
				}

				if (!nextMergeLeft)
					sb.Append(" ");
				else
					nextMergeLeft = false;

				sb.Append(token);

				if (mergeRight)
					nextMergeLeft = true;
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
