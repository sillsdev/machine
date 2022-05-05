namespace SIL.Machine.WebApi.Services;

public class WebhookService : EntityServiceBase<Webhook>, IWebhookService
{
	private readonly HttpClient _httpClient;
	private readonly IMapper _mapper;
	private readonly IOptions<JsonOptions> _jsonOptions;

	public WebhookService(IRepository<Webhook> hooks, IMapper mapper, IOptions<JsonOptions> jsonOptions,
		HttpClient httpClient)
		: base(hooks)
	{
		_mapper = mapper;
		_jsonOptions = jsonOptions;
		_httpClient = httpClient;
	}

	public async Task<IEnumerable<Webhook>> GetAllAsync(string owner)
	{
		CheckDisposed();

		return await Entities.GetAllAsync(c => c.Owner == owner);
	}

	public async Task SendEventAsync<T>(WebhookEvent webhookEvent, string owner, T resource)
	{
		CheckDisposed();

		IReadOnlyList<Webhook> matchingHooks = await GetWebhooks(webhookEvent, owner);
		if (matchingHooks.Count == 0)
			return;

		string typeName = typeof(T).Name;
		var machineAssembly = Assembly.GetAssembly(typeof(ResourceDto));
		var dtoType = Type.GetType($"SIL.Machine.WebApi.{typeName}Dto, {machineAssembly!.FullName}");
		if (dtoType == null)
			throw new ArgumentException("A DTO type is not defined for the specified model.", nameof(resource));

		foreach (Webhook hook in matchingHooks)
		{
			string payload = CreatePayload(webhookEvent, resource, dtoType);
			var request = new HttpRequestMessage(HttpMethod.Post, hook.Url)
			{
				Content = new StringContent(payload, Encoding.UTF8, "application/json")
			};
			byte[] keyBytes = Encoding.UTF8.GetBytes(hook.Secret);
			byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
			using (var hmac = new HMACSHA256(keyBytes))
			{
				byte[] hash = hmac.ComputeHash(payloadBytes);
				string signature = $"sha256={Convert.ToHexString(hash)}";
				request.Headers.Add("X-Hub-Signature-256", signature);
			}
			try
			{
				await _httpClient.SendAsync(request);
			}
			catch (HttpRequestException)
			{
			}
		}
	}

	private Task<IReadOnlyList<Webhook>> GetWebhooks(WebhookEvent webhookEvent, string owner)
	{
		return Entities.GetAllAsync(h => h.Owner == owner && h.Events.Contains(webhookEvent));
	}

	private string CreatePayload<T>(WebhookEvent webhookEvent, T resource, Type dtoType)
	{
		string resourcePropName = ToCamelCase(typeof(T).Name);
		var dto = _mapper.Map(resource, typeof(T), dtoType);
		var payload = new JsonObject
		{
			["event"] = webhookEvent.ToString(),
			[resourcePropName] = JsonSerializer.SerializeToNode(dto, dtoType, _jsonOptions.Value.JsonSerializerOptions)
		};
		return payload.ToJsonString();
	}

	private static string ToCamelCase(string str)
	{
		if (!string.IsNullOrEmpty(str) && str.Length > 1)
			return char.ToLowerInvariant(str[0]) + str.Substring(1);
		return str;
	}
}
