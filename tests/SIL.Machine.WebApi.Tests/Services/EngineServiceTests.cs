using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.DataAccess;
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
				TranslationResult result = await env.Service.TranslateAsync(EngineLocatorType.Id, "engine1",
					"Esto es una prueba .".Split());
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
				TranslationResult result = await env.Service.TranslateAsync(EngineLocatorType.Id, engineId,
					"Esto es una prueba .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("this is a test .".Split()));
			}
		}

		[Test]
		public async Task InteractiveTranslateAsync_EngineDoesNotExist()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				env.CreateEngineService();
				HybridInteractiveTranslationResult result = await env.Service.InteractiveTranslateAsync(
					EngineLocatorType.Id, "engine1", "Esto es una prueba .".Split());
				Assert.That(result, Is.Null);
			}
		}

		[Test]
		public async Task InteractiveTranslateAsync_EngineExists()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				HybridInteractiveTranslationResult result = await env.Service.InteractiveTranslateAsync(
					EngineLocatorType.Id, engineId, "Esto es una prueba .".Split());
				Assert.That(result.RuleResult.TargetSegment, Is.EqualTo("this is a test .".Split()));
				Assert.That(result.SmtWordGraph.Arcs.SelectMany(a => a.Words), Is.EqualTo("this is a test .".Split()));
			}
		}

		[Test]
		public async Task TrainSegmentAsync_EngineDoesNotExist()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				env.CreateEngineService();
				bool result = await env.Service.TrainSegmentAsync(EngineLocatorType.Id, "engine1",
					"Esto es una prueba .".Split(), "This is a test .".Split());
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
				bool result = await env.Service.TrainSegmentAsync(EngineLocatorType.Id, engineId,
					"Esto es una prueba .".Split(), "This is a test .".Split());
				Assert.That(result, Is.True);
			}
		}

		[Test]
		public async Task AddProjectAsync_EngineDoesNotExist()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				env.CreateEngineService();
				Project project = await env.Service.AddProjectAsync("project1", "es", "en", "latin", "latin", true);
				Assert.That(project, Is.Not.Null);

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
				Project project = await env.Service.AddProjectAsync("project2", "es", "en", "latin", "latin", true);
				Assert.That(project, Is.Not.Null);

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
				Project project = await env.Service.AddProjectAsync("project2", "es", "en", "latin", "latin", true);
				Assert.That(project, Is.Not.Null);

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
				Project project = await env.Service.AddProjectAsync("project1", "es", "en", "latin", "latin", true);
				Assert.That(project, Is.Null);
			}
		}

		[Test]
		public async Task AddProjectAsync_NonsharedProjectExists()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				await env.CreateEngineAsync("es", "en", false);
				env.CreateEngineService();
				Project project = await env.Service.AddProjectAsync("project1", "es", "en", "latin", "latin", false);
				Assert.That(project, Is.Null);
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
				Build build = await env.Service.StartBuildAsync(EngineLocatorType.Id, engineId);
				Assert.That(build, Is.Not.Null);
			}
		}

		[Test]
		public async Task CancelBuildAsync_ProjectExistsNotBuilding()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				bool result = await env.Service.CancelBuildAsync(BuildLocatorType.Engine, engineId);
				Assert.That(result, Is.False);
			}
		}
	}
}
