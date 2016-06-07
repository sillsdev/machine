using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Models
{
	public class SessionService : DisposableBase, ISessionService
	{
		private readonly Dictionary<string, SessionContext> _sessions;

		public SessionService()
		{
			_sessions = new Dictionary<string, SessionContext>();
		}

		public void Add(SessionContext sessionContext)
		{
			lock (_sessions)
				_sessions[sessionContext.Id] = sessionContext;
		}

		public bool TryGet(string id, out SessionContext sessionContext)
		{
			lock (_sessions)
				return _sessions.TryGetValue(id, out sessionContext);
		}

		public bool Remove(string id)
		{
			SessionContext sessionContext;
			lock (_sessions)
			{
				if (_sessions.TryGetValue(id, out sessionContext))
					_sessions.Remove(id);
				else
					return false;
			}

			using (sessionContext.EngineContext.Mutex.Lock())
			{
				sessionContext.Session.Dispose();
				return true;
			}
		}

		public bool TryStartTranslation(string id, string segment, out Suggestion suggestion)
		{
			SessionContext sessionContext;
			lock (_sessions)
			{
				if (!_sessions.TryGetValue(id, out sessionContext))
				{
					suggestion = null;
					return false;
				}
			}

			using (sessionContext.EngineContext.Mutex.Lock())
			{
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
			lock (_sessions)
			{
				if (!_sessions.TryGetValue(id, out sessionContext))
				{
					suggestion = null;
					return false;
				}
			}

			using (sessionContext.EngineContext.Mutex.Lock())
			{
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
			lock (_sessions)
			{
				if (!_sessions.TryGetValue(id, out sessionContext))
					return false;
			}

			using (sessionContext.EngineContext.Mutex.Lock())
			{
				sessionContext.Session.Approve();
				return true;
			}
		}

		protected override void DisposeManagedResources()
		{
			SessionContext[] sessions;
			lock (_sessions)
			{
				sessions = _sessions.Values.ToArray();
				_sessions.Clear();
			}

			foreach (SessionContext sessionContext in sessions)
			{
				using (sessionContext.EngineContext.Mutex.Lock())
					sessionContext.Session.Dispose();
			}
		}
	}
}
