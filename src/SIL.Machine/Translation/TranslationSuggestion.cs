using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class TranslationSuggestion
	{
		public TranslationSuggestion()
			: this(Enumerable.Empty<int>(), 0)
		{
		}

		public TranslationSuggestion(IEnumerable<int> indices, double confidence)
		{
			TargetWordIndices = indices.ToArray();
			Confidence = confidence;
		}

		public IReadOnlyList<int> TargetWordIndices { get; }
		public double Confidence { get; }
	}
}
