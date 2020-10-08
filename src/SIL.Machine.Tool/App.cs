using System.Reflection;

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
			AddCommand(new CorpusCommand());
			AddCommand(new ExtractCommand());
			AddCommand(new TranslateCommand());
			AddCommand(new TokenizeCommand());
		}

		protected override int ExecuteCommand()
		{
			ShowHelp();
			return 0;
		}
	}
}
