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
			: this(baseUrl, projectId, new AjaxHttpClient())
		{
		}

		public TranslationEngine(string baseUrl, string projectId, IHttpClient httpClient)
		{
			var wordTokenizer = new LatinWordTokenizer();
			SourceWordTokenizer = wordTokenizer;
			TargetWordTokenizer = wordTokenizer;
			var segmentTokenizer = new LatinSentenceTokenizer();
			SourceSegmentTokenizer = segmentTokenizer;
			TargetSegmentTokenizer = segmentTokenizer;
			if (!baseUrl.EndsWith("/"))
				baseUrl += "/";
			RestClient = new TranslationRestClient(baseUrl, projectId, httpClient);
			ErrorCorrectionModel = new ErrorCorrectionModel();
		}

		public ITokenizer<string, int> SourceWordTokenizer { get; set; }
		public ITokenizer<string, int> TargetWordTokenizer { get; set; }
		public ITokenizer<string, int> SourceSegmentTokenizer { get; set; }
		public ITokenizer<string, int> TargetSegmentTokenizer { get; set; }

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

		public int[] TokenizeSourceSegment(string sourceSegment)
		{
			return SourceWordTokenizer.Tokenize(sourceSegment).Select(s => s.Start).ToArray();
		}

		public int[] TokenizeTargetSegment(string targetSegment)
		{
			return TargetWordTokenizer.Tokenize(targetSegment).Select(s => s.Start).ToArray();
		}

		public int[] TokenizeSourceDocument(string sourceDocument)
		{
			return SourceSegmentTokenizer.Tokenize(sourceDocument).Select(s => s.Start).ToArray();
		}

		public int[] TokenizeTargetDocument(string targetDocument)
		{
			return TargetSegmentTokenizer.Tokenize(targetDocument).Select(s => s.Start).ToArray();
		}
	}
}
