using System.Threading.Tasks;
using FluentAssertions;
using SIL.Machine.WebApi.Server.Models;
using Xunit;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	public class DataAccessExtensionsTests
	{
		[Fact]
		public async Task GetNewerRevisionByEngineIdAsync_Insert_ReturnsInsertedEntity()
		{
			var buildRepo = new MemoryBuildRepository();
			Task task = Task.Run(async () =>
			{
				await Task.Delay(10);
				var build = new Build {EngineId = "engine1", CurrentStep = 1};
				await buildRepo.InsertAsync(build);
			});
			Build newBuild = await buildRepo.GetNewerRevisionByEngineIdAsync("engine1", 0, true);
			await task;
			newBuild.Revision.Should().Be(0);
			newBuild.CurrentStep.Should().Be(1);
		}

		[Fact]
		public async Task GetNewerRevisionAsync_Update_ReturnsUpdatedEntity()
		{
			var buildRepo = new MemoryBuildRepository();
			var build = new Build {EngineId = "engine1"};
			await buildRepo.InsertAsync(build);
			Task task = Task.Run(async () =>
				{
					await Task.Delay(10);
					build.CurrentStep = 1;
					buildRepo.Update(build);
				});
			Build newBuild = await buildRepo.GetNewerRevisionAsync(build.Id, 1, false);
			await task;
			newBuild.Revision.Should().Be(1);
			newBuild.CurrentStep.Should().Be(1);
		}

		[Fact]
		public async Task GetNewerRevisionAsync_Delete_ReturnsNull()
		{
			var buildRepo = new MemoryBuildRepository();
			var build = new Build {EngineId = "engine1"};
			await buildRepo.InsertAsync(build);
			Task task = Task.Run(async () =>
				{
					await Task.Delay(10);
					buildRepo.Delete(build);
				});
			Build newBuild = await buildRepo.GetNewerRevisionAsync(build.Id, 1, false);
			await task;
			newBuild.Should().BeNull();
		}
	}
}
