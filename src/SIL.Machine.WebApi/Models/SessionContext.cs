using System;
using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Translation;

namespace SIL.Machine.WebApi.Models
{
	public class SessionContext
	{
		public SessionContext(string id, EngineContext engineContext, IInteractiveTranslationSession session)
		{
			Id = id;
			EngineContext = engineContext;
			ConfidenceThreshold = 0.2;
			SourceSegment = "";
			Prefix = "";
			Session = session;
			LastActiveTime = DateTime.Now;
			IsActive = true;
		}

		public string Id { get; }
		public string SourceSegment { get; set; }
		public IList<string> SourceSegmentTokens { get; set; }
		public IList<Span<int>> SourceSegmentTokenSpans { get; set; }
		public string Prefix { get; set; }
		public double ConfidenceThreshold { get; set; }
		public EngineContext EngineContext { get; }
		public IInteractiveTranslationSession Session { get; }
		public bool IsActive { get; set; }
		public DateTime LastActiveTime { get; set; }
	}
}
