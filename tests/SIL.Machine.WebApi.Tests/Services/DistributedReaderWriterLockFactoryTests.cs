namespace SIL.Machine.WebApi.Services;

[TestFixture]
public class DistributedReaderWriterLockFactoryTests
{
	[Test]
	public async Task InitAsync()
	{
		var locks = new MemoryRepository<RWLock>();
		locks.Add(new RWLock
		{
			Id = "resource1",
			WriterLock = new Lock
			{
				Id = "lock1",
				HostId = "test_service"
			}
		});
		locks.Add(new RWLock
		{
			Id = "resource2",
			ReaderLocks =
			{
				new Lock
				{
					Id = "lock2",
					HostId = "test_service"
				}
			}
		});

		var serviceOptions = new ServiceOptions { ServiceId = "test_service" };
		var factory = new DistributedReaderWriterLockFactory(new OptionsWrapper<ServiceOptions>(serviceOptions), locks);

		await factory.InitAsync();

		RWLock resource1 = locks.Get("resource1");
		Assert.That(resource1.WriterLock, Is.Null);
		RWLock resource2 = locks.Get("resource2");
		Assert.That(resource2.ReaderLocks, Is.Empty);
	}
}
