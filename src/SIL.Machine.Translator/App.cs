using System.Reflection;
using McMaster.Extensions.CommandLineUtils;

namespace SIL.Machine.Translation
{
	public class App : CommandLineApplication
	{
		public App()
		{
			Name = "translator";
			FullName = "SIL.Machine Translator";
			Description = "A tool for training and evaluating machine translation engines.";
			UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.CollectAndContinue;

			HelpOption("-?|-h|--help", true);
			string version = Assembly.GetEntryAssembly().GetName().Version.ToString();
			VersionOption("-v|--version", version);

			AddCommand(new TrainCommand());
			AddCommand(new TestCommand());
			AddCommand(new AlignCommand());
			AddCommand(new CorpusCommand());
			AddCommand(new ExtractCommand());
			AddCommand(new TranslateCommand());
			AddCommand(new TokenizeCommand());

			OnExecute(() =>
			{
				ShowHelp();
				return 0;
			});
		}

		private void AddCommand(CommandBase command)
		{
			command.Parent = this;
			Commands.Add(command);
		}
	}
}
