using System;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Tokenization;
using SIL.Machine.WebApi.Client;

namespace SIL.Machine.Translation
{
	public class TranslationEngine
	{
		public TranslationEngine(string baseUrl, string projectId, IHttpClient httpClient = null)
		{
			var wordTokenizer = new LatinWordTokenizer();
			SourceWordTokenizer = wordTokenizer;
			TargetWordTokenizer = wordTokenizer;
			var segmentTokenizer = new LatinSentenceTokenizer();
			SourceSegmentTokenizer = segmentTokenizer;
			TargetSegmentTokenizer = segmentTokenizer;
			if (!baseUrl.EndsWith("/"))
				baseUrl += "/";
			RestClient = new TranslationRestClient(baseUrl, projectId, httpClient ?? new AjaxHttpClient());
			ErrorCorrectionModel = new ErrorCorrectionModel();
		}

		internal ITokenizer<string, int> SourceWordTokenizer { get; set; }
		internal ITokenizer<string, int> TargetWordTokenizer { get; set; }
		internal ITokenizer<string, int> SourceSegmentTokenizer { get; set; }
		internal ITokenizer<string, int> TargetSegmentTokenizer { get; set; }
		internal TranslationRestClient RestClient { get; }
		internal ErrorCorrectionModel ErrorCorrectionModel { get; }

		public void TranslateInteractively(string sourceSegment, double confidenceThreshold,
			Action<InteractiveTranslationSession> onFinished)
		{
			string[] tokens = SourceWordTokenizer.TokenizeToStrings(sourceSegment).ToArray();
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
