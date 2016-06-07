using System;
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

	public class SimpleStringDetokenizer : IDetokenizer<string, string>
	{
		private readonly Func<string, DetokenizeOperation> _operationSelector;

		public SimpleStringDetokenizer(Func<string, DetokenizeOperation> operationSelector)
		{
			_operationSelector = operationSelector;
		}

		public string Detokenize(IEnumerable<string> tokens)
		{
			var currentRightLeftTokens = new HashSet<string>();
			var sb = new StringBuilder();
			bool nextMergeLeft = true;
			foreach (string token in tokens)
			{
				bool mergeRight = false;
				switch (_operationSelector(token))
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
	}
}
