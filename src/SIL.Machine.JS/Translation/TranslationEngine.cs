using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Tokenization;
using SIL.Machine.WebApi.Client;

namespace SIL.Machine.Translation
{
	public class TranslationEngine
	{
		internal const int MaxSegmentSize = 110;

		private readonly CancellationTokenSource _cts;

		public TranslationEngine(string baseUrl, string projectId, IHttpClient httpClient = null)
		{
			ProjectId = projectId;
			var wordTokenizer = new LatinWordTokenizer();
			SourceWordTokenizer = wordTokenizer;
			TargetWordTokenizer = wordTokenizer;
			RestClient = new TranslationRestClient(baseUrl, httpClient ?? new AjaxHttpClient());
			ErrorCorrectionModel = new ErrorCorrectionModel();
			_cts = new CancellationTokenSource();
		}

		internal string ProjectId { get; }
		internal StringTokenizer SourceWordTokenizer { get; }
		internal StringTokenizer TargetWordTokenizer { get; }
		internal TranslationRestClient RestClient { get; }
		internal ErrorCorrectionModel ErrorCorrectionModel { get; }

		public void TranslateInteractively(string sourceSegment, double confidenceThreshold,
			Action<InteractiveTranslationSession> onFinished)
		{
			string[] tokens = SourceWordTokenizer.TokenizeToStrings(sourceSegment).ToArray();
			if (tokens.Length > MaxSegmentSize)
			{
				var results = new HybridInteractiveTranslationResult(new WordGraph(), null);
				onFinished(new InteractiveTranslationSession(this, tokens, confidenceThreshold, results));
				return;
			}

			Task<HybridInteractiveTranslationResult> task = RestClient.TranslateInteractivelyAsync(ProjectId, tokens);
			task.ContinueWith(t => onFinished(t.IsFaulted ? null
				: new InteractiveTranslationSession(this, tokens, confidenceThreshold, t.Result)));
		}

		public void Train(Action<ProgressStatus> onStatusUpdate, Action<TrainResultCode> onFinished)
		{
			RestClient.TrainAsync(ProjectId, onStatusUpdate, _cts.Token)
				.ContinueWith(t => onFinished(GetResultCode(t)));
		}

		public void StartTraining(Action<bool> onFinished)
		{
			RestClient.StartTrainingAsync(ProjectId).ContinueWith(t => onFinished(!t.IsFaulted));
		}

		public void ListenForTrainingStatus(Action<ProgressStatus> onStatusUpdate, Action<TrainResultCode> onFinished)
		{
			RestClient.ListenForTrainingStatusAsync(ProjectId, onStatusUpdate, _cts.Token)
				.ContinueWith(t => onFinished(GetResultCode(t)));
		}

		public void GetConfidence(Action<bool, double> onFinished)
		{
			RestClient.GetEngineAsync(ProjectId).ContinueWith(t =>
				{
					if (t.IsFaulted)
						onFinished(false, 0);
					else
						onFinished(true, t.Result.Confidence);
				});
		}

		public void Close()
		{
			_cts.Cancel();
			_cts.Dispose();
		}

		private static TrainResultCode GetResultCode(Task task)
		{
			if (task.IsFaulted)
			{
				if (task.Exception.InnerException is HttpException)
					return TrainResultCode.HttpError;
				else
					return TrainResultCode.TrainError;
			}
			return TrainResultCode.NoError;
		}
	}
}
