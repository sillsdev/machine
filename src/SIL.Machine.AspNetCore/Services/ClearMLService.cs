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

    public Task<ClearMLTask?> GetTaskAsync(string name, string projectId, CancellationToken cancellationToken = default)
    {
        return GetTaskAsync(
            new JsonObject
            {
                ["id"] = new JsonArray(),
                ["name"] = name,
                ["project"] = new JsonArray(projectId)
            },
            cancellationToken
        );
    }

    public Task<ClearMLTask?> GetTaskAsync(string id, CancellationToken cancellationToken = default)
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

    private async Task<string?> GetMetricAsync(
        string taskId,
        string metricName,
        string variantName,
        CancellationToken cancellationToken = default
    )
    {
        var body = new JsonObject { ["id"] = taskId };
        JsonObject? result = await CallAsync("tasks", "get_by_id_ex", body, cancellationToken);
        var tasks = (JsonArray?)result?["data"]?["tasks"];
        if (tasks is null || tasks.Count == 0)
            return null;
        JsonObject task = (JsonObject)tasks[0]!;
        string metricNameHash,
            variantNameHash;
        using (var md5 = MD5.Create())
        {
            metricNameHash = Convert.ToHexString(md5.ComputeHash(Encoding.ASCII.GetBytes(metricName))).ToLower();
            variantNameHash = Convert.ToHexString(md5.ComputeHash(Encoding.ASCII.GetBytes(variantName))).ToLower();
        }
        return (string?)task?["last_metrics"]?[metricNameHash]?[variantNameHash];
    }

    public async Task<float> GetInferencePercentCompleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return float.Parse(
            await GetMetricAsync(id, "inference_percent_complete", "inference_percent_complete") ?? "0.0"
        );
    }

    public async Task<IReadOnlyList<string>?> GetTasksAheadInQueueAsync(
        string taskId,
        CancellationToken cancellationToken = default
    )
    {
        ClearMLTask? task = await GetTaskAsync(taskId, cancellationToken);
        if (task is null)
            return null;
        JsonObject? result = await CallAsync(
            "queues",
            "get_all_ex",
            // Uses python regex syntax to only match exact queue name. See https://clear.ml/docs/latest/docs/references/api/queues#post-queuesget_all
            new JsonObject { ["name"] = $"^{_options.CurrentValue.Queue}$" },
            cancellationToken
        );
        JsonNode? queuesNode = result?["data"]?["queues"];
        if (queuesNode is null)
            return null;
        JsonArray queues = (JsonArray)queuesNode;
        JsonNode? target_queue = queues[0];
        if (target_queue is null)
            return null;
        JsonNode? entriesNode = target_queue["entries"];
        if (entriesNode is null)
            return null;
        JsonArray entries = (JsonArray)entriesNode;
        List<string> tasksAheadInQueue = new();
        foreach (JsonNode? entry in entries)
        {
            JsonNode? task_node = entry?["task"];
            if (task_node is null)
                return null;
            string? id = (string?)task_node["id"];
            string? name = (string?)task_node["name"];
            if (id == taskId)
                break;
            if (name is not null)
                tasksAheadInQueue.Add(name);
        }
        return tasksAheadInQueue;
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
        JsonObject? result = await CallAsync("tasks", "get_by_id_ex", body, cancellationToken);
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
