using System.Reflection;

namespace SIL.Machine.Translation
{
	public class App : CommandBase
	{
		public App()
		{
			Name = "translator";
			FullName = "SIL.Machine Translator";
			Description = "A tool for training and evaluating translation models.";

			HelpOption("-?|-h|--help", true);
			string version = Assembly.GetEntryAssembly().GetName().Version.ToString();
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
