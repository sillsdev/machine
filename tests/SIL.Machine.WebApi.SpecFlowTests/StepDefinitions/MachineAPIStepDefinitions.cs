
using RestSharp;
using RestSharp.Authenticators;
using SIL.Machine.WebApi.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;



namespace SIL.Machine.WebApi.SpecFlowTests.StepDefinitions
{

	[Binding]
	public sealed class MachineApiStepDefinitions
	{
		const string MACHINE_API_TEST_URL = "https://machine-api.org/"; // localhost "http://machine-api.vcap.me/" QA server: https://machine-api.org/
		// For additional details on SpecFlow step definitions see https://go.specflow.org/doc-stepdef
		private Dictionary<string, Dictionary<string, string>> _userInfo = new Dictionary<string, Dictionary<string, string>>();
		private RestClient _client = new RestClient(new RestClientOptions(MACHINE_API_TEST_URL)
		{
			RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
		});
		private string api_access_token = Environment.GetEnvironmentVariable("MACHINE_API_TOKEN") ?? "";

		public MachineApiStepDefinitions()
		{
			if(this.api_access_token == "")
			{
				throw new ArgumentException("You need an auth0 access token in the environment variable MACHINE_API_TOKEN!  Look at README for instructions on getting one.");
			}	
		}
		private RestRequest AddAuth0(RestRequest request)
		{
			request.AddHeader("content-type", "application/json");
			request.AddHeader("authorization", $"Bearer {this.api_access_token}");
			return request;
		}

		[Given(@"a new SMT engine for (.*) from (.*) to (.*)")]
		public void GivenNewSMTEngine(string user, string source_language, string target_language)
		{
			this._userInfo.Add(user, new Dictionary<string, string>
			{
				{"engine_id","" },
				{"corpora","" },
				{"source_language",source_language },
				{"target_language",target_language },
			});
			var engRequest = AddAuth0(new RestRequest("translation-engines"));
			var existingTranslationEngines = this._client.GetAsync(engRequest).Result;
			if(existingTranslationEngines.Content != null)
			{
				var translationEngines = JsonConvert.DeserializeObject<List<TranslationEngineDto>>(existingTranslationEngines.Content, WebApiClient.SerializerSettings);
				foreach (TranslationEngineDto translationEngine in translationEngines ?? new List<TranslationEngineDto>())
				{
					var deleteResult = this._client.DeleteAsync(AddAuth0(new RestRequest("translation-engines/" + translationEngine.Id))).Result;
				}
			}
			else
			{
				throw new Exception("Current Machine Engines unable to be read from " + MACHINE_API_TEST_URL);
			}
			var request = new RestRequest("translation-engines").AddJsonBody(new
			{
				name = user,
				sourceLanguageTag = source_language,
				targetLanguageTag = target_language,
				type = "SmtTransfer"
			});
			var postResult = this._client.PostAsync(AddAuth0(request)).Result;
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