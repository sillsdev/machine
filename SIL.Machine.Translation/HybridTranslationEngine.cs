using System;
using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;
using SIL.Progress;

namespace SIL.Machine.Translation
{
	public class HybridTranslationEngine : DisposableBase, IImtEngine
	{
		private readonly ITranslationEngine _transferEngine;
		private readonly ISmtEngine _smtEngine;
		private readonly HashSet<HybridTranslationSession> _sessions;

		public HybridTranslationEngine(ISmtEngine smtEngine, ITranslationEngine transferEngine = null)
		{
			_smtEngine = smtEngine;
			_transferEngine = transferEngine;
			_sessions = new HashSet<HybridTranslationSession>();
		}

		public IEnumerable<IEnumerable<string>> SourceCorpus { get; set; }
		public IEnumerable<IEnumerable<string>> TargetCorpus { get; set; }

		public void Rebuild(IProgress progress = null)
		{
			lock (_sessions)
			{
				if (_sessions.Count > 0)
					throw new InvalidOperationException("The engine cannot be trained while there are active sessions open.");

				if (SourceCorpus != null && TargetCorpus != null)
					_smtEngine.Train(SourceCorpus, TargetCorpus, progress);
			}
		}

		public void Save()
		{
			_smtEngine.SaveModels();
		}

		public IImtSession StartSession()
		{
			var session = new HybridTranslationSession(this, _smtEngine.StartSession(), _transferEngine);
			lock (_sessions)
				_sessions.Add(session);
			return session;
		}

		internal void RemoveSession(HybridTranslationSession session)
		{
			lock (_sessions)
				_sessions.Remove(session);
		}

		protected override void DisposeManagedResources()
		{
			lock (_sessions)
			{
				foreach (HybridTranslationSession session in _sessions.ToArray())
					session.Dispose();
			}
		}
	}
}
