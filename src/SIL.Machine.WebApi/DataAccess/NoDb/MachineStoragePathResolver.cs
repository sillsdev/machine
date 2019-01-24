using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NoDb;
using SIL.Machine.WebApi.Configuration;

namespace SIL.Machine.WebApi.DataAccess.NoDb
{
	public class MachineStoragePathResolver<T> : IStoragePathResolver<T> where T : class
	{
		private readonly string _dataDir;
		private readonly string _entityName;

		public MachineStoragePathResolver(IOptions<NoDbDataAccessOptions> options)
		{
			_dataDir = options.Value.DataDir;
			_entityName = typeof(T).Name.ToLowerInvariant();
		}

		public Task<string> ResolvePath(string projectId, string key = "", string fileExtension = ".json",
			bool ensureFoldersExist = false, CancellationToken cancellationToken = default(CancellationToken))
		{
			string dir = Path.Combine(_dataDir, _entityName);
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			if (string.IsNullOrEmpty(key))
				return Task.FromResult(dir + Path.DirectorySeparatorChar);
			return Task.FromResult(Path.Combine(dir, key + fileExtension));
		}

		public async Task<string> ResolvePath(string projectId, string key, T obj, string fileExtension = ".json",
			bool ensureFoldersExist = false, CancellationToken cancellationToken = default(CancellationToken))
		{
			return await ResolvePath(projectId, key, fileExtension, ensureFoldersExist, cancellationToken);
		}
	}
}
