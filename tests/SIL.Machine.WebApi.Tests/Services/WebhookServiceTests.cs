namespace SIL.Machine.WebApi.Services;

[TestFixture]
public class WebhookServiceTests
{
    [Test]
    public async Task SendEventAsync_NoHooks()
    {
        var env = new TestEnvironment();
        MockedRequest req = env.MockHttp.When("*").Respond(HttpStatusCode.OK);

        var build = new Build { Id = "build1", ParentRef = "engine1" };
        await env.Service.SendEventAsync(WebhookEvent.BuildStarted, "client", build);

        Assert.That(env.MockHttp.GetMatchCount(req), Is.EqualTo(0));
    }

    [Test]
    public async Task SendEventAsync_MatchingHook()
    {
        var env = new TestEnvironment();
        env.Hooks.Add(
            new Webhook
            {
                Id = "hook1",
                Url = "https://test.client.com/hook",
                Secret = "this is a secret",
                Owner = "client",
                Events = { WebhookEvent.BuildStarted }
            }
        );
        env.MockHttp
            .Expect("https://test.client.com/hook")
            .WithHeaders(
                "X-Hub-Signature-256",
                "sha256=F0A5C120AF1C2BB581802DEB53089F0A1209F97D450F2F7D627127D36001CB3F"
            )
            .Respond(HttpStatusCode.OK);

        var build = new Build { Id = "build1", ParentRef = "engine1" };
        await env.Service.SendEventAsync(WebhookEvent.BuildStarted, "client", build);

        env.MockHttp.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task SendEventAsync_NoMatchingHook()
    {
        var env = new TestEnvironment();
        env.Hooks.Add(
            new Webhook
            {
                Id = "hook1",
                Url = "https://test.client.com/hook",
                Secret = "this is a secret",
                Owner = "client",
                Events = { WebhookEvent.BuildStarted }
            }
        );
        MockedRequest req = env.MockHttp.When("*").Respond(HttpStatusCode.OK);

        var build = new Build { Id = "build1", ParentRef = "engine1" };
        await env.Service.SendEventAsync(WebhookEvent.BuildFinished, "client", build);

        Assert.That(env.MockHttp.GetMatchCount(req), Is.EqualTo(0));
    }

    [Test]
    public async Task SendEventAsync_RequestTimeout()
    {
        var env = new TestEnvironment();
        env.Hooks.Add(
            new Webhook
            {
                Id = "hook1",
                Url = "https://test.client.com/hook",
                Secret = "this is a secret",
                Owner = "client",
                Events = { WebhookEvent.BuildStarted }
            }
        );
        env.MockHttp
            .Expect("https://test.client.com/hook")
            .WithHeaders(
                "X-Hub-Signature-256",
                "sha256=F0A5C120AF1C2BB581802DEB53089F0A1209F97D450F2F7D627127D36001CB3F"
            )
            .Respond(HttpStatusCode.RequestTimeout);

        var build = new Build { Id = "build1", ParentRef = "engine1" };
        await env.Service.SendEventAsync(WebhookEvent.BuildStarted, "client", build);

        env.MockHttp.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task SendEventAsync_Exception()
    {
        var env = new TestEnvironment();
        env.Hooks.Add(
            new Webhook
            {
                Id = "hook1",
                Url = "https://test.client.com/hook",
                Secret = "this is a secret",
                Owner = "client",
                Events = { WebhookEvent.BuildStarted }
            }
        );
        env.MockHttp
            .Expect("https://test.client.com/hook")
            .WithHeaders(
                "X-Hub-Signature-256",
                "sha256=F0A5C120AF1C2BB581802DEB53089F0A1209F97D450F2F7D627127D36001CB3F"
            )
            .Throw(new HttpRequestException());

        var build = new Build { Id = "build1", ParentRef = "engine1" };
        await env.Service.SendEventAsync(WebhookEvent.BuildStarted, "client", build);

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
            var mapperConfig = new MapperConfiguration(c => c.AddProfile<MapperProfile>());
            _mapper = new Mapper(mapperConfig);
            var jsonOptions = new JsonOptions();
            jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            Service = new WebhookService(
                Hooks,
                _mapper,
                new OptionsWrapper<JsonOptions>(jsonOptions),
                MockHttp.ToHttpClient()
            );
        }

        public IWebhookService Service { get; }
        public MemoryRepository<Webhook> Hooks { get; } = new MemoryRepository<Webhook>();
        public MockHttpMessageHandler MockHttp { get; } = new MockHttpMessageHandler();
    }
}
