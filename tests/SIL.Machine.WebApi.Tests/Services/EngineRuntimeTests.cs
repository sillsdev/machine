using System;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Services
{
	[TestFixture]
	public class EngineRuntimeTests
	{
		[Test]
		public async Task StartBuildAsync_BatchTrainerCalled()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				env.CreateEngineService();
				Engine engine = await env.CreateEngineAsync();
				EngineRuntime runtime = env.GetRuntime(engine.Id);
				await runtime.InitNewAsync();
				Build build = await runtime.StartBuildAsync();
				Assert.That(build, Is.Not.Null);
				await env.WaitForBuildToFinishAsync(build.Id);
				env.BatchTrainer.Received().Train(Arg.Any<IProgress<ProgressStatus>>(), Arg.Any<Action>());
				env.BatchTrainer.Received().Save();
				build = await env.BuildRepository.GetAsync(build.Id);
				Assert.That(build.State, Is.EqualTo(BuildStates.Completed));
			}
		}

		[Test]
		public async Task CancelBuildAsync_BatchTrainerCalled()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				env.CreateEngineService();
				Engine engine = await env.CreateEngineAsync();
				EngineRuntime runtime = env.GetRuntime(engine.Id);
				await runtime.InitNewAsync();
				env.BatchTrainer.Train(Arg.Any<IProgress<ProgressStatus>>(), Arg.Do<Action>(checkCanceled =>
					{
						while (true)
							checkCanceled();
					}));
				Build build = await runtime.StartBuildAsync();
				Assert.That(build, Is.Not.Null);
				await env.WaitForBuildToStartAsync(build.Id);
				await runtime.CancelBuildAsync();
				await env.WaitForBuildToFinishAsync(build.Id);
				env.BatchTrainer.Received().Train(Arg.Any<IProgress<ProgressStatus>>(), Arg.Any<Action>());
				env.BatchTrainer.DidNotReceive().Save();
				build = await env.BuildRepository.GetAsync(build.Id);
				Assert.That(build.State, Is.EqualTo(BuildStates.Canceled));
			}
		}

		[Test]
		public async Task RestartUnfinishedBuild()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				env.CreateEngineService();
				Engine engine = await env.CreateEngineAsync();
				EngineRuntime runtime = env.GetRuntime(engine.Id);
				await runtime.InitNewAsync();
				env.BatchTrainer.Train(Arg.Any<IProgress<ProgressStatus>>(), Arg.Do<Action>(checkCanceled =>
					{
						while (true)
							checkCanceled();
					}));
				Build build = await runtime.StartBuildAsync();
				Assert.That(build, Is.Not.Null);
				await env.WaitForBuildToStartAsync(build.Id);
				env.DisposeEngineService();
				build = await env.BuildRepository.GetAsync(build.Id);
				Assert.That(build.State, Is.EqualTo(BuildStates.Pending));
				env.CreateEngineService();
				await env.WaitForBuildToFinishAsync(build.Id);
				build = await env.BuildRepository.GetAsync(build.Id);
				Assert.That(build.State, Is.EqualTo(BuildStates.Completed));
			}
		}

		[Test]
		public async Task CommitAsync_LoadedInactive()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				env.MachineOptions.InactiveEngineTimeout = TimeSpan.Zero;
				env.CreateEngineService();
				Engine engine = await env.CreateEngineAsync();
				EngineRuntime runtime = env.GetRuntime(engine.Id);
				await runtime.InitNewAsync();
				await runtime.TrainSegmentPairAsync("esto es una prueba .".Split(), "this is a test .".Split());
				await Task.Delay(10);
				await runtime.CommitAsync();
				env.SmtModel.Received().Save();
				Assert.That(runtime.IsLoaded, Is.False);
			}
		}

		[Test]
		public async Task CommitAsync_LoadedActive()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				env.MachineOptions.InactiveEngineTimeout = TimeSpan.FromHours(1);
				env.CreateEngineService();
				Engine engine = await env.CreateEngineAsync();
				EngineRuntime runtime = env.GetRuntime(engine.Id);
				await runtime.InitNewAsync();
				await runtime.TrainSegmentPairAsync("esto es una prueba .".Split(), "this is a test .".Split());
				await runtime.CommitAsync();
				env.SmtModel.Received().Save();
				Assert.That(runtime.IsLoaded, Is.True);
			}
		}

		[Test]
		public async Task TranslateAsync()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				env.MachineOptions.InactiveEngineTimeout = TimeSpan.FromHours(1);
				env.CreateEngineService();
				Engine engine = await env.CreateEngineAsync();
				EngineRuntime runtime = env.GetRuntime(engine.Id);
				await runtime.InitNewAsync();
				TranslationResult result = await runtime.TranslateAsync("esto es una prueba .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("this is a test .".Split()));
			}
		}
	}
}
