using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine
{
    public class App : CommandBase
    {
        public App()
        {
            Name = "machine";
            FullName = "Machine";
            Description = "Natural language processing tools with a focus on resource-poor languages.";

            HelpOption("-?|-h|--help", true);
            string version = Assembly.GetEntryAssembly().GetName().Version.ToString();
            int endIndex = version.LastIndexOf('.');
            version = version.Substring(0, endIndex);
            VersionOption("-v|--version", version);

            AddCommand(new TrainCommand());
            AddCommand(new SuggestCommand());
            AddCommand(new AlignCommand());
            AddCommand(new CorpusStatsCommand());
            AddCommand(new BuildCorpusCommand());
            AddCommand(new TranslateCommand());
            AddCommand(new TokenizeCommand());
            AddCommand(new ExtractLexiconCommand());
            AddCommand(new SymmetrizeCommand());
        }

        protected override Task<int> ExecuteCommandAsync(CancellationToken ct)
        {
            ShowHelp();
            return Task.FromResult(0);
        }
    }
}
