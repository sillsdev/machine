namespace SIL.Machine.AspNetCore.Services;

public class ClearMLService : IClearMLService
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ClearMLOptions> _options;
    private readonly IHostEnvironment _env;
    private static readonly JsonNamingPolicy JsonNamingPolicy = new SnakeCaseJsonNamingPolicy();
    private static readonly JsonSerializerOptions JsonSerializerOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy) }
        };

    private readonly IClearMLAuthenticationService _clearMLAuthService;

    public ClearMLService(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<ClearMLOptions> options,
        IClearMLAuthenticationService clearMLAuthService,
        IHostEnvironment env
    )
    {
        _httpClient = httpClientFactory.CreateClient("ClearML");
        _options = options;
        _clearMLAuthService = clearMLAuthService;
        _env = env;
    }

    public async Task<string?> GetProjectIdAsync(string name, CancellationToken cancellationToken = default)
    {
        var body = new JsonObject
        {
            ["name"] = $"{_options.CurrentValue.RootProject}/{_options.CurrentValue.Project}/{name}",
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
        var body = new JsonObject
        {
            ["name"] = $"{_options.CurrentValue.RootProject}/{_options.CurrentValue.Project}/{name}"
        };
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
        string script,
        CancellationToken cancellationToken = default
    )
    {
        var snakeCaseEnvironment = JsonNamingPolicy.ConvertName(_env.EnvironmentName);
        var body = new JsonObject
        {
            ["name"] = buildId,
            ["project"] = projectId,
            ["script"] = new JsonObject { ["diff"] = script },
            ["container"] = new JsonObject
            {
                ["image"] = _options.CurrentValue.DockerImage,
                ["arguments"] = "--env ENV_FOR_DYNACONF=" + snakeCaseEnvironment,
            },
            ["type"] = "training"
        };
        JsonObject? result = await CallAsync("tasks", "create", body, cancellationToken);
        var taskId = (string?)result?["data"]?["id"];
        if (taskId is null)
            throw new InvalidOperationException("Malformed response from ClearML server.");
        return taskId;
    }

    public async Task<bool> DeleteTaskAsync(string id, CancellationToken cancellationToken = default)
    {
        var body = new JsonObject { ["task"] = id };
        JsonObject? result = await CallAsync("tasks", "delete", body, cancellationToken);
        var deleted = (bool?)result?["data"]?["deleted"];
        if (deleted is null)
            throw new InvalidOperationException("Malformed response from ClearML server.");
        return deleted.Value;
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

    public async Task<IReadOnlyList<ClearMLTask>> GetTasksForCurrentQueueAsync(
        CancellationToken cancellationToken = default
    )
    {
        var body = new JsonObject { ["name"] = _options.CurrentValue.Queue };
        JsonObject? result = await CallAsync("queues", "get_all_ex", body, cancellationToken);
        var tasks = (JsonArray?)result?["data"]?["queues"]?[0]?["entries"];
        IEnumerable<string> taskIds = tasks?.Select(t => (string)t?["id"]!) ?? new List<string>();
        return await GetTasksByIdAsync(taskIds, cancellationToken);
    }

    public async Task<ClearMLTask?> GetTaskByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ClearMLTask> tasks = await GetTasksAsync(new JsonObject { ["name"] = name }, cancellationToken);
        if (tasks.Count == 0)
            return null;
        return tasks[0];
    }

    public Task<IReadOnlyList<ClearMLTask>> GetTasksByIdAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken = default
    )
    {
        return GetTasksAsync(new JsonObject { ["id"] = JsonValue.Create(ids.ToArray()) }, cancellationToken);
    }

    private async Task<IReadOnlyList<ClearMLTask>> GetTasksAsync(
        JsonObject body,
        CancellationToken cancellationToken = default
    )
    {
        body["only_fields"] = new JsonArray(
            "id",
            "name",
            "status",
            "project",
            "last_iteration",
            "status_reason",
            "status_message",
            "created",
            "active_duration",
            "last_metrics"
        );
        JsonObject? result = await CallAsync("tasks", "get_all_ex", body, cancellationToken);
        var tasks = (JsonArray?)result?["data"]?["tasks"];
        return tasks?.Select(t => t.Deserialize<ClearMLTask>(JsonSerializerOptions)!).ToArray()
            ?? Array.Empty<ClearMLTask>();
    }

    private async Task<JsonObject?> CallAsync(
        string service,
        string action,
        JsonNode body,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{service}.{action}")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Add(
            "Authorization",
            $"Bearer {await _clearMLAuthService.GetAuthTokenAsync(cancellationToken)}"
        );
        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        string result = await response.Content.ReadAsStringAsync(cancellationToken);
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
