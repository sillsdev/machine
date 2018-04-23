using McMaster.Extensions.CommandLineUtils;
using System.IO;

namespace SIL.Machine.Translation
{
	public abstract class EngineCommandBase : ParallelTextCorpusCommandBase
	{
		private readonly CommandArgument _engineArgument;

		public EngineCommandBase(bool supportAlignmentsCorpus)
			: base(supportAlignmentsCorpus)
		{
			_engineArgument = Argument("engine", "The translation engine directory or configuration file.");
		}

		protected override int ExecuteCommand()
		{
			int result = base.ExecuteCommand();
			if (result != 0)
				return result;

			if (File.Exists(_engineArgument.Value))
			{
				EngineDirectory = Path.GetDirectoryName(_engineArgument.Value);
				EngineConfigFileName = _engineArgument.Value;
			}
			else if (Directory.Exists(_engineArgument.Value))
			{
				EngineDirectory = _engineArgument.Value;
				EngineConfigFileName = Path.Combine(_engineArgument.Value, "smt.cfg");
			}
			else if (IsDirectoryPath(_engineArgument.Value))
			{
				EngineDirectory = _engineArgument.Value;
				EngineConfigFileName = Path.Combine(_engineArgument.Value, "smt.cfg");
			}
			else
			{
				EngineDirectory = Path.GetDirectoryName(_engineArgument.Value);
				EngineConfigFileName = _engineArgument.Value;
			}

			return 0;
		}

		protected string EngineDirectory { get; private set; }
		protected string EngineConfigFileName { get; private set; }

		private static bool IsDirectoryPath(string path)
		{
			string separator1 = Path.DirectorySeparatorChar.ToString();
			string separator2 = Path.AltDirectorySeparatorChar.ToString();
			path = path.TrimEnd();
			return path.EndsWith(separator1) || path.EndsWith(separator2);
		}
	}
}
