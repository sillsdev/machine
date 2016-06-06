using System.Collections.Generic;
using System.Linq;
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
				TranslationResult result = sessionContext.Session.TranslateInteractively(segment.Tokenize());
				suggestion = CreateSuggestion(sessionContext, result);
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
				TranslationResult result = sessionContext.Session.SetPrefix(prefix.Tokenize(), !prefix.EndsWith(" "));
				suggestion = CreateSuggestion(sessionContext, result);
				return true;
			}
		}

		private static Suggestion CreateSuggestion(SessionContext sessionContext, TranslationResult result)
		{
			return new Suggestion(result, GetSuggestedWordIndices(sessionContext, result), sessionContext.SourceSegment, sessionContext.Prefix);
		}

		private static IEnumerable<int> GetSuggestedWordIndices(SessionContext sessionContext, TranslationResult result)
		{
			int lookaheadCount = 1;
			for (int i = 0; i < result.SourceSegment.Count; i++)
			{
				int wordPairCount = result.GetSourceWordPairs(i).Count();
				if (wordPairCount == 0)
					lookaheadCount++;
				else
					lookaheadCount += wordPairCount - 1;
			}
			int j;
			for (j = 0; j < result.TargetSegment.Count; j++)
			{
				int wordPairCount = result.GetTargetWordPairs(j).Count();
				if (wordPairCount == 0)
					lookaheadCount++;
			}
			j = sessionContext.Session.Prefix.Count;
			// ensure that we include a partial word as a suggestion
			if (sessionContext.Session.IsLastWordPartial)
				j--;
			bool inPhrase = false;
			while (j < result.TargetSegment.Count && (lookaheadCount > 0 || inPhrase))
			{
				string word = result.TargetSegment[j];
				// stop suggesting at punctuation
				if (word.All(char.IsPunctuation))
					break;

				if (result.GetTargetWordConfidence(j) >= sessionContext.ConfidenceThreshold
					|| result.GetTargetWordPairs(j).Any(awi => (awi.Sources & TranslationSources.Transfer) == TranslationSources.Transfer))
				{
					yield return j;
					inPhrase = true;
					lookaheadCount--;
				}
				else
				{
					// skip over inserted words
					if (result.GetTargetWordPairs(j).Any())
					{
						lookaheadCount--;
						// only suggest the first word/phrase we find
						if (inPhrase)
							break;
					}
				}
				j++;
			}
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
