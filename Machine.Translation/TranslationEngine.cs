using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class TranslationEngine : DisposableBase
	{
		private readonly TransferEngine _transferEngine;
		private readonly ISmtSession _smtSession;
		private readonly Dictionary<string, string> _transferCache;
		private readonly ISegmentAligner _segmentAligner;

		public TranslationEngine(ISmtEngine smtEngine, TransferEngine transferEngine = null)
		{
			_transferEngine = transferEngine;
			_segmentAligner = new SimpleSegmentAligner(smtEngine.SegmentAligner);
			if (_transferEngine != null)
				_transferCache = new Dictionary<string, string>();
			_smtSession = smtEngine.StartSession();
		}

		public SegmentTranslator StartSegmentTranslation(IEnumerable<string> segment)
		{
			return new SegmentTranslator(_segmentAligner, _smtSession, _transferEngine, _transferCache, segment);
		}

		protected override void DisposeManagedResources()
		{
			_smtSession.Dispose();
		}
	}
}
