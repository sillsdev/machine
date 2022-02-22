namespace SIL.Machine.WebApi.DataAccess.Mongo;

public class MongoEngineRepository : MongoRepository<Engine>
{
	public MongoEngineRepository(IOptions<MongoDataAccessOptions> options)
		: base(options, "engines")
	{
	}
}
