using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using Serval.Core;

namespace Serval.Client
{
    public class WebApiClient
    {
        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private RestClient RestClient { get; }
        private bool _authentication_added = false;

        public WebApiClient(string baseUrl, bool bypassSsl = false, string api_access_token = "")
        {
            var options = new RestClientOptions(baseUrl);
            if (bypassSsl)
            {
                options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            }
            RestClient = new RestClient(options);
            RestClient.AddDefaultHeader("content-type", "application/json");
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
                        MaxTimeout = 3000,
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
                    RestClient.AddDefaultHeader("authorization", $"Bearer {dict["access_token"]}");
                    _authentication_added = true;
                }
            }
        }

        public async Task<IEnumerable<TranslationEngineDto>> GetAllTranslationEnginesAsync()
        {
            var request = new RestRequest($"translation-engines");
            var response = await RestClient.ExecuteGetAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, "Error getting project list.");
            return JsonConvert.DeserializeObject<IEnumerable<TranslationEngineDto>>(
                response.Content,
                SerializerSettings
            );
        }

        public async Task<TranslationEngineDto> CreateTranslationEngineAsync(
            string name,
            string sourceLanguageTag,
            string targetLanguageTag,
            string type
        )
        {
            var request = new RestRequest($"translation-engines");
            var engineConfig = new TranslationEngineConfigDto()
            {
                Name = name,
                SourceLanguageTag = sourceLanguageTag,
                TargetLanguageTag = targetLanguageTag,
                Type = type
            };

            request.AddStringBody(
                JsonConvert.SerializeObject(engineConfig, SerializerSettings),
                dataFormat: DataFormat.Json
            );
            var response = await RestClient.ExecutePostAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, "Error creating project.");
            return JsonConvert.DeserializeObject<TranslationEngineDto>(response.Content, SerializerSettings);
        }

        public async Task<TranslationEngineDto> GetTranslationEngineAsync(string engineId)
        {
            var request = new RestRequest($"translation-engines/{engineId}");
            var response = await RestClient.ExecuteGetAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, $"Error getting project {engineId}.");
            return JsonConvert.DeserializeObject<TranslationEngineDto>(response.Content, SerializerSettings);
        }

        public async Task DeleteTranslationEngineAsync(string engineId)
        {
            var request = new RestRequest($"translation-engines/{engineId}");
            // will throw error if unsuccessful
            await RestClient.DeleteAsync(request).ConfigureAwait(false);
        }

        public async Task<TranslationResultDto> TranslateSegmentAsync(string engineId, string sourceSegment)
        {
            var sourceSegmentArray = sourceSegment.ToArray();
            var request = new RestRequest($"translation-engines/{engineId}/translate");
            request.AddStringBody(
                JsonConvert.SerializeObject(sourceSegmentArray, SerializerSettings),
                dataFormat: DataFormat.Json
            );
            var response = await RestClient.ExecutePostAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, $"Error translating on engine {engineId}.");
            return JsonConvert.DeserializeObject<TranslationResultDto>(response.Content, SerializerSettings);
        }

        public async Task<IEnumerable<TranslationResultDto>> TranslateSegmentAsync(
            string engineId,
            string sourceSegment,
            int n
        )
        {
            var sourceSegmentArray = sourceSegment.ToArray();
            var request = new RestRequest($"translation-engines/{engineId}/translate/{n}");
            request.AddStringBody(
                JsonConvert.SerializeObject(sourceSegmentArray, SerializerSettings),
                dataFormat: DataFormat.Json
            );
            var response = await RestClient.ExecutePostAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, $"Error translating on engine {engineId} for {n} results.");
            return JsonConvert.DeserializeObject<IEnumerable<TranslationResultDto>>(
                response.Content,
                SerializerSettings
            );
        }

        public async Task<WordGraphDto> GetWordGraph(string engineId, string sourceSegment)
        {
            var sourceSegmentArray = sourceSegment.ToArray();
            var request = new RestRequest($"translation-engines/{engineId}/get-word-graph");
            request.AddStringBody(
                JsonConvert.SerializeObject(sourceSegmentArray, SerializerSettings),
                dataFormat: DataFormat.Json
            );
            var response = await RestClient.ExecuteGetAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, "Error getting word graph.");
            return JsonConvert.DeserializeObject<WordGraphDto>(response.Content, SerializerSettings);
        }

        public async Task TrainSegmentPairAsync(string engineId, string sourceSegment, string targetSegment)
        {
            var request = new RestRequest($"translation-engines/{engineId}/train-segment");
            var pairDto = new SegmentPairDto
            {
                SourceSegment = sourceSegment,
                TargetSegment = targetSegment,
                SentenceStart = true
            };
            request.AddStringBody(
                JsonConvert.SerializeObject(pairDto, SerializerSettings),
                dataFormat: DataFormat.Json
            );
            var response = await RestClient.ExecutePostAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, "Error calling train-segment action.");
        }

        public async Task<TranslationEngineCorpusDto> LinkCorpusToTranslationEngineAsync(
            string engineId,
            string corpusId,
            bool pretranslateCorpus = false
        )
        {
            var engineCorpusConfig = new TranslationEngineCorpusConfigDto()
            {
                CorpusId = corpusId,
                Pretranslate = pretranslateCorpus
            };
            var request = new RestRequest($"translation-engines/{engineId}/corpora");
            request.AddStringBody(
                JsonConvert.SerializeObject(engineCorpusConfig, SerializerSettings),
                dataFormat: DataFormat.Json
            );
            var response = await RestClient.ExecutePostAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(
                response,
                $"Error adding corpora {engineCorpusConfig.CorpusId} to engine {engineId}."
            );
            return JsonConvert.DeserializeObject<TranslationEngineCorpusDto>(response.Content, SerializerSettings);
        }

        public async Task<IEnumerable<TranslationEngineCorpusDto>> GetAllTranslationEngineCorporaAsync(string engineId)
        {
            var request = new RestRequest($"translation-engines/{engineId}/corpora");
            var response = await RestClient.ExecuteGetAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, $"Error getting corpora list from project {engineId}.");
            return JsonConvert.DeserializeObject<IEnumerable<TranslationEngineCorpusDto>>(
                response.Content,
                SerializerSettings
            );
        }

        public async Task<TranslationEngineCorpusDto> GetTranslationEngineCorpusAsync(string engineId, string corpusId)
        {
            var request = new RestRequest($"translation-engines/{engineId}/corpora/{corpusId}");
            var response = await RestClient.ExecuteGetAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, $"Error getting corpus {corpusId} from project {engineId}.");
            return JsonConvert.DeserializeObject<TranslationEngineCorpusDto>(response.Content, SerializerSettings);
        }

        public async Task DeleteCorpusFromEngineAsync(string engineId, string corpusId)
        {
            var request = new RestRequest($"translation-engines/{engineId}/corpora/{corpusId}");
            // will throw error if unsuccessful
            await RestClient.DeleteAsync(request).ConfigureAwait(false);
        }

        public async Task<IEnumerable<PretranslationDto>> GetAllPretranslationsAsync(string engineId, string corpusId)
        {
            var request = new RestRequest($"translation-engines/{engineId}/corpora/{corpusId}/pretranslations");
            var response = await RestClient.ExecuteGetAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(
                response,
                $"Error getting pretranslation list from corpora {corpusId} from project {engineId}."
            );
            return JsonConvert.DeserializeObject<IEnumerable<PretranslationDto>>(response.Content, SerializerSettings);
        }

        public async Task<PretranslationDto> GetPretranslationAsync(
            string engineId,
            string corpusId,
            string pretranslationId
        )
        {
            var request = new RestRequest(
                $"translation-engines/{engineId}/corpora/{corpusId}/pretranslations/{pretranslationId}"
            );
            var response = await RestClient.ExecuteGetAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(
                response,
                $"Error getting pretranslation {pretranslationId} from corpora {corpusId} from project {engineId}."
            );
            return JsonConvert.DeserializeObject<PretranslationDto>(response.Content, SerializerSettings);
        }

        public async Task<IEnumerable<BuildDto>> GetAllBuildsAsync(string engineId)
        {
            var request = new RestRequest($"translation-engines/{engineId}/builds");
            var response = await RestClient.ExecuteGetAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, $"Error getting builds for engine {engineId}.");
            return JsonConvert.DeserializeObject<IEnumerable<BuildDto>>(response.Content, SerializerSettings);
        }

        public async Task<BuildDto> StartBuildAsync(string engineId)
        {
            var request = new RestRequest($"translation-engines/{engineId}/builds");
            var response = await RestClient.ExecutePostAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, $"Error building engine {engineId}.");
            return JsonConvert.DeserializeObject<BuildDto>(response.Content, SerializerSettings);
        }

        public async Task<BuildDto> GetBuildAsync(string engineId, string buildId, int minRevision = 0)
        {
            var request = new RestRequest($"translation-engines/{engineId}/builds{buildId}?minRevision={minRevision}");
            var response = await RestClient.ExecuteGetAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, $"Error getting build {buildId} for engine {engineId}.");
            return JsonConvert.DeserializeObject<BuildDto>(response.Content, SerializerSettings);
        }

        public async Task<BuildDto> GetCurrentBuildAsync(string engineId, int minRevision = 0)
        {
            var request = new RestRequest($"translation-engines/{engineId}/current-build?minRevision={minRevision}");
            var response = await RestClient.ExecuteGetAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, $"Error getting current build for engine {engineId}.");
            return JsonConvert.DeserializeObject<BuildDto>(response.Content, SerializerSettings);
        }

        public async Task CancelCurrentBuildAsync(string engineId)
        {
            var request = new RestRequest($"translation-engines/{engineId}/current-build/cancel");
            var response = await RestClient.ExecutePostAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, $"Error cancelling current build for engine {engineId}.");
        }

        public async Task TrainAsync(
            string engineId,
            Action<BuildDto> updateWithProgress,
            CancellationToken ct = default
        )
        {
            BuildDto buildDto = await StartBuildAsync(engineId).ConfigureAwait(false);
            updateWithProgress(buildDto);
            await PollBuildProgressAsync(
                    engineId,
                    $"builds/{buildDto.Id}",
                    buildDto.Revision + 1,
                    updateWithProgress,
                    ct
                )
                .ConfigureAwait(false);
        }

        public async Task ListenForTrainingStatusAsync(
            string engineId,
            Action<BuildDto> updateWithProgress,
            CancellationToken ct = default
        )
        {
            await PollBuildProgressAsync(engineId, "current-build", 0, updateWithProgress, ct).ConfigureAwait(false);
        }

        private async Task PollBuildProgressAsync(
            string engineId,
            string buildRelativeUrl,
            int minRevision,
            Action<BuildDto> updateWithProgress,
            CancellationToken ct = default
        )
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var request = new RestRequest(
                    $"translation-engines/{engineId}/{buildRelativeUrl}?minRevision={minRevision}"
                );
                var response = await RestClient.ExecuteGetAsync(request, ct).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    BuildDto buildDto = JsonConvert.DeserializeObject<BuildDto>(response.Content, SerializerSettings);
                    updateWithProgress(buildDto);
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

        public async Task<IEnumerable<CorpusDto>> GetAllCorporaAsync()
        {
            var request = new RestRequest($"corpora");
            var response = await RestClient.ExecuteGetAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, "Error getting corpus list.");
            return JsonConvert.DeserializeObject<IEnumerable<CorpusDto>>(response.Content, SerializerSettings);
        }

        public async Task<CorpusDto> CreateCorpusAsync(CorpusConfigDto corpusConfig)
        {
            var request = new RestRequest($"corpora");
            request.AddStringBody(
                JsonConvert.SerializeObject(corpusConfig, SerializerSettings),
                dataFormat: DataFormat.Json
            );
            var response = await RestClient.ExecutePostAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, "Error creating project.");
            return JsonConvert.DeserializeObject<CorpusDto>(response.Content, SerializerSettings);
        }

        public async Task<CorpusDto> GetCorpusAsync(string corpusId)
        {
            var request = new RestRequest($"corpora/{corpusId}");
            var response = await RestClient.ExecuteGetAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, $"Error getting Corpus {corpusId}.");
            return JsonConvert.DeserializeObject<CorpusDto>(response.Content, SerializerSettings);
        }

        public async Task DeleteCorpusAsync(string corpusId)
        {
            var request = new RestRequest($"corpora/{corpusId}");
            // will throw error if unsuccessful
            await RestClient.DeleteAsync(request).ConfigureAwait(false);
        }

        public async Task<IEnumerable<DataFileDto>> GetAllDataFilesAsync(string corpusId)
        {
            var request = new RestRequest($"corpora/{corpusId}/files");
            var response = await RestClient.ExecuteGetAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, $"Error getting corpus {corpusId} file list.");
            return JsonConvert.DeserializeObject<IEnumerable<DataFileDto>>(response.Content, SerializerSettings);
        }

        public async Task<DataFileDto> UploadDataFileAsync(
            string corpusId,
            string languageTag,
            string textId,
            string filePath
        )
        {
            var request = new RestRequest($"corpora/{corpusId}/files");
            request.AddParameter(name: "languageTag", value: languageTag);
            request.AddParameter(name: "textId", value: textId);
            request.AddFile(name: "file", path: filePath, contentType: "text/plain");
            var response = await RestClient.ExecutePostAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, $"Error posting {filePath} to {corpusId}.");
            return JsonConvert.DeserializeObject<DataFileDto>(response.Content, SerializerSettings);
        }

        public async Task<DataFileDto> GetDataFileAsync(string corpusId, string fileId)
        {
            var request = new RestRequest($"corpora/{corpusId}/files/{fileId}");
            var response = await RestClient.ExecuteGetAsync(request).ConfigureAwait(false);
            ThrowResponseIfNotSucessful(response, $"Error getting file {fileId} from {corpusId}.");
            return JsonConvert.DeserializeObject<DataFileDto>(response.Content, SerializerSettings);
        }

        public async Task DeleteDataFileAsync(string corpusId, string fileId)
        {
            var request = new RestRequest($"corpora/{corpusId}/files/{fileId}");
            // will throw error if unsuccessful
            await RestClient.DeleteAsync(request).ConfigureAwait(false);
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
                uri = RestClient.BuildUri(request),
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
