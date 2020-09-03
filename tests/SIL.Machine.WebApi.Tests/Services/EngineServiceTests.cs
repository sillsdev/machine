using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Services
{
	[TestFixture]
	public class EngineServiceTests
	{
		[Test]
		public async Task TranslateAsync_EngineDoesNotExist()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				env.CreateEngineService();
				TranslationResult result = await env.Service.TranslateAsync("engine1", "Esto es una prueba .".Split());
				Assert.That(result, Is.Null);
			}
		}

		[Test]
		public async Task TranslateAsync_EngineExists()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				TranslationResult result = await env.Service.TranslateAsync(engineId, "Esto es una prueba .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("this is a TEST .".Split()));
			}
		}

		[Test]
		public async Task GetWordGraphAsync_EngineDoesNotExist()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				env.CreateEngineService();
				WordGraph result = await env.Service.GetWordGraphAsync("engine1", "Esto es una prueba .".Split());
				Assert.That(result, Is.Null);
			}
		}

		[Test]
		public async Task GetWordGraphAsync_EngineExists()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				WordGraph result = await env.Service.GetWordGraphAsync(engineId, "Esto es una prueba .".Split());
				Assert.That(result.Arcs.SelectMany(a => a.Words), Is.EqualTo("this is a TEST .".Split()));
			}
		}

		[Test]
		public async Task TrainSegmentAsync_EngineDoesNotExist()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				env.CreateEngineService();
				bool result = await env.Service.TrainSegmentAsync("engine1", "Esto es una prueba .".Split(),
					"This is a test .".Split(), true);
				Assert.That(result, Is.False);
			}

		}

		[Test]
		public async Task TrainSegmentAsync_EngineExists()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				bool result = await env.Service.TrainSegmentAsync(engineId, "Esto es una prueba .".Split(),
					"This is a test .".Split(), true);
				Assert.That(result, Is.True);
			}
		}

		[Test]
		public async Task AddProjectAsync_EngineDoesNotExist()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				env.CreateEngineService();
				var project = new Project
				{
					Id = "project1",
					SourceLanguageTag = "es",
					TargetLanguageTag = "en",
					SourceSegmentType = "latin",
					TargetSegmentType = "latin",
					IsShared = true
				};
				bool created = await env.Service.AddProjectAsync(project);
				Assert.That(created, Is.True);

				Engine engine = await env.EngineRepository.GetAsync(project.EngineRef);
				Assert.That(engine.Projects, Contains.Item("project1"));
			}
		}

		[Test]
		public async Task AddProjectAsync_SharedEngineExists()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				var project = new Project
				{
					Id = "project2",
					SourceLanguageTag = "es",
					TargetLanguageTag = "en",
					SourceSegmentType = "latin",
					TargetSegmentType = "latin",
					IsShared = true
				};
				bool created = await env.Service.AddProjectAsync(project);
				Assert.That(created, Is.True);

				Engine engine = await env.EngineRepository.GetAsync(project.EngineRef);
				Assert.That(engine.Id, Is.EqualTo(engineId));
				Assert.That(engine.Projects, Contains.Item("project2"));
			}
		}

		[Test]
		public async Task AddProjectAsync_ProjectEngineExists()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", false)).Id;
				env.CreateEngineService();
				var project = new Project
				{
					Id = "project2",
					SourceLanguageTag = "es",
					TargetLanguageTag = "en",
					SourceSegmentType = "latin",
					TargetSegmentType = "latin",
					IsShared = true
				};
				bool created = await env.Service.AddProjectAsync(project);
				Assert.That(created, Is.True);

				Engine engine = await env.EngineRepository.GetAsync(project.EngineRef);
				Assert.That(engine.Id, Is.Not.EqualTo(engineId));
				Assert.That(engine.Projects, Contains.Item("project2"));
			}
		}

		[Test]
		public async Task AddProjectAsync_SharedProjectExists()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				await env.CreateEngineAsync("es", "en", true);
				env.CreateEngineService();
				var project = new Project
				{
					Id = "project1",
					SourceLanguageTag = "es",
					TargetLanguageTag = "en",
					SourceSegmentType = "latin",
					TargetSegmentType = "latin",
					IsShared = true
				};
				bool created = await env.Service.AddProjectAsync(project);
				Assert.That(created, Is.False);
			}
		}

		[Test]
		public async Task AddProjectAsync_NonsharedProjectExists()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				await env.CreateEngineAsync("es", "en", false);
				env.CreateEngineService();
				var project = new Project
				{
					Id = "project1",
					SourceLanguageTag = "es",
					TargetLanguageTag = "en",
					SourceSegmentType = "latin",
					TargetSegmentType = "latin",
					IsShared = false
				};
				bool created = await env.Service.AddProjectAsync(project);
				Assert.That(created, Is.False);
			}
		}

		[Test]
		public async Task RemoveProjectAsync_NonsharedProjectExists()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", false)).Id;
				env.CreateEngineService();
				bool result = await env.Service.RemoveProjectAsync("project1");
				Assert.That(result, Is.True);
				Engine engine = await env.EngineRepository.GetAsync(engineId);
				Assert.That(engine, Is.Null);
			}
		}

		[Test]
		public async Task RemoveProjectAsync_ProjectDoesNotExist()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				await env.CreateEngineAsync("es", "en", false);
				env.CreateEngineService();
				bool result = await env.Service.RemoveProjectAsync("project3");
				Assert.That(result, Is.False);
			}
		}

		[Test]
		public async Task StartBuildAsync_EngineExists()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				Build build = await env.Service.StartBuildAsync(engineId);
				Assert.That(build, Is.Not.Null);
			}
		}

		[Test]
		public async Task CancelBuildAsync_EngineExistsNotBuilding()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				await env.Service.CancelBuildAsync(engineId);
			}
		}
	}
}
