namespace SIL.Machine.AspNetCore.Services;

public class ClearMLService : IClearMLService
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ClearMLNmtEngineOptions> _options;
    private readonly ILogger<ClearMLService> _logger;
    private static readonly JsonNamingPolicy JsonNamingPolicy = new SnakeCaseJsonNamingPolicy();
    private static readonly JsonSerializerOptions JsonSerializerOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy,
            Converters = { new CustomEnumConverterFactory(JsonNamingPolicy) }
        };

    private IClearMLAuthenticationService _clearMLAuthService;

    public ClearMLService(
        HttpClient httpClient,
        IOptionsMonitor<ClearMLNmtEngineOptions> options,
        ILogger<ClearMLService> logger,
        IClearMLAuthenticationService clearMLAuthService
    )
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
        _clearMLAuthService = clearMLAuthService;
        Sldr.Initialize();
    }

    public async Task<string?> GetProjectIdAsync(string name, CancellationToken cancellationToken = default)
    {
        var body = new JsonObject
        {
            ["name"] = $"{_options.CurrentValue.RootProject}/{name}",
            ["only_fields"] = new JsonArray("id")
        };
        JsonObject? result = await CallAsync("projects", "get_all", body, cancellationToken);
        var projects = (JsonArray?)result?["data"]?["projects"];
        if (projects is null)
            throw new InvalidOperationException("Malformed response from ClearML server.");
        if (projects.Count == 0)
            return null;
        return (string?)projects[0]?["id"];
    }

    public async Task<string> CreateProjectAsync(
        string name,
        string? description = null,
        CancellationToken cancellationToken = default
    )
    {
        var body = new JsonObject { ["name"] = $"{_options.CurrentValue.RootProject}/{name}" };
        if (description != null)
            body["description"] = description;
        JsonObject? result = await CallAsync("projects", "create", body, cancellationToken);
        var projectId = (string?)result?["data"]?["id"];
        if (projectId is null)
            throw new InvalidOperationException("Malformed response from ClearML server.");
        return projectId;
    }

    public async Task<bool> DeleteProjectAsync(string id, CancellationToken cancellationToken = default)
    {
        var body = new JsonObject
        {
            ["project"] = id,
            ["delete_contents"] = true,
            ["force"] = true // needed if there are tasks already in that project.
        };
        JsonObject? result = await CallAsync("projects", "delete", body, cancellationToken);
        var deleted = (int?)result?["data"]?["deleted"];
        if (deleted is null)
            throw new InvalidOperationException("Malformed response from ClearML server.");
        return deleted == 1;
    }

    public async Task<string> CreateTaskAsync(
        string buildId,
        string projectId,
        string engineId,
        string sourceLanguageTag,
        string targetLanguageTag,
        string sharedFileUri,
        CancellationToken cancellationToken = default
    )
    {
        string script =
            "from machine.jobs.build_nmt_engine import run\n"
            + "args = {\n"
            + $"    'model_type': '{_options.CurrentValue.ModelType}',\n"
            + $"    'engine_id': '{engineId}',\n"
            + $"    'build_id': '{buildId}',\n"
            + $"    'src_lang': '{ConvertLanguageTag(sourceLanguageTag)}',\n"
            + $"    'trg_lang': '{ConvertLanguageTag(targetLanguageTag)}',\n"
            + $"    'max_steps': {_options.CurrentValue.MaxSteps},\n"
            + $"    'shared_file_uri': '{sharedFileUri}',\n"
            + $"    'clearml': True\n"
            + "}\n"
            + "run(args)\n";

        var body = new JsonObject
        {
            ["name"] = buildId,
            ["project"] = projectId,
            ["script"] = new JsonObject { ["diff"] = script },
            ["container"] = new JsonObject { ["image"] = _options.CurrentValue.DockerImage, },
            ["type"] = "training"
        };
        JsonObject? result = await CallAsync("tasks", "create", body, cancellationToken);
        var taskId = (string?)result?["data"]?["id"];
        if (taskId is null)
            throw new InvalidOperationException("Malformed response from ClearML server.");
        return taskId;
    }

    public async Task<bool> EnqueueTaskAsync(string id, CancellationToken cancellationToken = default)
    {
        var body = new JsonObject { ["task"] = id, ["queue_name"] = _options.CurrentValue.Queue };
        JsonObject? result = await CallAsync("tasks", "enqueue", body, cancellationToken);
        var queued = (int?)result?["data"]?["queued"];
        if (queued is null)
            throw new InvalidOperationException("Malformed response from ClearML server.");
        return queued == 1;
    }

    public async Task<bool> DequeueTaskAsync(string id, CancellationToken cancellationToken = default)
    {
        var body = new JsonObject { ["task"] = id };
        JsonObject? result = await CallAsync("tasks", "dequeue", body, cancellationToken);
        var dequeued = (int?)result?["data"]?["dequeued"];
        if (dequeued is null)
            throw new InvalidOperationException("Malformed response from ClearML server.");
        return dequeued == 1;
    }

    public async Task<bool> StopTaskAsync(string id, CancellationToken cancellationToken = default)
    {
        var body = new JsonObject { ["task"] = id, ["force"] = true };
        JsonObject? result = await CallAsync("tasks", "stop", body, cancellationToken);
        var updated = (int?)result?["data"]?["updated"];
        if (updated is null)
            throw new InvalidOperationException("Malformed response from ClearML server.");
        return updated == 1;
    }

    public Task<ClearMLTask?> GetTaskByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return GetTaskAsync(new JsonObject { ["name"] = name }, cancellationToken);
    }

    public Task<ClearMLTask?> GetTaskByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return GetTaskAsync(new JsonObject { ["id"] = id }, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, double>> GetTaskMetricsAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var body = new JsonObject { ["task"] = id };
        JsonObject? result = await CallAsync("events", "get_task_latest_scalar_values", body, cancellationToken);
        var metrics = (JsonArray?)result?["data"]?["metrics"];
        if (metrics is null)
            throw new InvalidOperationException("Malformed response from ClearML server.");
        var performanceMetrics = (JsonObject?)metrics.FirstOrDefault(m => (string?)m?["name"] == "metrics");
        var results = new Dictionary<string, double>();
        if (performanceMetrics is null)
            return results;
        var variants = (JsonArray?)performanceMetrics?["variants"];
        if (variants is null)
            return results;
        foreach (JsonObject? variant in variants)
        {
            if (variant is null)
                continue;
            var name = (string?)variant?["name"];
            if (name is null)
                continue;
            var value = (double?)variant?["last_value"];
            if (value is null)
                continue;
            results[name] = value.Value;
        }
        return results;
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        JsonObject? result = await CallAsync("debug", "ping", new JsonObject(), cancellationToken);
        return result is not null;
    }

    public async Task<bool> AvailableWorkersExist(CancellationToken cancellationToken = default)
    {
        JsonObject? result = await CallAsync("workers", "get_all", new JsonObject(), cancellationToken);
        JsonNode? workers_node = result?["data"]?["workers"];
        if (workers_node is null)
            return false;
        JsonArray workers = (JsonArray)workers_node;
        foreach (var worker in workers)
        {
            JsonNode? queues_node = worker?["queues"];
            if (queues_node is null)
                return false;
            JsonArray queues = (JsonArray)queues_node;
            foreach (var queue in queues)
            {
                if ((string?)queue?["name"] == "production")
                    return true;
            }
        }
        return false;
    }

    private async Task<ClearMLTask?> GetTaskAsync(JsonObject body, CancellationToken cancellationToken = default)
    {
        body["only_fields"] = new JsonArray(
            "id",
            "name",
            "status",
            "project",
            "last_iteration",
            "status_reason",
            "active_duration"
        );
        JsonObject? result = await CallAsync("tasks", "get_all_ex", body, cancellationToken);
        var tasks = (JsonArray?)result?["data"]?["tasks"];
        if (tasks is null || tasks.Count == 0)
            return null;
        return tasks[0].Deserialize<ClearMLTask>(JsonSerializerOptions);
    }

    private async Task<JsonObject?> CallAsync(
        string service,
        string action,
        JsonNode body,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.CurrentValue.ApiServer}/{service}.{action}")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_clearMLAuthService.GetAuthToken()}");
        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        string result = await response.Content.ReadAsStringAsync(cancellationToken);
        return (JsonObject?)JsonNode.Parse(result);
    }

    private static string ConvertLanguageTag(string languageTag)
    {
        if (
            !IetfLanguageTag.TryGetSubtags(
                languageTag,
                out LanguageSubtag languageSubtag,
                out ScriptSubtag scriptSubtag,
                out _,
                out _
            )
        )
            return languageTag;

        // Convert to NLLB language codes
        return $"{languageSubtag.Iso3Code}_{scriptSubtag.Code}";
    }

    private class SnakeCaseJsonNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString()))
                .ToLowerInvariant();
        }
    }
}
