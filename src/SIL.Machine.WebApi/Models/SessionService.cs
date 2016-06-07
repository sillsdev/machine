using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;
using SIL.Machine.Annotations;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Models
{
	public class SessionService : DisposableBase, ISessionService
	{
		private readonly object _lockObject;
		private readonly Dictionary<string, SessionContext> _sessions;
		private readonly Dictionary<string, DateTime> _lastActiveTimes;
		private readonly Timer _cleanupTimer;
		private readonly SessionOptions _options;
		private bool _isTimerStopped;

		public SessionService(IOptions<SessionOptions> options)
		{
			_options = options.Value;
			_lockObject = new object();
			_sessions = new Dictionary<string, SessionContext>();
			_lastActiveTimes = new Dictionary<string, DateTime>();
			_cleanupTimer = new Timer(CleanupStaleSessions, null, _options.StaleSessionCleanupFrequency, _options.StaleSessionCleanupFrequency);
		}

		private void CleanupStaleSessions(object state)
		{
			if (_isTimerStopped)
				return;

			SessionContext[] staleSessionContexts;
			lock (_lockObject)
			{
				staleSessionContexts = _sessions.Values.Where(sc => DateTime.Now - _lastActiveTimes[sc.Id] > _options.SessionIdleTimeout).ToArray();
				foreach (SessionContext sessionContext in staleSessionContexts)
				{
					_sessions.Remove(sessionContext.Id);
					_lastActiveTimes.Remove(sessionContext.Id);
				}
			}

			foreach (SessionContext sessionContext in staleSessionContexts)
			{
				lock (sessionContext.EngineContext)
				{
					sessionContext.Session.Dispose();
					sessionContext.IsActive = false;
					sessionContext.EngineContext.SessionCount--;
				}
			}
		}

		public void Add(SessionContext sessionContext)
		{
			lock (_lockObject)
			{
				_sessions[sessionContext.Id] = sessionContext;
				_lastActiveTimes[sessionContext.Id] = DateTime.Now;
			}
		}

		public bool TryGet(string id, out SessionContext sessionContext)
		{
			lock (_lockObject)
				return _sessions.TryGetValue(id, out sessionContext);
		}

		public bool Remove(string id)
		{
			SessionContext sessionContext;
			lock (_lockObject)
			{
				if (_sessions.TryGetValue(id, out sessionContext))
				{
					_sessions.Remove(id);
					_lastActiveTimes.Remove(id);
				}
				else
				{
					return false;
				}
			}

			lock (sessionContext.EngineContext)
			{
				sessionContext.Session.Dispose();
				sessionContext.IsActive = false;
				sessionContext.EngineContext.SessionCount--;
				return true;
			}
		}

		public bool TryStartTranslation(string id, string segment, out Suggestion suggestion)
		{
			SessionContext sessionContext;
			lock (_lockObject)
			{
				if (_sessions.TryGetValue(id, out sessionContext))
				{
					_lastActiveTimes[id] = DateTime.Now;
				}
				else
				{
					suggestion = null;
					return false;
				}
			}

			lock (sessionContext.EngineContext)
			{
				if (!sessionContext.IsActive)
				{
					suggestion = null;
					return false;
				}
				sessionContext.SourceSegment = segment;
				sessionContext.Prefix = "";
				sessionContext.Session.TranslateInteractively(sessionContext.EngineContext.Tokenizer.TokenizeToStrings(segment));
				suggestion = CreateSuggestion(sessionContext);
				return true;
			}
		}

		public bool TryUpdatePrefix(string id, string prefix, out Suggestion suggestion)
		{
			SessionContext sessionContext;
			lock (_lockObject)
			{
				if (_sessions.TryGetValue(id, out sessionContext))
				{
					_lastActiveTimes[id] = DateTime.Now;
				}
				else
				{
					suggestion = null;
					return false;
				}
			}

			lock (sessionContext.EngineContext)
			{
				if (!sessionContext.IsActive)
				{
					suggestion = null;
					return false;
				}
				sessionContext.Prefix = prefix;
				sessionContext.Session.SetPrefix(sessionContext.EngineContext.Tokenizer.TokenizeToStrings(prefix), !prefix.EndsWith(" "));
				suggestion = CreateSuggestion(sessionContext);
				return true;
			}
		}

		private static Suggestion CreateSuggestion(SessionContext sessionContext)
		{
			IEnumerable<string> suggestedWords = sessionContext.Session.GetSuggestedWordIndices(sessionContext.ConfidenceThreshold)
				.Select(j => sessionContext.Session.CurrenTranslationResult.RecaseTargetWord(j));
			IEnumerable<Span<int>> sourceSegmentTokens = sessionContext.EngineContext.Tokenizer.Tokenize(sessionContext.SourceSegment);
			IEnumerable<Span<int>> prefixTokens = sessionContext.EngineContext.Tokenizer.Tokenize(sessionContext.Prefix);
			TranslationResult result = sessionContext.Session.CurrenTranslationResult;
			return new Suggestion(suggestedWords, sourceSegmentTokens, prefixTokens, result);
		}

		public bool TryApprove(string id)
		{
			SessionContext sessionContext;
			lock (_lockObject)
			{
				if (_sessions.TryGetValue(id, out sessionContext))
					_lastActiveTimes[id] = DateTime.Now;
				else
					return false;
			}

			lock (sessionContext.EngineContext)
			{
				if (!sessionContext.IsActive)
					return false;
				sessionContext.Session.Approve();
				return true;
			}
		}

		protected override void DisposeManagedResources()
		{
			_isTimerStopped = true;
			_cleanupTimer.Dispose();

			lock (_lockObject)
			{
				foreach (SessionContext sessionContext in _sessions.Values)
				{
					lock (sessionContext.EngineContext)
					{
						sessionContext.Session.Dispose();
						sessionContext.IsActive = false;
						sessionContext.EngineContext.SessionCount--;
					}
				}
				_sessions.Clear();
			}
		}
	}
}
