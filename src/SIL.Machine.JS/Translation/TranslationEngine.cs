using System;
using System.Collections.Generic;
using Bridge.Html5;

namespace SIL.Machine.Translation
{
	public class TranslationEngine
	{
		public TranslationEngine(string baseUrl, string sourceLanguageTag, string targetLanguageTag)
		{
			BaseUrl = baseUrl;
			SourceLanguageTag = sourceLanguageTag;
			TargetLanguageTag = targetLanguageTag;
			ErrorCorrectingModel = new ErrorCorrectingModel();
			ConfidenceThreshold = 0.2;
		}

		public string SourceLanguageTag { get; }
		public string TargetLanguageTag { get; }
		public string BaseUrl { get; }
		public double ConfidenceThreshold { get; set; }
		internal ErrorCorrectingModel ErrorCorrectingModel { get; }

		public void GetSuggester(string[] sourceSegment, Action<InteractiveTranslationSuggester> onFinished)
		{
			var request = new XMLHttpRequest();
			request.OnReadyStateChange = () =>
			{
				if (request.ReadyState != AjaxReadyState.Done)
					return;

				if (request.Status == 200 || request.Status == 304)
				{
					onFinished(CreateSuggester(sourceSegment, JSON.Parse(request.ResponseText)));
				}
				else
				{
					onFinished(null);
				}
			};

			string url = string.Format("{0}/translation/engines/{1}/{2}/actions/interactive-translate", BaseUrl, SourceLanguageTag, TargetLanguageTag);
			request.Open("POST", url);
			string body = JSON.Stringify(sourceSegment);
			request.SetRequestHeader("Content-type", "application/json");
			request.Send(body);
		}

		private InteractiveTranslationSuggester CreateSuggester(string[] sourceSegment, dynamic json)
		{
			WordGraph wordGraph = ParseWordGraph(json["wordGraph"]);
			TranslationResult transferResult = ParseRuleResult(sourceSegment, json["ruleResult"]);
			return new InteractiveTranslationSuggester(this, wordGraph, transferResult, sourceSegment);
		}

		private WordGraph ParseWordGraph(dynamic jsonWordGraph)
		{
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

		private TranslationResult ParseRuleResult(string[] sourceSegment, dynamic jsonResult)
		{
			var jsonTarget = jsonResult["target"];
			var targetSegment = new List<string>();
			foreach (var jsonWord in jsonTarget)
				targetSegment.Add(jsonWord);

			var jsonConfidences = jsonResult["confidences"];
			var confidences = new List<double>();
			foreach (var jsonConfidence in jsonConfidences)
				confidences.Add(jsonConfidence);

			var jsonAlignment = jsonResult["alignment"];
			var alignment = new AlignedWordPair[sourceSegment.Length, targetSegment.Count];
			foreach (var jsonAligned in jsonAlignment)
			{
				int i = jsonAligned["sourceIndex"];
				int j = jsonAligned["targetIndex"];
				var sources = (TranslationSources) jsonAligned["sources"];
				alignment[i, j] = new AlignedWordPair(i, j, sources);
			}

			return new TranslationResult(sourceSegment, targetSegment, confidences, alignment);
		}
	}
}
