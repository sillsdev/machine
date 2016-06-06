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
		}

		public string Id { get; }
		public string SourceSegment { get; set; }
		public string Prefix { get; set; }
		public double ConfidenceThreshold { get; set; }
		public EngineContext EngineContext { get; }
		public IInteractiveTranslationSession Session { get; }
	}
}
