namespace SIL.Machine.WebApi.Services;

[TestFixture]
public class WebhookServiceTests
{
	[Test]
	public async Task TriggerEventAsync_NoHooks()
	{
		var env = new TestEnvironment();
		MockedRequest req = env.MockHttp.When("*")
			.Respond(HttpStatusCode.OK);

		var build = new Build
		{
			Id = "build1",
			EngineRef = "engine1"
		};
		await env.Service.TriggerEventAsync(WebhookEvent.BuildStarted, "client", build);

		Assert.That(env.MockHttp.GetMatchCount(req), Is.EqualTo(0));
	}

	[Test]
	public async Task TriggerEventAsync_MatchingHook()
	{
		var env = new TestEnvironment();
		env.Hooks.Add(new Webhook
		{
			Id = "hook1",
			Url = "https://test.client.com/hook",
			Secret = "this is a secret",
			Owner = "client",
			Events = { WebhookEvent.BuildStarted }
		});
		env.MockHttp.Expect("https://test.client.com/hook")
			.WithHeaders("X-Hub-Signature-256",
				"sha256=91D511CB690762688C2C3AF1AE9874D6BF49EE2119B62FFB7654B2642A30BD49")
			.Respond(HttpStatusCode.OK);

		var build = new Build
		{
			Id = "build1",
			EngineRef = "engine1"
		};
		await env.Service.TriggerEventAsync(WebhookEvent.BuildStarted, "client", build);

		env.MockHttp.VerifyNoOutstandingExpectation();
	}

	[Test]
	public async Task TriggerEventAsync_NoMatchingHook()
	{
		var env = new TestEnvironment();
		env.Hooks.Add(new Webhook
		{
			Id = "hook1",
			Url = "https://test.client.com/hook",
			Secret = "this is a secret",
			Owner = "client",
			Events = { WebhookEvent.BuildStarted }
		});
		MockedRequest req = env.MockHttp.When("*")
			.Respond(HttpStatusCode.OK);

		var build = new Build
		{
			Id = "build1",
			EngineRef = "engine1"
		};
		await env.Service.TriggerEventAsync(WebhookEvent.BuildFinished, "client", build);

		Assert.That(env.MockHttp.GetMatchCount(req), Is.EqualTo(0));
	}

	[Test]
	public async Task TriggerEventAsync_RequestTimeout()
	{
		var env = new TestEnvironment();
		env.Hooks.Add(new Webhook
		{
			Id = "hook1",
			Url = "https://test.client.com/hook",
			Secret = "this is a secret",
			Owner = "client",
			Events = { WebhookEvent.BuildStarted }
		});
		env.MockHttp.Expect("https://test.client.com/hook")
			.WithHeaders("X-Hub-Signature-256",
				"sha256=91D511CB690762688C2C3AF1AE9874D6BF49EE2119B62FFB7654B2642A30BD49")
			.Respond(HttpStatusCode.RequestTimeout);

		var build = new Build
		{
			Id = "build1",
			EngineRef = "engine1"
		};
		await env.Service.TriggerEventAsync(WebhookEvent.BuildStarted, "client", build);

		env.MockHttp.VerifyNoOutstandingExpectation();
	}

	[Test]
	public async Task TriggerEventAsync_Exception()
	{
		var env = new TestEnvironment();
		env.Hooks.Add(new Webhook
		{
			Id = "hook1",
			Url = "https://test.client.com/hook",
			Secret = "this is a secret",
			Owner = "client",
			Events = { WebhookEvent.BuildStarted }
		});
		env.MockHttp.Expect("https://test.client.com/hook")
			.WithHeaders("X-Hub-Signature-256",
				"sha256=91D511CB690762688C2C3AF1AE9874D6BF49EE2119B62FFB7654B2642A30BD49")
			.Throw(new HttpRequestException());

		var build = new Build
		{
			Id = "build1",
			EngineRef = "engine1"
		};
		await env.Service.TriggerEventAsync(WebhookEvent.BuildStarted, "client", build);

		env.MockHttp.VerifyNoOutstandingExpectation();
	}

	private class TestEnvironment
	{
		private readonly IMapper _mapper;
		private readonly IUrlHelper _urlHelper;

		public TestEnvironment()
		{
			_urlHelper = Substitute.For<IUrlHelper>();
			_urlHelper.RouteUrl(Arg.Any<UrlRouteContext>()).Returns("/translation/engines");
			var mapperConfig = new MapperConfiguration(c => c.AddProfile<MachineMapperProfile>());
			_mapper = new Mapper(mapperConfig);
			var jsonOptions = new JsonOptions();
			jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
			jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
			Service = new WebhookService(Hooks, _mapper, new OptionsWrapper<JsonOptions>(jsonOptions),
				MockHttp.ToHttpClient());
		}

		public IWebhookService Service { get; }
		public MemoryRepository<Webhook> Hooks { get; } = new MemoryRepository<Webhook>();
		public MockHttpMessageHandler MockHttp { get; } = new MockHttpMessageHandler();
	}
}
