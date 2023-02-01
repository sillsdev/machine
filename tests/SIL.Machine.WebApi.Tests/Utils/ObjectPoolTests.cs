namespace SIL.Machine.WebApi.Utils;

[TestFixture]
public class ObjectPoolTests
{
    [Test]
    public async Task GetAsync_Empty()
    {
        int i = 0;
        using var pool = new ObjectPool<int>(2, () => i++);
        Assert.That(pool.Count, Is.EqualTo(0));
        using (ObjectPoolItem<int> item = await pool.GetAsync())
        {
            Assert.That(item.Object, Is.EqualTo(0));
        }
        Assert.That(pool.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetAsync_NotEmpty()
    {
        int i = 0;
        using var pool = new ObjectPool<int>(2, () => i++);
        Assert.That(pool.Count, Is.EqualTo(0));
        using (ObjectPoolItem<int> item = await pool.GetAsync()) { }
        Assert.That(pool.Count, Is.EqualTo(1));
        using (ObjectPoolItem<int> item = await pool.GetAsync())
        {
            Assert.That(item.Object, Is.EqualTo(0));
        }
        Assert.That(pool.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetAsync_ObjectInUse()
    {
        int i = 0;
        using var pool = new ObjectPool<int>(2, () => i++);
        Assert.That(pool.Count, Is.EqualTo(0));
        var evt = new AsyncAutoResetEvent();
        Task task = HoldObjectAsync(pool, evt);
        Assert.That(pool.Count, Is.EqualTo(1));
        using (ObjectPoolItem<int> item = await pool.GetAsync())
        {
            Assert.That(item.Object, Is.EqualTo(1));
        }
        evt.Set();
        await task;
        Assert.That(pool.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetAsync_MaxObjectsInUse()
    {
        int i = 0;
        using var pool = new ObjectPool<int>(2, () => i++);
        Assert.That(pool.Count, Is.EqualTo(0));
        var evt1 = new AsyncAutoResetEvent();
        Task task1 = HoldObjectAsync(pool, evt1);
        Assert.That(pool.Count, Is.EqualTo(1));
        Assert.That(pool.AvailableCount, Is.EqualTo(0));
        var evt2 = new AsyncAutoResetEvent();
        Task task2 = HoldObjectAsync(pool, evt2);
        Assert.That(pool.Count, Is.EqualTo(2));
        Assert.That(pool.AvailableCount, Is.EqualTo(0));
        Task<ObjectPoolItem<int>> task3 = pool.GetAsync();
        Assert.That(pool.Count, Is.EqualTo(2));
        evt1.Set();
        await task1;
        using (ObjectPoolItem<int> item = await task3)
        {
            Assert.That(item.Object, Is.EqualTo(0));
        }
        Assert.That(pool.AvailableCount, Is.EqualTo(1));
        evt2.Set();
        await task2;
        Assert.That(pool.Count, Is.EqualTo(2));
        Assert.That(pool.AvailableCount, Is.EqualTo(2));
    }

    private static async Task HoldObjectAsync<T>(ObjectPool<T> pool, AsyncAutoResetEvent evt)
    {
        using ObjectPoolItem<T> item = pool.GetAsync().WaitAndUnwrapException();
        await evt.WaitAsync();
    }
}
