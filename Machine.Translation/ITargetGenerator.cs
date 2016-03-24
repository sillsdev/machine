using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public interface ITargetGenerator
	{
		IReadOnlyObservableCollection<MorphemeInfo> Morphemes { get; }  

		IEnumerable<string> GenerateWords(WordAnalysis wordAnalysis);
	}
}
