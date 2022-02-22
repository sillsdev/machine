using System.Threading.Tasks;
using NUnit.Framework;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess
{
	[TestFixture]
	public class DataAccessExtensionsTests
	{
		[Test]
		public async Task GetNewerRevisionByEngineIdAsync_Insert()
		{
			var buildRepo = new MemoryRepository<Build>();
			Task task = Task.Run(async () =>
			{
				await Task.Delay(10);
				var build = new Build { EngineRef = "engine1", PercentCompleted = 0.1 };
				await buildRepo.InsertAsync(build);
			});
			EntityChange<Build> change = await buildRepo.GetNewerRevisionByEngineIdAsync("engine1", 0);
			await task;
			Assert.That(change.Type, Is.EqualTo(EntityChangeType.Insert));
			Assert.That(change.Entity.Revision, Is.EqualTo(0));
			Assert.That(change.Entity.PercentCompleted, Is.EqualTo(0.1));
		}

		[Test]
		public async Task GetNewerRevisionAsync_Update()
		{
			var buildRepo = new MemoryRepository<Build>();
			var build = new Build { EngineRef = "engine1" };
			await buildRepo.InsertAsync(build);
			Task task = Task.Run(async () =>
				{
					await Task.Delay(10);
					await buildRepo.UpdateAsync(build, u => u
						.Inc(b => b.Revision)
						.Set(b => b.PercentCompleted, 0.1));
				});
			EntityChange<Build> change = await buildRepo.GetNewerRevisionAsync(build.Id, 1);
			await task;
			Assert.That(change.Type, Is.EqualTo(EntityChangeType.Update));
			Assert.That(change.Entity.Revision, Is.EqualTo(1));
			Assert.That(change.Entity.PercentCompleted, Is.EqualTo(0.1));
		}

		[Test]
		public async Task GetNewerRevisionAsync_Delete()
		{
			var buildRepo = new MemoryRepository<Build>();
			var build = new Build { EngineRef = "engine1" };
			await buildRepo.InsertAsync(build);
			Task task = Task.Run(async () =>
				{
					await Task.Delay(10);
					await buildRepo.DeleteAsync(build);
				});
			EntityChange<Build> change = await buildRepo.GetNewerRevisionAsync(build.Id, 1);
			await task;
			Assert.That(change.Type, Is.EqualTo(EntityChangeType.Delete));
		}

		[Test]
		public async Task GetNewerRevisionAsync_DoesNotExist()
		{
			var buildRepo = new MemoryRepository<Build>();
			EntityChange<Build> change = await buildRepo.GetNewerRevisionAsync("build1", 1);
			Assert.That(change.Type, Is.EqualTo(EntityChangeType.Delete));
		}
	}
}
