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
        private bool _authentication_added = false;

        public WebApiClient(string baseUrl, bool bypassSsl = false, string api_access_token = "")
        {
            var options = new RestClientOptions(baseUrl);
            if (bypassSsl)
            {
                options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            }
            restClient = new RestClient(options);
            restClient.AddDefaultHeader("content-type", "application/json");
        }

        public void AquireAccessToken(string client_id, string client_secret)
        {
            if (!_authentication_added)
            {
                var request = new RestRequest();
                request.AddParameter("client_id", client_id);
                request.AddParameter("client_secret", client_secret);
                request.AddParameter("audience", "https://machine.sil.org");
                request.AddParameter("grant_type", "client_credentials");
                var auth0client = new RestClient(
                    new RestClientOptions("https://sil-appbuilder.auth0.com/oauth/token")
                    {
                        Timeout = 3000,
                        ThrowOnAnyError = true
                    }
                );
                auth0client.AddDefaultHeader("content-type", "application/x-www-form-urlencoded");
                var response = auth0client.ExecutePostAsync(request).Result;
                if (response.Content is null)
                    throw new HttpException("Error getting auth0 Authentication.");
                else
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
                    restClient.AddDefaultHeader("authorization", $"Bearer {dict["access_token"]}");
                    _authentication_added = true;
                }
            }
        }

        public async Task<List<TranslationEngineDto>> GetEnginesAsync()
        {
            var request = new RestRequest($"translation-engines");
            var response = await restClient.ExecuteGetAsync(request);
            ThrowResponseIfNotSucessful(response, "Error getting project list.");
            return JsonConvert.DeserializeObject<List<TranslationEngineDto>>(response.Content, SerializerSettings);
        }

        public async Task<TranslationEngineDto> GetEngineAsync(string engineId)
        {
            var request = new RestRequest($"translation-engines/{engineId}");
            var response = await restClient.ExecuteGetAsync(request);
            ThrowResponseIfNotSucessful(response, "Error getting project.");
            return JsonConvert.DeserializeObject<TranslationEngineDto>(response.Content, SerializerSettings);
        }

        public async Task<TranslationEngineDto> PostEngineAsync(TranslationEngineConfigDto engineConfig)
        {
            var request = new RestRequest($"translation-engines");
            request.AddStringBody(JsonConvert.SerializeObject(engineConfig, SerializerSettings),dataFormat: DataFormat.Json);
            var response = await restClient.ExecutePostAsync(request);
            ThrowResponseIfNotSucessful(response, "Error creating project.");
            return JsonConvert.DeserializeObject<TranslationEngineDto>(response.Content, SerializerSettings);
        }

        public async Task DeleteEngineAsync(string id)
        {
            var request = new RestRequest($"translation-engines/{id}");
            // will throw error if unsuccessful
            await restClient.DeleteAsync(request);
        }

        public async Task<WordGraph> GetWordGraph(string engineId, IReadOnlyList<string> sourceSegment)
        {
            var request = new RestRequest($"translation-engines/{engineId}/get-word-graph");
            var response = await restClient.ExecuteGetAsync(request);
            ThrowResponseIfNotSucessful(response, "Error getting word graph.");
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
            var response = await restClient.ExecutePostAsync(request);
            ThrowResponseIfNotSucessful(response, "Error calling train-segment action.");
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
            var response = await restClient.ExecutePostAsync(request);
            ThrowResponseIfNotSucessful(response, "Error building engine.");
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
                var request = new RestRequest(
                    $"translation/engines/{engineId}/{buildRelativeUrl}?minRevision={minRevision}"
                );
                var response = await restClient.ExecuteGetAsync(request, ct);
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
                else if (
                    response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.NoContent
                )
                {
                    break;
                }
                else
                {
                    ThrowResponseIfNotSucessful(response, "Error getting build status.");
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

        private void ThrowResponseIfNotSucessful(RestResponse response, string message)
        {
            if (!response.IsSuccessful)
            {
                var added_message = "";
                if (response.ErrorException != null)
                {
                    added_message = $"\nError Message: {response.ErrorMessage ?? ""}\n{LogResponse(response)}";
                }
                throw new HttpException(message + added_message) { StatusCode = (int)response.StatusCode, };
            }
        }

        private string LogResponse(RestResponse response)
        {
            var request = response.Request;
            var requestToLog = new
            {
                resource = request.Resource,
                parameters = request.Parameters.Select(
                    parameter =>
                        new { name = parameter.Name, value = parameter.Value, type = parameter.Type.ToString() }
                ),
                method = request.Method.ToString(),
                uri = restClient.BuildUri(request),
            };

            var responseToLog = new
            {
                statusCode = response.StatusCode,
                content = response.Content,
                headers = response.Headers,
                responseUri = response.ResponseUri,
                errorMessage = response.ErrorMessage,
            };

            return string.Format(
                "Request: {0}, Response: {1}",
                JsonConvert.SerializeObject(requestToLog, formatting: Formatting.Indented),
                JsonConvert.SerializeObject(responseToLog, formatting: Formatting.Indented)
            );
        }
    }
}
