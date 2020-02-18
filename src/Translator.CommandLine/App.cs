using McMaster.Extensions.CommandLineUtils;
using System.Reflection;

namespace SIL.Machine.Translation
{
	public class App : CommandLineApplication
	{
		public App()
			: base(false)
		{
			Name = "translator";
			FullName = "Machine Translator";

			HelpOption("-?|-h|--help");
			string version = Assembly.GetEntryAssembly().GetName().Version.ToString();
			VersionOption("-v|--version", version);

			AddCommand(new TrainCommand());
			AddCommand(new TestCommand());
			AddCommand(new AlignCommand());
			AddCommand(new CorpusCommand());
			AddCommand(new ExtractCommand());
		}

		private void AddCommand(CommandBase command)
		{
			command.Parent = this;
			Commands.Add(command);
		}
	}
}
