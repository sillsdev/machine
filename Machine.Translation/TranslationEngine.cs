using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class TranslationEngine : DisposableBase
	{
		private readonly TransferEngine _transferEngine;
		private readonly ISmtEngine _smtEngine;
		private readonly ISmtSession _smtSession;

		public TranslationEngine(ISmtEngine smtEngine, TransferEngine transferEngine = null)
		{
			_smtEngine = smtEngine;
			_transferEngine = transferEngine;
			_smtSession = _smtEngine.StartSession();
		}

		public SegmentTranslator StartSegmentTranslation(IEnumerable<string> segment)
		{
			return new SegmentTranslator(_smtEngine, _smtSession, _transferEngine, segment);
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
