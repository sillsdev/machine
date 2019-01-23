using System.IO;
using System.Linq;
using Microsoft.Extensions.Options;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;
using SIL.Machine.WebApi.Options;
using System.IO.Compression;

namespace SIL.Machine.WebApi.Services
{
	public class ThotSmtModelFactory : ISmtModelFactory
	{
		private readonly ThotSmtModelOptions _options;
		private readonly string _rootDir;

		public ThotSmtModelFactory(IOptions<ThotSmtModelOptions> options, IOptions<MachineOptions> machineOptions)
		{
			_options = options.Value;
			_rootDir = machineOptions.Value.EnginesDir;
		}

		public IInteractiveSmtModel Create(string engineId)
		{
			string smtConfigFileName = Path.Combine(_rootDir, engineId, "smt.cfg");
			return new ThotSmtModel(smtConfigFileName);
		}

		public void InitNewModel(string engineId)
		{
			string engineDir = Path.Combine(_rootDir, engineId);
			if (!Directory.Exists(engineDir))
				Directory.CreateDirectory(engineDir);
			ZipFile.ExtractToDirectory(_options.NewModelFile, engineDir);
		}

		public void CleanupModel(string engineId)
		{
			string engineDir = Path.Combine(_rootDir, engineId);
			if (!Directory.Exists(engineDir))
				return;
			string lmDir = Path.Combine(engineDir, "lm");
			if (Directory.Exists(lmDir))
				Directory.Delete(lmDir, true);
			string tmDir = Path.Combine(engineDir, "tm");
			if (Directory.Exists(tmDir))
				Directory.Delete(tmDir, true);
			string smtConfigFileName = Path.Combine(engineDir, "smt.cfg");
			if (File.Exists(smtConfigFileName))
				File.Delete(smtConfigFileName);
			if (!Directory.EnumerateFileSystemEntries(engineDir).Any())
				Directory.Delete(engineDir);
		}
	}
}
