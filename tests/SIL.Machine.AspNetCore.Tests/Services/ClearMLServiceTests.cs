namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class ClearMLServiceTests
{
    private const string ApiServier = "https://clearml.com";
    private const string AccessKey = "accessKey";
    private const string SecretKey = "secretKey";

    [Test]
    public async Task CreateTaskAsync()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .Expect(HttpMethod.Post, $"{ApiServier}/tasks.create")
            .WithHeaders("Authorization", $"Bearer accessToken")
            .WithPartialContent("\\u0027src_lang\\u0027: \\u0027spa_Latn\\u0027")
            .WithPartialContent("\\u0027trg_lang\\u0027: \\u0027eng_Latn\\u0027")
            .Respond("application/json", "{ \"data\": { \"id\": \"projectId\" } }");

        var options = Substitute.For<IOptionsMonitor<ClearMLNmtEngineOptions>>();
        options.CurrentValue.Returns(
            new ClearMLNmtEngineOptions
            {
                ApiServer = ApiServier,
                AccessKey = AccessKey,
                SecretKey = SecretKey
            }
        );
        var authService = Substitute.For<IClearMLAuthenticationService>();
        authService.GetAuthTokenAsync().Returns(Task.FromResult("accessToken"));
        var service = new ClearMLService(
            mockHttp.ToHttpClient(),
            options,
            Substitute.For<ILogger<ClearMLService>>(),
            authService
        );

        string projectId = await service.CreateTaskAsync(
            "build1",
            "project1",
            "engine1",
            "es",
            "en",
            "s3://aqua-ml-data"
        );
        Assert.That(projectId, Is.EqualTo("projectId"));
        mockHttp.VerifyNoOutstandingExpectation();
    }
}
