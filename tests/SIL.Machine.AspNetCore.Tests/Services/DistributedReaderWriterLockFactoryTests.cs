namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class DistributedReaderWriterLockFactoryTests
{
    [Test]
    public async Task InitAsync_ReleaseWriterLocks()
    {
        TestEnvironment env = new();
        env.Locks.Add(
            new RWLock
            {
                Id = "resource1",
                WriterLock = new() { Id = "lock1", HostId = "this_service" },
                ReaderLocks = [],
                WriterQueue = []
            }
        );

        await env.Factory.InitAsync();

        RWLock resource1 = env.Locks.Get("resource1");
        Assert.That(resource1.WriterLock, Is.Null);
    }

    [Test]
    public async Task InitAsync_ReleaseReaderLocks()
    {
        TestEnvironment env = new();
        env.Locks.Add(
            new RWLock
            {
                Id = "resource1",
                ReaderLocks = [new() { Id = "lock1", HostId = "this_service" }],
                WriterQueue = []
            }
        );

        await env.Factory.InitAsync();

        RWLock resource1 = env.Locks.Get("resource1");
        Assert.That(resource1.ReaderLocks, Is.Empty);
    }

    [Test]
    public async Task InitAsync_RemoveWaiters()
    {
        TestEnvironment env = new();
        env.Locks.Add(
            new RWLock
            {
                Id = "resource1",
                WriterLock = new() { Id = "lock1", HostId = "other_service" },
                ReaderLocks = [],
                WriterQueue = [new() { Id = "lock2", HostId = "this_service" }]
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
            ServiceOptions serviceOptions = new() { ServiceId = "this_service" };
            Factory = new DistributedReaderWriterLockFactory(
                new OptionsWrapper<ServiceOptions>(serviceOptions),
                Locks,
                new ObjectIdGenerator()
            );
        }

        public MemoryRepository<RWLock> Locks { get; }
        public DistributedReaderWriterLockFactory Factory { get; }
    }
}
