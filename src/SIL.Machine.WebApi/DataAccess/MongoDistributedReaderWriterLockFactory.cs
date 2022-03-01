namespace SIL.Machine.WebApi.DataAccess;

public class MongoDistributedReaderWriterLockFactory : IDistributedReaderWriterLockFactory
{
	private readonly IMongoCollection<RWLock> _locks;
	private readonly IMongoCollection<ReleaseSignal> _signals;

	public MongoDistributedReaderWriterLockFactory(IMongoDatabase db)
	{
		var filter = new BsonDocument("name", "signals");
		if (!db.ListCollectionNames(new ListCollectionNamesOptions { Filter = filter }).Any())
			db.CreateCollection("signals", new CreateCollectionOptions { Capped = true, MaxSize = 320_000 });
		_locks = db.GetCollection<RWLock>("locks");
		_signals = db.GetCollection<ReleaseSignal>("signals");
	}

	public IDistributedReaderWriterLock Create(string name)
	{
		return new MongoDistributedReaderWriterLock(_locks, _signals, name);
	}

	public async ValueTask<bool> DeleteAsync(string name)
	{
		DeleteResult result = await _locks.DeleteOneAsync(rwl => rwl.Id == name);
		return result.IsAcknowledged && result.DeletedCount > 0;
	}
}
