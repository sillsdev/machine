using SIL.Collections;

namespace SIL.Machine.Translation
{
	public interface ITargetGenerator
	{
		IReadOnlyObservableCollection<Morpheme> Morphemes { get; }  

		string GenerateWord(WordAnalysis wordAnalysis);
	}
}
