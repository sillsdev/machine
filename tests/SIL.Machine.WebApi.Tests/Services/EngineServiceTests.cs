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
				TranslationResult result = await env.Service.TranslateAsync("engine1", "Esto es una prueba .".Split());
				Assert.That(result, Is.Null);
			}
		}

		[Test]
		public async Task TranslateAsync_EngineExists()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync()).Id;
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
				string engineId = (await env.CreateEngineAsync()).Id;
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
				string engineId = (await env.CreateEngineAsync()).Id;
				env.CreateEngineService();
				bool result = await env.Service.TrainSegmentAsync(engineId, "Esto es una prueba .".Split(),
					"This is a test .".Split(), true);
				Assert.That(result, Is.True);
			}
		}

		[Test]
		public async Task CreateAsync_EngineDoesNotExist()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				env.CreateEngineService();
				var engine = new Engine
				{
					Id = "engine1",
					SourceLanguageTag = "es",
					TargetLanguageTag = "en"
				};
				bool created = await env.Service.CreateAsync(engine);
				Assert.That(created, Is.True);

				engine = await env.EngineRepository.GetAsync("engine1");
				Assert.That(engine.SourceLanguageTag, Is.EqualTo("es"));
				Assert.That(engine.TargetLanguageTag, Is.EqualTo("en"));
			}
		}

		[Test]
		public async Task CreateAsync_EngineExists()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				await env.CreateEngineAsync();
				env.CreateEngineService();
				var engine = new Engine
				{
					Id = "engine1",
					SourceLanguageTag = "es",
					TargetLanguageTag = "en"
				};
				bool created = await env.Service.CreateAsync(engine);
				Assert.That(created, Is.False);
			}
		}

		[Test]
		public async Task DeleteAsync_EngineExists()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync()).Id;
				env.CreateEngineService();
				bool result = await env.Service.DeleteAsync("engine1");
				Assert.That(result, Is.True);
				Engine engine = await env.EngineRepository.GetAsync(engineId);
				Assert.That(engine, Is.Null);
			}
		}

		[Test]
		public async Task DeleteAsync_ProjectDoesNotExist()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				await env.CreateEngineAsync();
				env.CreateEngineService();
				bool result = await env.Service.DeleteAsync("engine3");
				Assert.That(result, Is.False);
			}
		}

		[Test]
		public async Task StartBuildAsync_EngineExists()
		{
			using (var env = new EngineServiceTestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync()).Id;
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
				string engineId = (await env.CreateEngineAsync()).Id;
				env.CreateEngineService();
				await env.Service.CancelBuildAsync(engineId);
			}
		}
	}
}
