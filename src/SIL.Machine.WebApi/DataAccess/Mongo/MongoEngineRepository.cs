namespace SIL.Machine.WebApi.DataAccess.Mongo;

public class MongoEngineRepository : MongoRepository<Engine>, IEngineRepository
{
	public MongoEngineRepository(IOptions<MongoDataAccessOptions> options)
		: base(options, "engines")
	{
	}
}
