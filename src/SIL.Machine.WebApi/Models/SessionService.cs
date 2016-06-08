using System;
using System.Collections.Concurrent;
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
		private readonly ConcurrentDictionary<string, SessionContext> _sessions;
		private readonly Timer _cleanupTimer;
		private readonly SessionOptions _options;
		private bool _isTimerStopped;

		public SessionService(IOptions<SessionOptions> options)
		{
			_options = options.Value;
			_sessions = new ConcurrentDictionary<string, SessionContext>();
			_cleanupTimer = new Timer(CleanupStaleSessions, null, _options.StaleSessionCleanupFrequency, _options.StaleSessionCleanupFrequency);
		}

		private void CleanupStaleSessions(object state)
		{
			if (_isTimerStopped)
				return;

			var staleSessionIds = new List<string>();
			DateTime now = DateTime.Now;
			foreach (SessionContext sessionContext in _sessions.Values)
			{
				lock (sessionContext.EngineContext)
				{
					if (now - sessionContext.LastActiveTime > _options.SessionIdleTimeout)
					{
						sessionContext.Session.Dispose();
						sessionContext.IsActive = false;
						sessionContext.EngineContext.SessionCount--;
						staleSessionIds.Add(sessionContext.Id);
					}
				}
			}

			foreach (string sessionId in staleSessionIds)
			{
				SessionContext sessionContext;
				_sessions.TryRemove(sessionId, out sessionContext);
			}
		}

		public void Add(SessionContext sessionContext)
		{
			_sessions[sessionContext.Id] = sessionContext;
		}

		public bool TryGet(string id, out SessionDto session)
		{
			SessionContext sessionContext;
			if (!_sessions.TryGetValue(id, out sessionContext))
			{
				session = null;
				return false;
			}

			lock (sessionContext.EngineContext)
			{
				if (!sessionContext.IsActive)
				{
					session = null;
					return false;
				}

				session = sessionContext.CreateDto();
				return true;
			}
		}

		public bool Remove(string id)
		{
			SessionContext sessionContext;
			if (!_sessions.TryRemove(id, out sessionContext))
				return false;

			lock (sessionContext.EngineContext)
			{
				sessionContext.Session.Dispose();
				sessionContext.IsActive = false;
				sessionContext.EngineContext.SessionCount--;
				return true;
			}
		}

		public bool TryStartTranslation(string id, string segment, out SuggestionDto suggestion)
		{
			SessionContext sessionContext;
			if (!_sessions.TryGetValue(id, out sessionContext))
			{
				suggestion = null;
				return false;
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
				sessionContext.SourceSegmentTokenSpans = sessionContext.EngineContext.Tokenizer.Tokenize(segment).ToArray();
				sessionContext.SourceSegmentTokens = sessionContext.SourceSegmentTokenSpans.Select(span => segment.Substring(span.Start, span.Length)).ToArray();
				sessionContext.Session.TranslateInteractively(sessionContext.SourceSegmentTokens.Select(w => w.ToLowerInvariant()));
				suggestion = CreateSuggestion(sessionContext);
				sessionContext.LastActiveTime = DateTime.Now;
				return true;
			}
		}

		public bool TryUpdatePrefix(string id, string prefix, out SuggestionDto suggestion)
		{
			SessionContext sessionContext;
			if (!_sessions.TryGetValue(id, out sessionContext))
			{
				suggestion = null;
				return false;
			}

			lock (sessionContext.EngineContext)
			{
				if (!sessionContext.IsActive)
				{
					suggestion = null;
					return false;
				}
				sessionContext.Prefix = prefix;
				sessionContext.Session.SetPrefix(sessionContext.EngineContext.Tokenizer.TokenizeToStrings(prefix.ToLowerInvariant()), !prefix.EndsWith(" "));
				suggestion = CreateSuggestion(sessionContext);
				sessionContext.LastActiveTime = DateTime.Now;
				return true;
			}
		}

		private static SuggestionDto CreateSuggestion(SessionContext sessionContext)
		{
			return new SuggestionDto
			{
				Suggestion = sessionContext.Session.GetSuggestedWordIndices(sessionContext.ConfidenceThreshold)
					.Select(j => sessionContext.Session.CurrenTranslationResult.RecaseTargetWord(sessionContext.SourceSegmentTokens, j)).ToArray(),
				Alignment = sessionContext.EngineContext.Tokenizer.Tokenize(sessionContext.Prefix).Select((prefixSpan, j) => new TargetWordDto
				{
					Range = new[] {prefixSpan.Start, prefixSpan.End},
					SourceWords = sessionContext.Session.CurrenTranslationResult.GetTargetWordPairs(j).Select(wp =>
					{
						Span<int> sourceSpan = sessionContext.SourceSegmentTokenSpans[wp.SourceIndex];
						return new SourceWordDto
						{
							Range = new[] {sourceSpan.Start, sourceSpan.End},
							Confidence = wp.Confidence
						};
					}).ToArray()
				}).ToArray()
			};
		}

		public bool TryApprove(string id)
		{
			SessionContext sessionContext;
			if (!_sessions.TryGetValue(id, out sessionContext))
				return false;

			lock (sessionContext.EngineContext)
			{
				if (!sessionContext.IsActive)
					return false;
				sessionContext.Session.Approve();
				sessionContext.LastActiveTime = DateTime.Now;
				return true;
			}
		}

		protected override void DisposeManagedResources()
		{
			_isTimerStopped = true;
			_cleanupTimer.Dispose();

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
