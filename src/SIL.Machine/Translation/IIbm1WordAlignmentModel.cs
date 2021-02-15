using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.Machine.Translation
{
	public interface IIbm1WordAlignmentModel : IWordAlignmentModel
	{
		double GetTranslationProbability(string sourceWord, string targetWord);
		double GetTranslationProbability(int sourceWordIndex, int targetWordIndex);
	}
}
