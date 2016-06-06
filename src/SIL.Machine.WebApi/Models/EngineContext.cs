using Nito.AsyncEx;
using SIL.Machine.Translation;

namespace SIL.Machine.WebApi.Models
{
	public class EngineContext
	{
		public EngineContext(string sourceLanguageTag, string targetLanguageTag)
		{
			SourceLanguageTag = sourceLanguageTag;
			TargetLanguageTag = targetLanguageTag;
			Mutex = new AsyncLock();
		}

		public string SourceLanguageTag { get; }
		public string TargetLanguageTag { get; }
		public AsyncLock Mutex { get; }
		public HybridTranslationEngine Engine { get; set; }
	}
}
