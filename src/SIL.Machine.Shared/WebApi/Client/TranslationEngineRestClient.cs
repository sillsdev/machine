using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Dtos;

namespace SIL.Machine.WebApi.Client
{
	public class TranslationEngineRestClient : RestClientBase
	{
		public TranslationEngineRestClient(string baseUrl, IHttpClient httpClient)
			: base(baseUrl, httpClient)
		{
		}

		public async Task<InteractiveTranslationResult> TranslateInteractivelyAsync(string engineId,
			IReadOnlyList<string> sourceSegment)
		{
			string url = $"translation/engines/id:{engineId}/actions/interactiveTranslate";
			string body = JsonConvert.SerializeObject(sourceSegment, SerializerSettings);
			HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Post, url, body, "application/json");
			if (!response.IsSuccess)
			{
				throw new HttpException("Error calling interactiveTranslate action.")
				{
					StatusCode = response.StatusCode
				};
			}
			var resultDto = JsonConvert.DeserializeObject<InteractiveTranslationResultDto>(response.Content,
				SerializerSettings);
			return CreateModel(resultDto, sourceSegment);
		}

		public async Task TrainSegmentPairAsync(string engineId, IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			string url = $"translation/engines/id:{engineId}/actions/trainSegment";
			var pairDto = new SegmentPairDto
			{
				SourceSegment = sourceSegment.ToArray(),
				TargetSegment = targetSegment.ToArray()
			};
			string body = JsonConvert.SerializeObject(pairDto, SerializerSettings);
			HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Post, url, body, "application/json");
			if (!response.IsSuccess)
				throw new HttpException("Error calling trainSegment action.") { StatusCode = response.StatusCode };
		}

		public async Task TrainAsync(string engineId, Action<SmtTrainProgress> progress)
		{
			BuildDto buildDto = await CreateBuildAsync(engineId);
			await PollBuildProgressAsync(buildDto, progress);
		}

		public async Task ListenForTrainingStatus(string engineId, Action<SmtTrainProgress> progress)
		{
			string url = $"translation/builds/engine:{engineId}?waitNew=true";
			HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Get, url);
			if (!response.IsSuccess)
				throw new HttpException("Error getting build.") { StatusCode = response.StatusCode };
			BuildDto buildDto = JsonConvert.DeserializeObject<BuildDto>(response.Content, SerializerSettings);
			await PollBuildProgressAsync(buildDto, progress);
		}

		private async Task<BuildDto> CreateBuildAsync(string engineId)
		{
			string body = JsonConvert.SerializeObject(engineId, SerializerSettings);
			HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Post, "translation/builds", body,
				"application/json");
			if (!response.IsSuccess)
				throw new HttpException("Error starting build.") { StatusCode = response.StatusCode };
			return JsonConvert.DeserializeObject<BuildDto>(response.Content, SerializerSettings);
		}

		private async Task PollBuildProgressAsync(BuildDto buildDto, Action<SmtTrainProgress> progress)
		{
			while (true)
			{
				progress(new SmtTrainProgress(buildDto.CurrentStep, buildDto.CurrentStepMessage, buildDto.StepCount));

				string url = $"translation/builds/id:{buildDto.Id}?minRevision={buildDto.Revision + 1}";
				HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Get, url);
				if (response.IsSuccess)
					buildDto = JsonConvert.DeserializeObject<BuildDto>(response.Content, SerializerSettings);
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
