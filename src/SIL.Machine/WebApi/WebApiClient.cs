using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using RestSharp.Authenticators;
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

        private RestClient restClient { get; }

        public WebApiClient(string baseUrl, bool bypassSsl = false, string api_access_token = "")
        {
            var options = new RestClientOptions(baseUrl);
            if (bypassSsl) {
                options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            }
            restClient = new RestClient(options);
            if (api_access_token != "")
            {
                restClient.AddDefaultHeaders(new Dictionary<string, string>
                {
                    ["content-type"] = "application/json",
                    ["authorization"] = $"Bearer {api_access_token}"
                });
            }
            else
            {
                restClient.AddDefaultHeader("content-type","application/json");
            }
        }

        public async Task<List<TranslationEngineDto>> GetEnginesAsync()
        {
            var request = new RestRequest($"translation-engines");
            var response = await restClient.GetAsync(request);
            if (!response.IsSuccessful)
                throw new HttpException("Error getting project list.") { StatusCode = (int)response.StatusCode };
            return JsonConvert.DeserializeObject<List<TranslationEngineDto>>(response.Content, SerializerSettings);
        }

        public async Task<TranslationEngineDto> GetEngineAsync(string engineId)
        {
            var request = new RestRequest($"translation-engines/{engineId}");
            var response = await restClient.GetAsync(request);
            if (!response.IsSuccessful)
                throw new HttpException("Error getting project.") { StatusCode = (int)response.StatusCode };
            return JsonConvert.DeserializeObject<TranslationEngineDto>(response.Content, SerializerSettings);
        }

        public async Task<TranslationEngineDto> PostEngineAsync(string name, string sourceLanguageTag, string targetLanguageTag, string type = "SmtTransfer")
        {
            var request = new RestRequest($"translation-engines");
            request.AddJsonBody(JsonConvert.SerializeObject(
                new Dictionary<string,string>
                {
                    ["name"] = name,
                    ["sourceLanguageTag"] = sourceLanguageTag,
                    ["targetLanguageTag"] = targetLanguageTag,
                    ["type"] = type
                }, SerializerSettings));
            var response = await restClient.PostAsync(request);
            if (!response.IsSuccessful)
                throw new HttpException("Error getting project.") { StatusCode = (int)response.StatusCode };
            return JsonConvert.DeserializeObject<TranslationEngineDto>(response.Content, SerializerSettings);
        }

        public async Task<WordGraph> GetWordGraph(string engineId, IReadOnlyList<string> sourceSegment)
        {
            var request = new RestRequest($"translation-engines/{engineId}/get-word-graph");
            var response = await restClient.GetAsync(request);
            if (!response.IsSuccessful)
                throw new HttpException("Error getting project.") { StatusCode = (int)response.StatusCode };
            var resultDto = JsonConvert.DeserializeObject<WordGraphDto>(response.Content, SerializerSettings);
            return CreateWordGraph(resultDto);
        }

        public async Task TrainSegmentPairAsync(
            string engineId,
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment
        )
        {
            var request = new RestRequest($"translation-engines/{engineId}/train-segment");
            var pairDto = new SegmentPairDto
            {
                SourceSegment = sourceSegment.ToArray(),
                TargetSegment = targetSegment.ToArray()
            };
            request.AddJsonBody(JsonConvert.SerializeObject(pairDto, SerializerSettings));
            var response = await restClient.PostAsync(request);
            if (!response.IsSuccessful)
                throw new HttpException("Error calling train-segment action.") { StatusCode = (int)response.StatusCode };
        }

        public async Task StartTrainingAsync(string engineId)
        {
            await CreateBuildAsync(engineId);
        }

        public async Task TrainAsync(string engineId, Action<ProgressStatus> progress, CancellationToken ct = default)
        {
            BuildDto buildDto = await CreateBuildAsync(engineId);
            progress(CreateProgressStatus(buildDto));
            await PollBuildProgressAsync(engineId, $"builds/{buildDto.Id}", buildDto.Revision + 1, progress, ct);
        }

        public async Task ListenForTrainingStatusAsync(
            string engineId,
            Action<ProgressStatus> progress,
            CancellationToken ct = default
        )
        {
            await PollBuildProgressAsync(engineId, "current-build", 0, progress, ct);
        }

        private async Task<BuildDto> CreateBuildAsync(string engineId)
        {
            var request = new RestRequest($"translation-engines/{engineId}/builds");
            var response = await restClient.PostAsync(request);
            if (!response.IsSuccessful)
                throw new HttpException("Error getting project.") { StatusCode = (int)response.StatusCode };
            return JsonConvert.DeserializeObject<BuildDto>(response.Content, SerializerSettings);
        }

        private async Task PollBuildProgressAsync(
            string engineId,
            string buildRelativeUrl,
            int minRevision,
            Action<ProgressStatus> progress,
            CancellationToken ct
        )
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var request = new RestRequest($"translation/engines/{engineId}/{buildRelativeUrl}?minRevision={minRevision}");
                var response = await restClient.GetAsync(request,ct);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    BuildDto buildDto = JsonConvert.DeserializeObject<BuildDto>(response.Content, SerializerSettings);
                    progress(CreateProgressStatus(buildDto));
                    buildRelativeUrl = $"builds/{buildDto.Id}";
                    if (buildDto.State == BuildState.Completed || buildDto.State == BuildState.Canceled)
                        break;
                    else if (buildDto.State == BuildState.Faulted)
                        throw new InvalidOperationException("Error occurred during build: " + buildDto.Message);
                    minRevision = buildDto.Revision + 1;
                }
                else if (response.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    continue;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.NoContent)
                {
                    break;
                }
                else
                {
                    throw new HttpException("Error getting build status.") { StatusCode = (int)response.StatusCode };
                }
            }
        }

        private static ProgressStatus CreateProgressStatus(BuildDto buildDto)
        {
            return new ProgressStatus(buildDto.Step, buildDto.PercentCompleted, buildDto.Message);
        }

        private static WordGraph CreateWordGraph(WordGraphDto dto)
        {
            var arcs = new List<WordGraphArc>();
            foreach (WordGraphArcDto arcDto in dto.Arcs)
            {
                WordAlignmentMatrix alignment = CreateWordAlignmentMatrix(
                    arcDto.Alignment,
                    arcDto.SourceSegmentRange.End - arcDto.SourceSegmentRange.Start,
                    arcDto.Words.Length
                );
                arcs.Add(
                    new WordGraphArc(
                        arcDto.PrevState,
                        arcDto.NextState,
                        arcDto.Score,
                        arcDto.Words,
                        alignment,
                        CreateRange(arcDto.SourceSegmentRange),
                        arcDto.Sources,
                        arcDto.Confidences.Cast<double>()
                    )
                );
            }

            return new WordGraph(arcs, dto.FinalStates, dto.InitialStateScore);
        }

        private static TranslationResult CreateTranslationResult(TranslationResultDto dto, int sourceSegmentLength)
        {
            if (dto == null)
                return null;

            return new TranslationResult(
                sourceSegmentLength,
                dto.Target,
                dto.Confidences.Cast<double>(),
                dto.Sources,
                CreateWordAlignmentMatrix(dto.Alignment, sourceSegmentLength, dto.Target.Length),
                dto.Phrases.Select(CreatePhrase)
            );
        }

        private static WordAlignmentMatrix CreateWordAlignmentMatrix(AlignedWordPairDto[] dto, int i, int j)
        {
            var alignment = new WordAlignmentMatrix(i, j);
            foreach (AlignedWordPairDto wordPairDto in dto)
                alignment[wordPairDto.SourceIndex, wordPairDto.TargetIndex] = true;
            return alignment;
        }

        private static Phrase CreatePhrase(PhraseDto dto)
        {
            return new Phrase(CreateRange(dto.SourceSegmentRange), dto.TargetSegmentCut, dto.Confidence);
        }

        private static Range<int> CreateRange(RangeDto dto)
        {
            return Range<int>.Create(dto.Start, dto.End);
        }
    }
}
