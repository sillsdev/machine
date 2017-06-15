using System;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Tokenization;
using SIL.Machine.Web;

namespace SIL.Machine.Translation
{
	public class TranslationEngine
	{
		public TranslationEngine(string baseUrl, string sourceLanguageTag, string targetLanguageTag, string projectId)
			: this(baseUrl, sourceLanguageTag, targetLanguageTag, projectId, new LatinWordTokenizer())
		{
		}

		public TranslationEngine(string baseUrl, string sourceLanguageTag, string targetLanguageTag, string projectId,
			ITokenizer<string, int> tokenizer)
			: this(baseUrl, sourceLanguageTag, targetLanguageTag, projectId, tokenizer, tokenizer)
		{
		}

		public TranslationEngine(string baseUrl, string sourceLanguageTag, string targetLanguageTag, string projectId,
			ITokenizer<string, int> sourceTokenizer, ITokenizer<string, int> targetTokenizer)
			: this(baseUrl, sourceLanguageTag, targetLanguageTag, projectId, sourceTokenizer, targetTokenizer, new AjaxHttpClient())
		{
		}

		public TranslationEngine(string baseUrl, string sourceLanguageTag, string targetLanguageTag, string projectId,
			ITokenizer<string, int> sourceTokenizer, ITokenizer<string, int> targetTokenizer, IHttpClient httpClient)
		{
			SourceTokenizer = sourceTokenizer;
			TargetTokenizer = targetTokenizer;
			if (!baseUrl.EndsWith("/"))
				baseUrl += "/";
			RestClient = new TranslationRestClient(baseUrl, sourceLanguageTag, targetLanguageTag, projectId, httpClient);
			ErrorCorrectionModel = new ErrorCorrectionModel();
		}

		internal TranslationRestClient RestClient { get; }
		internal ITokenizer<string, int> SourceTokenizer { get; }
		internal ITokenizer<string, int> TargetTokenizer { get; }
		internal ErrorCorrectionModel ErrorCorrectionModel { get; }

		public void TranslateInteractively(string sourceSegment, double confidenceThreshold,
			Action<InteractiveTranslationSession> onFinished)
		{
			string[] tokens = SourceTokenizer.TokenizeToStrings(sourceSegment).ToArray();
			Task<InteractiveTranslationResult> task = RestClient.TranslateInteractivelyAsync(tokens);
			task.ContinueWith(t => onFinished(t.Result == null ? null
				: new InteractiveTranslationSession(this, tokens, confidenceThreshold, t.Result)));
		}
	}
}
