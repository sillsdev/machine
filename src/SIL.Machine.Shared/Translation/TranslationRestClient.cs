using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIL.Machine.Web;

namespace SIL.Machine.Translation
{
	public class TranslationRestClient
	{
		public TranslationRestClient(string baseUrl, string sourceLanguageTag, string targetLanguageTag, string projectId, IHttpClient httpClient)
		{
			SourceLanguageTag = sourceLanguageTag;
			TargetLanguageTag = targetLanguageTag;
			ProjectId = projectId;
			HttpClient = httpClient;
			ErrorCorrectionModel = new ErrorCorrectionModel();
			HttpClient.BaseUrl = baseUrl;
		}

		public string SourceLanguageTag { get; }
		public string TargetLanguageTag { get; }
		public string ProjectId { get; }
		public string BaseUrl => HttpClient.BaseUrl;
		internal IHttpClient HttpClient { get; }
		internal ErrorCorrectionModel ErrorCorrectionModel { get; }

		public async Task<Tuple<WordGraph, TranslationResult>> TranslateInteractivelyAsync(IReadOnlyList<string> sourceSegment)
		{
			string url = string.Format("translation/engines/{0}/{1}/projects/{2}/actions/interactive-translate", SourceLanguageTag, TargetLanguageTag, ProjectId);
			string body = HttpClient.ToJson(sourceSegment);
			HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Post, url, body, "application/json");
			if (response.IsSuccess)
			{
				dynamic json = HttpClient.ParseJson(response.Content);
				WordGraph wordGraph = CreateWordGraph(json["wordGraph"]);
				TranslationResult ruleResult = CreateRuleResult(sourceSegment, json["ruleResult"]);
				return Tuple.Create(wordGraph, ruleResult);
			}

			return null;
		}

		public async Task<bool> TrainSegmentPairAsync(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			string url = string.Format("translation/engines/{0}/{1}/projects/{2}/actions/train-segment", SourceLanguageTag, TargetLanguageTag, ProjectId);
			string body = HttpClient.ToJson(new {sourceSegment, targetSegment});
			HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Post, url, body, "application/json");
			return response.IsSuccess;
		}

		private WordGraph CreateWordGraph(object json)
		{
			dynamic jsonWordGraph = json;

			double initialStateScore = jsonWordGraph["initialStateScore"];

			var finalStates = new List<int>();
			var jsonFinalStates = jsonWordGraph["finalStates"];
			foreach (var jsonFinalState in jsonFinalStates)
				finalStates.Add(jsonFinalState);

			var jsonArcs = jsonWordGraph["arcs"];
			var arcs = new List<WordGraphArc>();
			foreach (var jsonArc in jsonArcs)
			{
				int prevState = jsonArc["prevState"];
				int nextState = jsonArc["nextState"];
				double score = jsonArc["score"];

				var jsonWords = jsonArc["words"];
				var words = new List<string>();
				foreach (var jsonWord in jsonWords)
					words.Add(jsonWord);

				var jsonConfidences = jsonArc["confidences"];
				var confidences = new List<double>();
				foreach (var jsonConfidence in jsonConfidences)
					confidences.Add(jsonConfidence);

				int srcStartIndex = jsonArc["sourceStartIndex"];
				int endStartIndex = jsonArc["sourceEndIndex"];
				bool isUnknown = jsonArc["isUnknown"];

				var jsonAlignment = jsonArc["alignment"];
				var alignment = new WordAlignmentMatrix(endStartIndex - srcStartIndex + 1, words.Count);
				foreach (var jsonAligned in jsonAlignment)
				{
					int i = jsonAligned["sourceIndex"];
					int j = jsonAligned["targetIndex"];
					alignment[i, j] = AlignmentType.Aligned;
				}

				arcs.Add(new WordGraphArc(prevState, nextState, score, words.ToArray(), alignment, confidences.ToArray(),
					srcStartIndex, endStartIndex, isUnknown));
			}

			return new WordGraph(arcs, finalStates, initialStateScore);
		}

		private TranslationResult CreateRuleResult(IReadOnlyList<string> sourceSegment, object json)
		{
			if (json == null)
				return null;

			dynamic jsonResult = json;

			var jsonTarget = jsonResult["target"];
			var targetSegment = new List<string>();
			foreach (var jsonWord in jsonTarget)
				targetSegment.Add(jsonWord);

			var jsonConfidences = jsonResult["confidences"];
			var confidences = new List<double>();
			foreach (var jsonConfidence in jsonConfidences)
				confidences.Add(jsonConfidence);

			var jsonSources = jsonResult["sources"];
			var sources = new List<TranslationSources>();
			foreach (var jsonSource in jsonSources)
				sources.Add((TranslationSources)jsonSource);

			var jsonAlignment = jsonResult["alignment"];
			var alignment = new WordAlignmentMatrix(sourceSegment.Count, targetSegment.Count);
			foreach (var jsonAligned in jsonAlignment)
			{
				int i = jsonAligned["sourceIndex"];
				int j = jsonAligned["targetIndex"];
				alignment[i, j] = AlignmentType.Aligned;
			}

			return new TranslationResult(sourceSegment, targetSegment, confidences, sources, alignment);
		}
	}
}
