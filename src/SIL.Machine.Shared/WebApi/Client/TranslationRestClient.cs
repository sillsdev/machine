using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Dtos;

namespace SIL.Machine.WebApi.Client
{
	public class TranslationRestClient
	{
		private readonly JsonSerializerSettings _serializerSettings;

		public TranslationRestClient(string baseUrl, string projectId, IHttpClient httpClient)
		{
			ProjectId = projectId;
			HttpClient = httpClient;
			ErrorCorrectionModel = new ErrorCorrectionModel();
			HttpClient.BaseUrl = baseUrl;
			_serializerSettings = new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()};
		}

		public string ProjectId { get; }
		public string BaseUrl => HttpClient.BaseUrl;
		internal IHttpClient HttpClient { get; }
		internal ErrorCorrectionModel ErrorCorrectionModel { get; }

		public async Task<InteractiveTranslationResult> TranslateInteractivelyAsync(IReadOnlyList<string> sourceSegment)
		{
			string url = string.Format("translation/engines/project:{0}/actions/interactiveTranslate", ProjectId);
			string body = JsonConvert.SerializeObject(sourceSegment, _serializerSettings);
			HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Post, url, body, "application/json");
			if (!response.IsSuccess)
				throw new HttpException("Error calling interactiveTranslate action.") {StatusCode = response.StatusCode};
			var resultDto = JsonConvert.DeserializeObject<InteractiveTranslationResultDto>(response.Content,
				_serializerSettings);
			return CreateModel(resultDto, sourceSegment);
		}

		public async Task TrainSegmentPairAsync(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			string url = string.Format("translation/engines/project:{0}/actions/trainSegment", ProjectId);
			var pairDto = new SegmentPairDto
			{
				SourceSegment = sourceSegment.ToArray(),
				TargetSegment = targetSegment.ToArray()
			};
			string body = JsonConvert.SerializeObject(pairDto, _serializerSettings);
			HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Post, url, body, "application/json");
			if (!response.IsSuccess)
				throw new HttpException("Error calling trainSegment action.") {StatusCode = response.StatusCode};
		}

		public async Task TrainAsync(Action<SmtTrainProgress> progress)
		{
			string url = string.Format("translation/engines/project:{0}", ProjectId);
			HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Get, url);
			if (!response.IsSuccess)
				throw new HttpException("Error getting engine identifier.") {StatusCode = response.StatusCode};
			var engineDto = JsonConvert.DeserializeObject<EngineDto>(response.Content, _serializerSettings);
			BuildDto buildDto = await CreateBuildAsync(engineDto.Id);
			await PollBuildProgressAsync(buildDto, progress);
		}

		private async Task<BuildDto> CreateBuildAsync(string engineId)
		{
			string body = JsonConvert.SerializeObject(engineId, _serializerSettings);
			HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Post, "translation/builds", body,
				"application/json");
			if (!response.IsSuccess)
				throw new HttpException("Error starting build.") {StatusCode = response.StatusCode};
			return JsonConvert.DeserializeObject<BuildDto>(response.Content, _serializerSettings);
		}

		private async Task PollBuildProgressAsync(BuildDto buildDto, Action<SmtTrainProgress> progress)
		{
			while (true)
			{
				progress(new SmtTrainProgress(buildDto.CurrentStep, buildDto.CurrentStepMessage, buildDto.StepCount));

				string url = string.Format("translation/builds/id:{0}?minRevision={1}", buildDto.Id, buildDto.Revision + 1);
				HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Get, url);
				if (response.IsSuccess)
					buildDto = JsonConvert.DeserializeObject<BuildDto>(response.Content, _serializerSettings);
				else if (response.StatusCode == 404)
					break;
				else
					throw new HttpException("Error getting build status.") {StatusCode = response.StatusCode};
			}
		}

		private static InteractiveTranslationResult CreateModel(InteractiveTranslationResultDto resultDto,
			IReadOnlyList<string> sourceSegment)
		{
			return new InteractiveTranslationResult(CreateModel(resultDto.WordGraph),
				CreateModel(resultDto.RuleResult, sourceSegment));
		}

		private static WordGraph CreateModel(WordGraphDto dto)
		{
			var arcs = new List<WordGraphArc>();
			foreach (WordGraphArcDto arcDto in dto.Arcs)
			{
				arcs.Add(new WordGraphArc(arcDto.PrevState, arcDto.NextState, arcDto.Score, arcDto.Words,
					CreateModel(arcDto.Alignment, arcDto.SourceEndIndex - arcDto.SourceStartIndex + 1, arcDto.Words.Length),
					arcDto.Confidences.Cast<double>(), arcDto.SourceStartIndex, arcDto.SourceEndIndex, arcDto.IsUnknown));
			}

			return new WordGraph(arcs, dto.FinalStates, dto.InitialStateScore);
		}

		private static TranslationResult CreateModel(TranslationResultDto dto, IReadOnlyList<string> sourceSegment)
		{
			if (dto == null)
				return null;

			return new TranslationResult(sourceSegment, dto.Target, dto.Confidences.Cast<double>(), dto.Sources,
				CreateModel(dto.Alignment, sourceSegment.Count, dto.Target.Length));
		}

		private static WordAlignmentMatrix CreateModel(AlignedWordPairDto[] dto, int i, int j)
		{
			var alignment = new WordAlignmentMatrix(i, j);
			foreach (AlignedWordPairDto wordPairDto in dto)
				alignment[wordPairDto.SourceIndex, wordPairDto.TargetIndex] = AlignmentType.Aligned;
			return alignment;
		}
	}
}
