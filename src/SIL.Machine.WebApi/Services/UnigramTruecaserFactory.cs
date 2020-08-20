using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Configuration;

namespace SIL.Machine.WebApi.Services
{
	public class UnigramTruecaserFactory : IComponentFactory<ITruecaser>
	{
		private readonly IOptions<EngineOptions> _engineOptions;

		public UnigramTruecaserFactory(IOptions<EngineOptions> engineOptions)
		{
			_engineOptions = engineOptions;
		}

		public async Task<ITruecaser> CreateAsync(string engineId)
		{
			var truecaser = new UnigramTruecaser();
			string path = GetModelPath(engineId);
			await truecaser.LoadAsync(path);
			return truecaser;
		}

		public void InitNew(string engineId)
		{
		}

		public void Cleanup(string engineId)
		{
			string path = GetModelPath(engineId);
			if (File.Exists(path))
				File.Delete(path);
		}

		private string GetModelPath(string engineId)
		{
			return Path.Combine(_engineOptions.Value.EnginesDir, engineId, "unigram-casing-model.txt");
		}
	}
}
