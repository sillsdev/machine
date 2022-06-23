using NUnit.Framework;
using SIL.Machine.WebApi.Client;

namespace SIL.Machine.WebApi.SpecFlowTests.StepDefinitions
{
    [Binding]
    public sealed class MachineApiStepDefinitions
    {
        // QA server: "https://machine-api.org/"
        // localhost: "https://machine-api.vcap.me:8444/"
        const string MACHINE_API_TEST_URL = "http://machine-api.vcap.me:81/";
        readonly Dictionary<string, string> EnginePerUser = new();
        readonly Dictionary<string, string> CorporaPerName = new();

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
            var engine = await client.PostEngineAsync(
                name: user,
                sourceLanguageTag: source_language,
                targetLanguageTag: target_language,
                type: TranslationEngineType.SmtTransfer
            );
            EnginePerUser.Add(user, engine.Id);
        }

        [Given(@"a new (.*) corpora named (.*) for (.*)")]
        public async Task GivenCorporaForEngine(string fileFormatString, string corpora, string user)
        {
            if (!Enum.TryParse(fileFormatString, ignoreCase: true, result: out FileFormat fileFormat))
                throw new ArgumentException(
                    "Corpus format type needs to be one of: " + string.Join(", ", EnumToStringList<FileFormat>())
                );
            var corpusId = await PostCorpus(corporaName: corpora, fileFormat: fileFormat);
            var engineId = await GetEngineFromUser(user);
            await client.PostCorporaToEngineAsync(engineId, corpusId);
        }

        [Given(@"(.*) are added to corpora (.*) in (.*) and (.*)")]
        public async Task AddFilesToCorpora(
            string filesToAddString,
            string corporaName,
            string language1,
            string language2
        )
        {
            var filesToAdd = filesToAddString.Split(", ");
            var corpusId = await GetCorporaFromName(corporaName);
            await PostFilesToCorpus(corpusId: corpusId, filesToAdd: filesToAdd, language: language1);
            await PostFilesToCorpus(corpusId: corpusId, filesToAdd: filesToAdd, language: language2);
        }

        [When(@"the engine is built for (.*)")]
        public async Task WhenEngineIsBuild(string user)
        {
            var engineId = await GetEngineFromUser(user);
            await client.TrainAsync(engineId, progressStatus => { });
        }

        [When(@"a translation for (.*) is added with ""(.*)"" for ""(.*)""")]
        public async Task WhenTranslationAdded(string user, string targetSegment, string sourceSegment)
        {
            var engineId = await GetEngineFromUser(user);
            await client.TrainSegmentPairAsync(engineId: engineId, targetSegment.Split(" "), sourceSegment.Split(" "));
        }

        [When(@"the translation for (.*) for ""(.*)"" is ""(.*)""")]
        public async Task WhenTheTranslationIs(string user, string sourceSegment, string targetSegment)
        {
            await ThenTheTranslationShouldBe(user, sourceSegment, targetSegment);
        }
        [Then(@"the translation for (.*) for ""(.*)"" should be ""(.*)""")]
        public async Task ThenTheTranslationShouldBe(string user, string sourceSegment, string targetSegment)
        {
            var engineId = await GetEngineFromUser(user);
            var translation = await client.TranslateSegmentAsync(engineId, sourceSegment.Split(" "));
            Assert.AreEqual(targetSegment, String.Join(" ", translation.TargetSegment));
        }

        public async Task<string> GetEngineFromUser(string user)
        {
            if (EnginePerUser.ContainsKey(user))
                return EnginePerUser[user];
            var engines = await client.GetAllEnginesAsync();
            foreach (var engine in engines)
            {
                if (engine.Name == user)
                    return engine.Id;
            }
            throw new ArgumentException($"No engine for user {user} available.");
        }

        public async Task<string> GetCorporaFromName(string corporaName)
        {
            if (CorporaPerName.ContainsKey(corporaName))
                return CorporaPerName[corporaName];
            var allCorpora = await client.GetAllCorporaAsync();
            foreach (var corpus in allCorpora)
            {
                if (corpus.Name == corporaName)
                    return corpus.Id;
            }
            throw new ArgumentException($"No corpus of name {corporaName} available.");
        }

        public async Task<string> PostCorpus(string corporaName, FileFormat fileFormat)
        {
            if (CorporaPerName.ContainsKey(corporaName))
                // we have already used it for this test.  Just return the id.
                return CorporaPerName[corporaName];
            var allCorpora = await client.GetAllCorporaAsync();
            foreach (var corpus in allCorpora)
            {
                if (corpus.Name == corporaName)
                {
                    // we want to recreate it to test out the process.
                    await client.DeleteCorpusAsync(corpus.Id);
                }
            }
            var newCorpus = await client.PostCorporaAsync(
                new CorpusConfigDto
                {
                    Name = corporaName,
                    Type = CorpusType.Text,
                    Format = fileFormat
                }
            );
            CorporaPerName.Add(corporaName, newCorpus.Id);
            return newCorpus.Id;
        }

        public async Task PostFilesToCorpus(string corpusId, IEnumerable<string> filesToAdd, string language)
        {
            string languageFolder = Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..", "data", language)
            );
            if (!Directory.Exists(languageFolder))
                throw new ArgumentException($"The langauge data directory {languageFolder} does not exist!");
            // Collect files for the corpus
            var files = Directory.GetFiles(languageFolder);
            if (files.Length == 0)
                throw new ArgumentException($"The langauge data directory {languageFolder} contains no files!");
            foreach (var fileName in filesToAdd)
            {
                string filePath = Path.GetFullPath(Path.Combine(languageFolder, fileName));
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"The corpus file {filePath} does not exist!");
                await client.PostCorporaFileAsync(
                    corpusId: corpusId,
                    languageTag: language,
                    textId: fileName,
                    filePath: filePath
                );
            }
        }

        public static IEnumerable<string> EnumToStringList<T>() where T : Enum
        {
            return ((IEnumerable<T>)Enum.GetValues(typeof(T))).Select(v => v.ToString());
        }
    }
}
