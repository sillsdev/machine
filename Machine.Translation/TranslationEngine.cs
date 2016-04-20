using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class TranslationEngine : DisposableBase
	{
		private readonly TransferEngine _transferEngine;
		private readonly ISmtEngine _smtEngine;
		private readonly ISmtSession _smtSession;
		private readonly Dictionary<string, string> _transferCache;
		private readonly ISingleWordAlignmentModel _swAlignModel;

		public TranslationEngine(ISmtEngine smtEngine, TransferEngine transferEngine = null)
		{
			_smtEngine = smtEngine;
			_transferEngine = transferEngine;
			_swAlignModel = new SimpleSingleWordAlignmentModel(_smtEngine);
			if (_transferEngine != null)
				_transferCache = new Dictionary<string, string>();
			_smtSession = _smtEngine.StartSession();
		}

		public SegmentTranslator StartSegmentTranslation(IEnumerable<string> segment)
		{
			return new SegmentTranslator(_swAlignModel, _smtSession, _transferEngine, _transferCache, segment);
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
