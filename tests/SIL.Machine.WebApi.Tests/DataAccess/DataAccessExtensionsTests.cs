using System.Threading.Tasks;
using FluentAssertions;
using SIL.Machine.WebApi.Models;
using Xunit;

namespace SIL.Machine.WebApi.DataAccess
{
	public class DataAccessExtensionsTests
	{
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
			Build newBuild = await buildRepo.GetNewerRevisionAsync(build.Id, 0);
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
			Build newBuild = await buildRepo.GetNewerRevisionAsync(build.Id, 0);
			await task;
			newBuild.Should().BeNull();
		}
	}
}
