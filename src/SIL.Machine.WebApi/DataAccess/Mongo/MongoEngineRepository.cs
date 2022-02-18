using Microsoft.Extensions.Options;
using SIL.Machine.WebApi.Configuration;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess.Mongo
{
	public class MongoEngineRepository : MongoRepository<Engine>, IEngineRepository
	{
		public MongoEngineRepository(IOptions<MongoDataAccessOptions> options)
			: base(options, "engines")
		{
		}
	}
}
