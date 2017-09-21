using System;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Tokenization;
using SIL.Machine.WebApi.Client;

namespace SIL.Machine.Translation
{
	public class TranslationEngine
	{
		public TranslationEngine(string baseUrl, string id, IHttpClient httpClient = null)
		{
			Id = id;
			var wordTokenizer = new LatinWordTokenizer();
			SourceWordTokenizer = wordTokenizer;
			TargetWordTokenizer = wordTokenizer;
			RestClient = new TranslationEngineRestClient(baseUrl, httpClient ?? new AjaxHttpClient());
			ErrorCorrectionModel = new ErrorCorrectionModel();
		}

		internal string Id { get; }
		internal StringTokenizer SourceWordTokenizer { get; }
		internal StringTokenizer TargetWordTokenizer { get; }
		internal TranslationEngineRestClient RestClient { get; }
		internal ErrorCorrectionModel ErrorCorrectionModel { get; }

		public void TranslateInteractively(string sourceSegment, double confidenceThreshold,
			Action<InteractiveTranslationSession> onFinished)
		{
			string[] tokens = SourceWordTokenizer.TokenizeToStrings(sourceSegment).ToArray();
			Task<InteractiveTranslationResult> task = RestClient.TranslateInteractivelyAsync(Id, tokens);
			task.ContinueWith(t => onFinished(t.IsFaulted ? null
				: new InteractiveTranslationSession(this, tokens, confidenceThreshold, t.Result)));
		}

		public void Train(Action<SmtTrainProgress> onStatusUpdate, Action<bool> onFinished)
		{
			RestClient.TrainAsync(Id, onStatusUpdate).ContinueWith(t => onFinished(!t.IsFaulted));
		}

		public void ListenForTrainingStatus(Action<SmtTrainProgress> onStatusUpdate, Action<bool> onFinished)
		{
			RestClient.ListenForTrainingStatus(Id, onStatusUpdate).ContinueWith(t => onFinished(!t.IsFaulted));
		}
	}
}
