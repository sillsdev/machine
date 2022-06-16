namespace SIL.Machine.WebApi.Services;

[TestFixture]
public class BuildServiceTests
{
    [Test]
    public async Task GetActiveNewerRevisionAsync_Insert()
    {
        var builds = new MemoryRepository<Build>();
        await using var service = new BuildService(builds);
        Task<EntityChange<Build>> task = service.GetActiveNewerRevisionAsync("engine1", 1);
        var build = new Build { ParentRef = "engine1", PercentCompleted = 0.1 };
        await builds.InsertAsync(build);
        EntityChange<Build> change = await task;
        Assert.That(change.Type, Is.EqualTo(EntityChangeType.Insert));
        Assert.That(change.Entity!.Revision, Is.EqualTo(1));
        Assert.That(change.Entity.PercentCompleted, Is.EqualTo(0.1));
    }

    [Test]
    public async Task GetNewerRevisionAsync_Update()
    {
        var builds = new MemoryRepository<Build>();
        await using var service = new BuildService(builds);
        var build = new Build { ParentRef = "engine1" };
        await builds.InsertAsync(build);
        Task<EntityChange<Build>> task = service.GetNewerRevisionAsync(build.Id, 2);
        await builds.UpdateAsync(build, u => u.Set(b => b.PercentCompleted, 0.1));
        EntityChange<Build> change = await task;
        Assert.That(change.Type, Is.EqualTo(EntityChangeType.Update));
        Assert.That(change.Entity!.Revision, Is.EqualTo(2));
        Assert.That(change.Entity.PercentCompleted, Is.EqualTo(0.1));
    }

    [Test]
    public async Task GetNewerRevisionAsync_Delete()
    {
        var builds = new MemoryRepository<Build>();
        await using var service = new BuildService(builds);
        var build = new Build { ParentRef = "engine1" };
        await builds.InsertAsync(build);
        Task<EntityChange<Build>> task = service.GetNewerRevisionAsync(build.Id, 2);
        await builds.DeleteAsync(build);
        EntityChange<Build> change = await task;
        Assert.That(change.Type, Is.EqualTo(EntityChangeType.Delete));
    }

    [Test]
    public async Task GetNewerRevisionAsync_DoesNotExist()
    {
        await using var service = new BuildService(new MemoryRepository<Build>());
        EntityChange<Build> change = await service.GetNewerRevisionAsync("build1", 2);
        Assert.That(change.Type, Is.EqualTo(EntityChangeType.Delete));
    }
}
