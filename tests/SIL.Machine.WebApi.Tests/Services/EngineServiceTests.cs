namespace SIL.Machine.WebApi.Services;

[TestFixture]
public class EngineServiceTests
{
	[Test]
	public async Task TranslateAsync_EngineDoesNotExist()
	{
		using var env = new TestEnvironment();
		TranslationResult? result = await env.Service.TranslateAsync("engine1", "Esto es una prueba .".Split());
		Assert.That(result, Is.Null);
	}

	[Test]
	public async Task TranslateAsync_EngineExists()
	{
		using var env = new TestEnvironment();
		string engineId = (await env.CreateEngineAsync()).Id;
		TranslationResult? result = await env.Service.TranslateAsync(engineId, "Esto es una prueba .".Split());
		Assert.That(result, Is.Not.Null);
		Assert.That(result!.TargetSegment, Is.EqualTo("this is a test .".Split()));
	}

	[Test]
	public async Task GetWordGraphAsync_EngineDoesNotExist()
	{
		using var env = new TestEnvironment();
		WordGraph? result = await env.Service.GetWordGraphAsync("engine1", "Esto es una prueba .".Split());
		Assert.That(result, Is.Null);
	}

	[Test]
	public async Task GetWordGraphAsync_EngineExists()
	{
		using var env = new TestEnvironment();
		string engineId = (await env.CreateEngineAsync()).Id;
		WordGraph? result = await env.Service.GetWordGraphAsync(engineId, "Esto es una prueba .".Split());
		Assert.That(result, Is.Not.Null);
		Assert.That(result!.Arcs.SelectMany(a => a.Words), Is.EqualTo("this is a test .".Split()));
	}

	[Test]
	public async Task TrainSegmentAsync_EngineDoesNotExist()
	{
		using var env = new TestEnvironment();
		bool result = await env.Service.TrainSegmentAsync("engine1", "Esto es una prueba .".Split(),
			"This is a test .".Split(), true);
		Assert.That(result, Is.False);

	}

	[Test]
	public async Task TrainSegmentAsync_EngineExists()
	{
		using var env = new TestEnvironment();
		string engineId = (await env.CreateEngineAsync()).Id;
		bool result = await env.Service.TrainSegmentAsync(engineId, "Esto es una prueba .".Split(),
			"This is a test .".Split(), true);
		Assert.That(result, Is.True);
	}

	[Test]
	public async Task CreateAsync_EngineDoesNotExist()
	{
		using var env = new TestEnvironment();
		var engine = new Engine
		{
			Id = "engine1",
			SourceLanguageTag = "es",
			TargetLanguageTag = "en",
			Type = "test"
		};
		bool created = await env.Service.CreateAsync(engine);
		Assert.That(created, Is.True);

		engine = (await env.Engines.GetAsync("engine1"))!;
		Assert.That(engine.SourceLanguageTag, Is.EqualTo("es"));
		Assert.That(engine.TargetLanguageTag, Is.EqualTo("en"));
	}

	[Test]
	public async Task CreateAsync_EngineExists()
	{
		using var env = new TestEnvironment();
		await env.CreateEngineAsync();
		var engine = new Engine
		{
			Id = "engine1",
			SourceLanguageTag = "es",
			TargetLanguageTag = "en",
			Type = "test"
		};
		bool created = await env.Service.CreateAsync(engine);
		Assert.That(created, Is.False);
	}

	[Test]
	public async Task DeleteAsync_EngineExists()
	{
		using var env = new TestEnvironment();
		string engineId = (await env.CreateEngineAsync()).Id;
		bool result = await env.Service.DeleteAsync("engine1");
		Assert.That(result, Is.True);
		Engine? engine = await env.Engines.GetAsync(engineId);
		Assert.That(engine, Is.Null);
	}

	[Test]
	public async Task DeleteAsync_ProjectDoesNotExist()
	{
		using var env = new TestEnvironment();
		await env.CreateEngineAsync();
		bool result = await env.Service.DeleteAsync("engine3");
		Assert.That(result, Is.False);
	}

	[Test]
	public async Task StartBuildAsync_EngineExists()
	{
		using var env = new TestEnvironment();
		string engineId = (await env.CreateEngineAsync()).Id;
		Build? build = await env.Service.StartBuildAsync(engineId);
		Assert.That(build, Is.Not.Null);
	}

	[Test]
	public async Task CancelBuildAsync_EngineExistsNotBuilding()
	{
		using var env = new TestEnvironment();
		string engineId = (await env.CreateEngineAsync()).Id;
		await env.Service.CancelBuildAsync(engineId);
	}

	private class TestEnvironment : DisposableBase
	{
		public TestEnvironment()
		{
			Engines = new MemoryRepository<Engine>();
			var engineRuntime = Substitute.For<IEngineRuntime>();
			var translationResult = new TranslationResult("esto es una prueba .".Split(),
				"this is a test .".Split(),
				new[] { 1.0, 1.0, 1.0, 1.0, 1.0 },
				new[]
				{
				TranslationSources.Smt,
				TranslationSources.Smt,
				TranslationSources.Smt,
				TranslationSources.Smt,
				TranslationSources.Smt
				},
				new WordAlignmentMatrix(5, 5)
				{
					[0, 0] = true,
					[1, 1] = true,
					[2, 2] = true,
					[3, 3] = true,
					[4, 4] = true
				},
				new[] { new Phrase(Range<int>.Create(0, 5), 5, 1.0) });
			engineRuntime.TranslateAsync(Arg.Any<IReadOnlyList<string>>()).Returns(Task.FromResult(translationResult));
			var wordGraph = new WordGraph(new[]
			{
				new WordGraphArc(0, 1, 1.0, "this is".Split(),
					new WordAlignmentMatrix(2, 2)
					{
						[0, 0] = true, [1, 1] = true
					},
					Range<int>.Create(0, 2), GetSources(2, false), new[] { 1.0, 1.0 }),
				new WordGraphArc(1, 2, 1.0, "a test".Split(),
					new WordAlignmentMatrix(2, 2)
					{
						[0, 0] = true, [1, 1] = true
					},
					Range<int>.Create(2, 4), GetSources(2, false), new[] { 1.0, 1.0 }),
				new WordGraphArc(2, 3, 1.0, new[] { "." },
					new WordAlignmentMatrix(1, 1) { [0, 0] = true },
					Range<int>.Create(4, 5), GetSources(1, false), new[] { 1.0 })
			}, new[] { 3 });
			engineRuntime.GetWordGraphAsync(Arg.Any<IReadOnlyList<string>>()).Returns(Task.FromResult(wordGraph));
			engineRuntime.StartBuildAsync().Returns(Task.FromResult(new Build { Id = "build1", EngineRef = "engine1" }));
			var engineRuntimeFactory = Substitute.For<IEngineRuntimeFactory>();
			engineRuntimeFactory.Key.Returns("test");
			engineRuntimeFactory.CreateEngineRuntime(Arg.Any<string>()).Returns(engineRuntime);
			Service = new EngineService(new OptionsWrapper<EngineOptions>(new EngineOptions()), Engines,
				new MemoryRepository<Build>(), new[] { engineRuntimeFactory });
		}

		public EngineService Service { get; }
		public IRepository<Engine> Engines { get; }

		public async Task<Engine> CreateEngineAsync()
		{
			var engine = new Engine
			{
				Id = "engine1",
				SourceLanguageTag = "es",
				TargetLanguageTag = "en",
				Type = "test"
			};
			await Engines.InsertAsync(engine);
			return engine;
		}

		private static IEnumerable<TranslationSources> GetSources(int count, bool isUnknown)
		{
			var sources = new TranslationSources[count];
			for (int i = 0; i < count; i++)
				sources[i] = isUnknown ? TranslationSources.None : TranslationSources.Smt;
			return sources;
		}

		protected override void DisposeManagedResources()
		{
			Service.Dispose();
		}
	}
}
