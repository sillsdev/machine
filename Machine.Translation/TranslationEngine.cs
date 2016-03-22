using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class TranslationEngine : DisposableBase
	{
		private readonly TransferEngine _transferEngine;
		private readonly SmtEngine _smtEngine;
		private readonly SmtSession _smtSession;

		public TranslationEngine(string smtConfigFileName)
			: this(smtConfigFileName, null, null, null)
		{
		}

		public TranslationEngine(string smtConfigFileName, ISourceAnalyzer sourceAnalyzer, IMorphemeMapper morphemeMapper, ITargetGenerator targetGenerator)
		{
			_smtEngine = new SmtEngine(smtConfigFileName);
			if (sourceAnalyzer != null && morphemeMapper != null && targetGenerator != null)
				_transferEngine = new TransferEngine(sourceAnalyzer, morphemeMapper, targetGenerator);
			_smtSession = _smtEngine.StartSession();
		}

		public SegmentTranslator StartSegmentTranslation(IEnumerable<string> segment)
		{
			return new SegmentTranslator(_smtSession, _transferEngine, segment);
		}

		public void Save()
		{
			_smtEngine.SaveModels();
		}

		protected override void DisposeManagedResources()
		{
			_smtSession.Dispose();
			_smtEngine.Dispose();
		}
	}
}
