using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ISourceAnalyzer
	{
		IEnumerable<WordAnalysis> AnalyzeWord(string word);
	}
}
