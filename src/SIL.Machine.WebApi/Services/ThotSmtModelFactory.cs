using System.IO;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Microsoft.Extensions.Options;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;
using SIL.Machine.WebApi.Options;

namespace SIL.Machine.WebApi.Services
{
	public class ThotSmtModelFactory : ISmtModelFactory
	{
		private readonly ThotSmtModelOptions _options;
		private readonly string _rootDir;

		public ThotSmtModelFactory(IOptions<ThotSmtModelOptions> options, IOptions<EngineOptions> engineOptions)
		{
			_options = options.Value;
			_rootDir = engineOptions.Value.RootDir;
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
			using (Stream fileStream = File.OpenRead(_options.NewModelFile))
			using (Stream gzipStream = new GZipInputStream(fileStream))
			using (TarArchive archive = TarArchive.CreateInputTarArchive(gzipStream))
			{
				archive.ExtractContents(engineDir);
			}
		}

		public void Delete(string engineId)
		{
			string engineDir = Path.Combine(_rootDir, engineId);
			string lmDir = Path.Combine(engineDir, "lm");
			if (Directory.Exists(lmDir))
				Directory.Delete(lmDir, true);
			string tmDir = Path.Combine(engineDir, "tm");
			if (Directory.Exists(tmDir))
				Directory.Delete(tmDir, true);
			string smtConfigFileName = Path.Combine(engineDir, "smt.cfg");
			if (File.Exists(smtConfigFileName))
				File.Delete(smtConfigFileName);
		}
	}
}
