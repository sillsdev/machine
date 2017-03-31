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

		public ThotSmtModelFactory(IOptions<ThotSmtModelOptions> options)
		{
			_options = options.Value;
		}

		public IInteractiveSmtModel Create(string configDir)
		{
			string smtConfigFileName = Path.Combine(configDir, "smt.cfg");
			return new ThotSmtModel(smtConfigFileName);
		}

		public void InitNewModel(string configDir)
		{
			using (Stream fileStream = File.OpenRead(_options.NewModelFile))
			using (Stream gzipStream = new GZipInputStream(fileStream))
			using (TarArchive archive = TarArchive.CreateInputTarArchive(gzipStream))
			{
				archive.ExtractContents(configDir);
			}
		}
	}
}
