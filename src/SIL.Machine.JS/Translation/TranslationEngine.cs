using System;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Tokenization;
using SIL.Machine.WebApi.Client;

namespace SIL.Machine.Translation
{
	public class TranslationEngine
	{
		public TranslationEngine(string baseUrl, string projectId)
			: this(baseUrl, projectId, new LatinWordTokenizer())
		{
		}

		public TranslationEngine(string baseUrl, string projectId, ITokenizer<string, int> tokenizer)
			: this(baseUrl, projectId, tokenizer, tokenizer)
		{
		}

		public TranslationEngine(string baseUrl, string projectId, ITokenizer<string, int> sourceTokenizer,
			ITokenizer<string, int> targetTokenizer)
			: this(baseUrl, projectId, sourceTokenizer, targetTokenizer, new AjaxHttpClient())
		{
		}

		public TranslationEngine(string baseUrl, string projectId, ITokenizer<string, int> sourceTokenizer,
			ITokenizer<string, int> targetTokenizer, IHttpClient httpClient)
		{
			SourceTokenizer = sourceTokenizer;
			TargetTokenizer = targetTokenizer;
			if (!baseUrl.EndsWith("/"))
				baseUrl += "/";
			RestClient = new TranslationRestClient(baseUrl, projectId, httpClient);
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
			task.ContinueWith(t => onFinished(t.IsFaulted ? null
				: new InteractiveTranslationSession(this, tokens, confidenceThreshold, t.Result)));
		}

		public void Train(Action<SmtTrainProgress> onStatusUpdate, Action<bool> onFinished)
		{
			RestClient.TrainAsync(onStatusUpdate).ContinueWith(t => onFinished(!t.IsFaulted));
		}

		public void ListenForTrainingStatus(Action<SmtTrainProgress> onStatusUpdate, Action<bool> onFinished)
		{
			RestClient.ListenForTrainingStatus(onStatusUpdate).ContinueWith(t => onFinished(!t.IsFaulted));
		}
	}
}
