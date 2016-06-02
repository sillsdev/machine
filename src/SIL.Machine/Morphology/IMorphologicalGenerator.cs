using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology
{
	public interface IMorphologicalGenerator
	{
		IReadOnlyObservableCollection<Morpheme> Morphemes { get; }  

		IEnumerable<string> GenerateWords(WordAnalysis wordAnalysis);
	}
}
