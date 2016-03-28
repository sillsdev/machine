using System.Linq;

namespace SIL.Machine.Translation
{
	public class TransferEngine
	{
		private readonly ISourceAnalyzer _sourceAnalyzer;
		private readonly ITransferer _transferer;
		private readonly ITargetGenerator _targetGenerator;

		public TransferEngine(ISourceAnalyzer sourceAnalyzer, ITransferer transferer, ITargetGenerator targetGenerator)
		{
			_sourceAnalyzer = sourceAnalyzer;
			_transferer = transferer;
			_targetGenerator = targetGenerator;
		}

		public bool TryTranslateWord(string sourceWord, out string targetWord)
		{
			targetWord = null;
			foreach (WordAnalysis sourceAnalysis in _sourceAnalyzer.AnalyzeWord(sourceWord))
			{
				foreach (WordAnalysis targetAnalysis in _transferer.Transfer(sourceAnalysis))
				{
					targetWord = _targetGenerator.GenerateWords(targetAnalysis).FirstOrDefault();
					if (targetWord != null)
						return true;
				}
			}
			return false;
		}
	}
}
