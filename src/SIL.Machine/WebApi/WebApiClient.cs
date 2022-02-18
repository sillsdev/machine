using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SIL.Machine.Annotations;
using SIL.Machine.Translation;
using SIL.Machine.Utils;

namespace SIL.Machine.WebApi.Client
{
	public class WebApiClient
	{
		public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
		{
			ContractResolver = new CamelCasePropertyNamesContractResolver()
		};

		public WebApiClient(string baseUrl, IHttpClient httpClient)
		{
			HttpClient = httpClient;
			if (!baseUrl.EndsWith("/"))
				baseUrl += "/";
			HttpClient.BaseUrl = baseUrl;
		}

		public string BaseUrl => HttpClient.BaseUrl;
		public IHttpClient HttpClient { get; }

		public async Task<EngineDto> GetEngineAsync(string engineId)
		{
			string url = $"translation/engines/{engineId}";
			HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Get, url, null, null);
			if (!response.IsSuccess)
				throw new HttpException("Error getting project.") { StatusCode = response.StatusCode };
			return JsonConvert.DeserializeObject<EngineDto>(response.Content, SerializerSettings);
		}

		public async Task<WordGraph> GetWordGraph(string engineId, IReadOnlyList<string> sourceSegment)
		{
			string url = $"translation/engines/{engineId}/actions/getWordGraph";
			string body = JsonConvert.SerializeObject(sourceSegment, SerializerSettings);
			HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Post, url, body, "application/json");
			if (!response.IsSuccess)
			{
				throw new HttpException("Error calling getWordGraph action.")
				{
					StatusCode = response.StatusCode
				};
			}
			var resultDto = JsonConvert.DeserializeObject<WordGraphDto>(response.Content,
				SerializerSettings);
			return CreateModel(resultDto);
		}

		public async Task TrainSegmentPairAsync(string engineId, IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			string url = $"translation/engines/{engineId}/actions/trainSegment";
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

		public async Task StartTrainingAsync(string engineId)
		{
			await CreateBuildAsync(engineId);
		}

		public async Task TrainAsync(string engineId, Action<ProgressStatus> progress, CancellationToken ct = default)
		{
			BuildDto buildDto = await CreateBuildAsync(engineId);
			progress(CreateProgressStatus(buildDto));
			await PollBuildProgressAsync("id", buildDto.Id, buildDto.Revision + 1, progress, ct);
		}

		public async Task ListenForTrainingStatusAsync(string engineId, Action<ProgressStatus> progress,
			CancellationToken ct = default(CancellationToken))
		{
			await PollBuildProgressAsync("engine", engineId, 0, progress, ct);
		}

		private async Task<BuildDto> CreateBuildAsync(string engineId)
		{
			string body = JsonConvert.SerializeObject(engineId, SerializerSettings);
			HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Post, "translation/builds", body,
				"application/json", CancellationToken.None);
			if (!response.IsSuccess)
				throw new HttpException("Error starting build.") { StatusCode = response.StatusCode };
			return JsonConvert.DeserializeObject<BuildDto>(response.Content, SerializerSettings);
		}

		private async Task PollBuildProgressAsync(string locatorType, string locator, int minRevision,
			Action<ProgressStatus> progress, CancellationToken ct)
		{
			while (true)
			{
				ct.ThrowIfCancellationRequested();

				string url = $"translation/builds/{locatorType}:{locator}?minRevision={minRevision}";
				HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Get, url, null, null, ct);
				if (response.StatusCode == 200)
				{
					BuildDto buildDto = JsonConvert.DeserializeObject<BuildDto>(response.Content, SerializerSettings);
					progress(CreateProgressStatus(buildDto));
					locatorType = "id";
					locator = buildDto.Id;
					if (buildDto.State == BuildStates.Completed || buildDto.State == BuildStates.Canceled)
						break;
					else if (buildDto.State == BuildStates.Faulted)
						throw new InvalidOperationException("Error occurred during build: " + buildDto.Message);
					minRevision = buildDto.Revision + 1;
				}
				else if (response.StatusCode == 204)
				{
					continue;
				}
				else if (response.StatusCode == 404)
				{
					break;
				}
				else
				{
					throw new HttpException("Error getting build status.") { StatusCode = response.StatusCode };
				}
			}
		}

		private static ProgressStatus CreateProgressStatus(BuildDto buildDto)
		{
			return new ProgressStatus(buildDto.PercentCompleted, buildDto.Message);
		}

		private static WordGraph CreateModel(WordGraphDto dto)
		{
			var arcs = new List<WordGraphArc>();
			foreach (WordGraphArcDto arcDto in dto.Arcs)
			{
				WordAlignmentMatrix alignment = CreateModel(arcDto.Alignment,
					arcDto.SourceSegmentRange.End - arcDto.SourceSegmentRange.Start, arcDto.Words.Length);
				arcs.Add(new WordGraphArc(arcDto.PrevState, arcDto.NextState, arcDto.Score, arcDto.Words, alignment,
					CreateModel(arcDto.SourceSegmentRange), arcDto.Sources, arcDto.Confidences.Cast<double>()));
			}

			return new WordGraph(arcs, dto.FinalStates, dto.InitialStateScore);
		}

		private static TranslationResult CreateModel(TranslationResultDto dto, IReadOnlyList<string> sourceSegment)
		{
			if (dto == null)
				return null;

			return new TranslationResult(sourceSegment, dto.Target, dto.Confidences.Cast<double>(), dto.Sources,
				CreateModel(dto.Alignment, sourceSegment.Count, dto.Target.Length), dto.Phrases.Select(CreateModel));
		}

		private static WordAlignmentMatrix CreateModel(AlignedWordPairDto[] dto, int i, int j)
		{
			var alignment = new WordAlignmentMatrix(i, j);
			foreach (AlignedWordPairDto wordPairDto in dto)
				alignment[wordPairDto.SourceIndex, wordPairDto.TargetIndex] = true;
			return alignment;
		}

		private static Phrase CreateModel(PhraseDto dto)
		{
			return new Phrase(CreateModel(dto.SourceSegmentRange), dto.TargetSegmentCut, dto.Confidence);
		}

		private static Range<int> CreateModel(RangeDto dto)
		{
			return Range<int>.Create(dto.Start, dto.End);
		}
	}
}
