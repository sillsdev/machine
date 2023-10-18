namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class ClearMLServiceTests
{
    private const string ApiServer = "https://clearml.com";
    private const string AccessKey = "accessKey";
    private const string SecretKey = "secretKey";

    [Test]
    public async Task CreateTaskAsync()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .Expect(HttpMethod.Post, $"{ApiServer}/tasks.create")
            .WithHeaders("Authorization", $"Bearer accessToken")
            .WithPartialContent("\\u0027src_lang\\u0027: \\u0027spa_Latn\\u0027")
            .WithPartialContent("\\u0027trg_lang\\u0027: \\u0027eng_Latn\\u0027")
            .Respond("application/json", "{ \"data\": { \"id\": \"projectId\" } }");
        HttpClient httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(ApiServer);

        var options = Substitute.For<IOptionsMonitor<ClearMLOptions>>();
        options.CurrentValue.Returns(new ClearMLOptions { AccessKey = AccessKey, SecretKey = SecretKey });
        var authService = Substitute.For<IClearMLAuthenticationService>();
        authService.GetAuthTokenAsync().Returns(Task.FromResult("accessToken"));
        var env = new HostingEnvironment { EnvironmentName = Environments.Development };
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient("ClearML").Returns(httpClient);
        var service = new ClearMLService(httpClientFactory, options, authService, env);

        string script =
            "from machine.jobs.build_nmt_engine import run\n"
            + "args = {\n"
            + "    'model_type': 'huggingface',\n"
            + "    'engine_id': 'engine1',\n"
            + "    'build_id': 'build1',\n"
            + "    'src_lang': 'spa_Latn',\n"
            + "    'trg_lang': 'eng_Latn',\n"
            + "    'max_steps': 20000,\n"
            + "    'shared_file_uri': 's3://aqua-ml-data',\n"
            + "    'clearml': True\n"
            + "}\n"
            + "run(args)\n";

        string projectId = await service.CreateTaskAsync("build1", "project1", script);
        Assert.That(projectId, Is.EqualTo("projectId"));
        mockHttp.VerifyNoOutstandingExpectation();
    }
}
