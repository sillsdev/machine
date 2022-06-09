namespace SIL.Machine.WebApi.Services;

public class ClearMLService : IClearMLService
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ClearMLOptions> _options;
    private static readonly JsonNamingPolicy JsonNamingPolicy = new SnakeCaseJsonNamingPolicy();
    private static readonly JsonSerializerOptions JsonSerializerOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy,
            Converters = { new CustomEnumConverterFactory(JsonNamingPolicy) }
        };

    public ClearMLService(HttpClient httpClient, IOptionsMonitor<ClearMLOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<string?> GetProjectIdAsync(string name, CancellationToken cancellationToken = default)
    {
        var body = new JsonObject { ["name"] = $"Machine/{name}", ["only_fields"] = new JsonArray("id") };
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
        var body = new JsonObject { ["name"] = $"Machine/{name}" };
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
        var body = new JsonObject { ["project"] = id, ["delete_contents"] = true };
        JsonObject? result = await CallAsync("projects", "delete", body, cancellationToken);
        var deleted = (int?)result?["data"]?["deleted"];
        if (deleted is null)
            throw new InvalidOperationException("Malformed response from ClearML server.");
        return deleted == 1;
    }

    public async Task<string> CreateTaskAsync(
        string name,
        string projectId,
        string sourceLanguageTag,
        string targetLanguageTag,
        Uri buildUri,
        CancellationToken cancellationToken = default
    )
    {
        string script =
            "from machine.webapi.clearml_nmt_engine_build_job import run\n"
            + "args = {\n"
            + $"    'src_lang': '{sourceLanguageTag}',\n"
            + $"    'trg_lang': '{targetLanguageTag}',\n"
            + $"    'build_uri': '{buildUri}',\n"
            + $"    'max_step': {_options.CurrentValue.MaxStep}\n"
            + "}\n"
            + "run(args)\n";

        var body = new JsonObject
        {
            ["name"] = name,
            ["project"] = projectId,
            ["script"] = new JsonObject { ["diff"] = script },
            ["container"] = new JsonObject
            {
                ["image"] = "ghcr.io/sillsdev/machine.py:latest",
                ["arguments"] = "--pull always"
            },
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
        return JsonSerializer.Deserialize<ClearMLTask>(tasks[0], JsonSerializerOptions);
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
        var authenticationString = $"{_options.CurrentValue.AccessKey}:{_options.CurrentValue.SecretKey}";
        var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
        request.Headers.Add("Authorization", $"Basic {base64EncodedAuthenticationString}");
        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        string result = await response.Content.ReadAsStringAsync();
        return (JsonObject?)JsonNode.Parse(result);
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
