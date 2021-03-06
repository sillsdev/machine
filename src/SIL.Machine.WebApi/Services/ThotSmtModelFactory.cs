﻿using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;
using SIL.Machine.WebApi.Configuration;

namespace SIL.Machine.WebApi.Services
{
	public class ThotSmtModelFactory : IComponentFactory<IInteractiveTranslationModel>
	{
		private readonly IOptions<ThotSmtModelOptions> _options;
		private readonly IOptions<EngineOptions> _engineOptions;

		public ThotSmtModelFactory(IOptions<ThotSmtModelOptions> options, IOptions<EngineOptions> engineOptions)
		{
			_options = options;
			_engineOptions = engineOptions;
		}

		public Task<IInteractiveTranslationModel> CreateAsync(string engineId)
		{
			string smtConfigFileName = Path.Combine(_engineOptions.Value.EnginesDir, engineId, "smt.cfg");
			return Task.FromResult<IInteractiveTranslationModel>(new ThotSmtModel(smtConfigFileName));
		}

		public void InitNew(string engineId)
		{
			string engineDir = Path.Combine(_engineOptions.Value.EnginesDir, engineId);
			if (!Directory.Exists(engineDir))
				Directory.CreateDirectory(engineDir);
			ZipFile.ExtractToDirectory(_options.Value.NewModelFile, engineDir);
		}

		public void Cleanup(string engineId)
		{
			string engineDir = Path.Combine(_engineOptions.Value.EnginesDir, engineId);
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
