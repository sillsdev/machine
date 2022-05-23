namespace SIL.Machine.WebApi.Services;

[TestFixture]
public class DistributedReaderWriterLockFactoryTests
{
    [Test]
    public async Task InitAsync_ReleaseWriterLocks()
    {
        var env = new TestEnvironment();
        env.Locks.Add(
            new RWLock
            {
                Id = "resource1",
                WriterLock = new Lock { Id = "lock1", HostId = "this_service" }
            }
        );

        await env.Factory.InitAsync();

        RWLock resource1 = env.Locks.Get("resource1");
        Assert.That(resource1.WriterLock, Is.Null);
    }

    [Test]
    public async Task InitAsync_ReleaseReaderLocks()
    {
        var env = new TestEnvironment();
        env.Locks.Add(
            new RWLock
            {
                Id = "resource1",
                ReaderLocks =
                {
                    new Lock { Id = "lock1", HostId = "this_service" }
                }
            }
        );

        await env.Factory.InitAsync();

        RWLock resource1 = env.Locks.Get("resource1");
        Assert.That(resource1.ReaderLocks, Is.Empty);
    }

    [Test]
    public async Task InitAsync_RemoveWaiters()
    {
        var env = new TestEnvironment();
        env.Locks.Add(
            new RWLock
            {
                Id = "resource1",
                WriterLock = new Lock { Id = "lock1", HostId = "other_service" },
                WriterQueue =
                {
                    new Lock { Id = "lock2", HostId = "this_service" }
                }
            }
        );

        await env.Factory.InitAsync();

        RWLock resource1 = env.Locks.Get("resource1");
        Assert.That(resource1.WriterQueue, Is.Empty);
    }

    private class TestEnvironment
    {
        public TestEnvironment()
        {
            Locks = new MemoryRepository<RWLock>();
            var serviceOptions = new ServiceOptions { ServiceId = "this_service" };
            Factory = new DistributedReaderWriterLockFactory(new OptionsWrapper<ServiceOptions>(serviceOptions), Locks);
        }

        public MemoryRepository<RWLock> Locks { get; }
        public DistributedReaderWriterLockFactory Factory { get; }
    }
}
