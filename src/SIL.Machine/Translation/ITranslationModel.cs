using System;
using System.Collections.Generic;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
	public interface ITranslationModel : IDisposable
	{
		ITranslationEngine CreateEngine();
		ITrainer CreateTrainer(IEnumerable<ParallelTextRow> corpus);
	}
}
