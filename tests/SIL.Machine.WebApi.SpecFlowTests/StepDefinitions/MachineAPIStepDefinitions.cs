using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using SIL.Machine.WebApi.Client;

namespace SIL.Machine.WebApi.SpecFlowTests.StepDefinitions
{
    [Binding]
    public sealed class MachineApiStepDefinitions
    {
        const string MACHINE_API_TEST_URL = "https://machine-api.org/"; // localhost "http://machine-api.vcap.me/" QA server: https://machine-api.org/

        // For additional details on SpecFlow step definitions see https://go.specflow.org/doc-stepdef
        private readonly WebApiClient client;

        public MachineApiStepDefinitions()
        {
            this.client = new WebApiClient(MACHINE_API_TEST_URL, true);
            SetAccessTokenFromEnvironment();
        }

        private void SetAccessTokenFromEnvironment()
        {
            var client_id =
                Environment.GetEnvironmentVariable("MACHINE_CLIENT_ID")
                ?? throw new ArgumentException(
                    "You need an auth0 client_id in the environment variable MACHINE_CLIENT_ID!  Look at README for instructions on getting one."
                );
            var client_secret =
                Environment.GetEnvironmentVariable("MACHINE_CLIENT_SECRET")
                ?? throw new ArgumentException(
                    "You need an auth0 client_secret in the environment variable MACHINE_CLIENT_SECRET!  Look at README for instructions on getting one."
                );
            client.AquireAccessToken(client_id, client_secret);
        }

        [Given(@"a new SMT engine for (.*) from (.*) to (.*)")]
        public async Task GivenNewSMTEngine(string user, string source_language, string target_language)
        {
            var existingTranslationEngines = await client.GetAllEnginesAsync();
            foreach (var translationEngine in existingTranslationEngines)
            {
                if (translationEngine.Name == user)
                {
                    await client.DeleteEngineAsync(translationEngine.Id);
                }
            }
            await client.PostEngineAsync(
                new TranslationEngineConfigDto()
                {
                    Name = user,
                    SourceLanguageTag = source_language,
                    TargetLanguageTag = target_language,
                    Type = TranslationEngineType.SmtTransfer
                }
            );
        }

        [Given(@"(.*) corpora for (.*) in (.*)")]
        public void GivenCorporaForEngine(string corpora, string user, string language)
        {
            throw new PendingStepException();
        }

        [When(@"the engine is built for (.*)")]
        public void WhenEngineIsBuild(string user)
        {
            throw new PendingStepException();
        }

        [When(@"a translation for (.*) is added with ""(.*)"" for ""(.*)""")]
        public void WhenTranslationAdded(string user, string targetSegment, string sourceSegment)
        {
            throw new PendingStepException();
        }

        [Then(@"the translation for (.*) for ""(.*)"" should be ""(.*)""")]
        public void ThenTheTranslationShouldBe(string user, string sourceSegment, string targetSegment)
        {
            throw new PendingStepException();
        }
    }
}
