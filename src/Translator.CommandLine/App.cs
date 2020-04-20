using System.Reflection;
using McMaster.Extensions.CommandLineUtils;

namespace SIL.Machine.Translation
{
	public class App : CommandLineApplication
	{
		public App()
			: base(false)
		{
			Name = "translator";
			FullName = "SIL.Machine Translator";
			Description = "A tool for training and evaluating machine translation engines.";

			HelpOption("-?|-h|--help", true);
			string version = Assembly.GetEntryAssembly().GetName().Version.ToString();
			VersionOption("-v|--version", version);

			AddCommand(new TrainCommand());
			AddCommand(new TestCommand());
			AddCommand(new AlignCommand());
			AddCommand(new CorpusCommand());
			AddCommand(new ExtractCommand());

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
