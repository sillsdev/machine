using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Features.OwnedInstances;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Server.DataAccess;
using SIL.Machine.WebApi.Server.Models;
using SIL.Machine.WebApi.Server.Options;
using SIL.ObjectModel;
using SIL.Machine.Annotations;

namespace SIL.Machine.WebApi.Server.Services
{
	[TestFixture]
	public class EngineServiceTests
	{
		[Test]
		public async Task TranslateAsync_EngineDoesNotExist_ReturnsNull()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				TranslationResult result = await env.Service.TranslateAsync(EngineLocatorType.Id, "engine1",
					"Esto es una prueba .".Split());
				Assert.That(result, Is.Null);
			}
		}

		[Test]
		public async Task TranslateAsync_EngineExists_ReturnsResult()
		{
			using (var env = new TestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				TranslationResult result = await env.Service.TranslateAsync(EngineLocatorType.Id, engineId,
					"Esto es una prueba .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("this is a test .".Split()));
			}
		}

		[Test]
		public async Task InteractiveTranslateAsync_EngineDoesNotExist_ReturnsNull()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				InteractiveTranslationResult result = await env.Service.InteractiveTranslateAsync(EngineLocatorType.Id,
					"engine1", "Esto es una prueba .".Split());
				Assert.That(result, Is.Null);
			}
		}

		[Test]
		public async Task InteractiveTranslateAsync_EngineExists_ReturnsResult()
		{
			using (var env = new TestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				InteractiveTranslationResult result = await env.Service.InteractiveTranslateAsync(EngineLocatorType.Id,
					engineId, "Esto es una prueba .".Split());
				Assert.That(result.RuleResult.TargetSegment, Is.EqualTo("this is a test .".Split()));
				Assert.That(result.SmtWordGraph.Arcs.SelectMany(a => a.Words), Is.EqualTo("this is a test .".Split()));
			}
		}

		[Test]
		public async Task TrainSegmentAsync_EngineDoesNotExist_ReturnsFalse()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				bool result = await env.Service.TrainSegmentAsync(EngineLocatorType.Id, "engine1",
					"Esto es una prueba .".Split(), "This is a test .".Split());
				Assert.That(result, Is.False);
			}

		}

		[Test]
		public async Task TrainSegmentAsync_EngineExists_ReturnsTrue()
		{
			using (var env = new TestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				bool result = await env.Service.TrainSegmentAsync(EngineLocatorType.Id, engineId,
					"Esto es una prueba .".Split(), "This is a test .".Split());
				Assert.That(result, Is.True);
			}
		}

		[Test]
		public async Task AddProjectAsync_EngineDoesNotExist_EngineCreated()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				Project project = await env.Service.AddProjectAsync("project1", "es", "en", "latin", "latin", true);
				Assert.That(project, Is.Not.Null);

				Engine engine = await env.EngineRepository.GetAsync(project.Engine);
				Assert.That(engine.Projects, Contains.Item("project1"));
			}
		}

		[Test]
		public async Task AddProjectAsync_SharedEngineExists_ProjectAdded()
		{
			using (var env = new TestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				Project project = await env.Service.AddProjectAsync("project2", "es", "en", "latin", "latin", true);
				Assert.That(project, Is.Not.Null);

				Engine engine = await env.EngineRepository.GetAsync(project.Engine);
				Assert.That(engine.Id, Is.EqualTo(engineId));
				Assert.That(engine.Projects, Contains.Item("project2"));
			}
		}

		[Test]
		public async Task AddProjectAsync_ProjectEngineExists_EngineCreated()
		{
			using (var env = new TestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", false)).Id;
				env.CreateEngineService();
				Project project = await env.Service.AddProjectAsync("project2", "es", "en", "latin", "latin", true);
				Assert.That(project, Is.Not.Null);

				Engine engine = await env.EngineRepository.GetAsync(project.Engine);
				Assert.That(engine.Id, Is.Not.EqualTo(engineId));
				Assert.That(engine.Projects, Contains.Item("project2"));
			}
		}

		[Test]
		public async Task AddProjectAsync_SharedProjectExists_ReturnsNull()
		{
			using (var env = new TestEnvironment())
			{
				await env.CreateEngineAsync("es", "en", true);
				env.CreateEngineService();
				Project project = await env.Service.AddProjectAsync("project1", "es", "en", "latin", "latin", true);
				Assert.That(project, Is.Null);
			}
		}

		[Test]
		public async Task AddProjectAsync_NonsharedProjectExists_ReturnsNull()
		{
			using (var env = new TestEnvironment())
			{
				await env.CreateEngineAsync("es", "en", false);
				env.CreateEngineService();
				Project project = await env.Service.AddProjectAsync("project1", "es", "en", "latin", "latin", false);
				Assert.That(project, Is.Null);
			}
		}

		[Test]
		public async Task RemoveProjectAsync_NonsharedProjectExists_EngineRemoved()
		{
			using (var env = new TestEnvironment())
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
		public async Task RemoveProjectAsync_ProjectDoesNotExist_ReturnsFalse()
		{
			using (var env = new TestEnvironment())
			{
				await env.CreateEngineAsync("es", "en", false);
				env.CreateEngineService();
				bool result = await env.Service.RemoveProjectAsync("project3");
				Assert.That(result, Is.False);
			}
		}

		[Test]
		public async Task StartBuildAsync_EngineExists_BuildStarted()
		{
			using (var env = new TestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				Build build = await env.Service.StartBuildAsync(EngineLocatorType.Id, engineId);
				Assert.That(build, Is.Not.Null);
			}
		}

		[Test]
		public async Task CancelBuildAsync_ProjectExistsNotBuilding_ReturnsTrue()
		{
			using (var env = new TestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				bool result = await env.Service.CancelBuildAsync(BuildLocatorType.Engine, engineId);
				Assert.That(result, Is.False);
			}
		}

		[Test]
		public async Task Constructor_UnfinishedBuild_BuildStarted()
		{
			using (var env = new TestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				var build = new Build {Id = "build1", EngineId = engineId};
				await env.BuildRepository.InsertAsync(build);
				env.CreateEngineService();
				// ensures that the build is completed
				env.DisposeEngineService();
				build = await env.BuildRepository.GetAsync("build1");
				Assert.That(build, Is.Null);
			}
		}

		private class TestEnvironment : DisposableBase
		{
			private readonly IOptions<EngineOptions> _engineOptions;
			private readonly ISmtModelFactory _smtModelFactory;
			private readonly IRuleEngineFactory _ruleEngineFactory;
			private readonly ITextCorpusFactory _textCorpusFactory;

			public TestEnvironment()
			{
				EngineRepository = new MemoryEngineRepository();
				BuildRepository = new MemoryBuildRepository();
				ProjectRepository = new MemoryRepository<Project>();
				_engineOptions = new OptionsWrapper<EngineOptions>(new EngineOptions
					{
						EngineCommitFrequency = TimeSpan.FromMinutes(5),
						InactiveEngineTimeout = TimeSpan.FromMinutes(10)
					});
				_smtModelFactory = CreateSmtModelFactory();
				_ruleEngineFactory = CreateRuleEngineFactory();
				_textCorpusFactory = CreateTextCorpusFactory();
			}

			public IEngineRepository EngineRepository { get; }
			public IBuildRepository BuildRepository { get; }
			public IRepository<Project> ProjectRepository { get; }
			public EngineService Service { get; private set; }

			public void CreateEngineService()
			{
				Service = new EngineService(_engineOptions, EngineRepository, BuildRepository,
					ProjectRepository, CreateEngineRunner);
				Service.Init();
			}

			public void DisposeEngineService()
			{
				Service?.Dispose();
				Service = null;
			}

			private Owned<EngineRunner> CreateEngineRunner(string engineId)
			{
				var runner = new EngineRunner(_engineOptions, BuildRepository, _smtModelFactory, _ruleEngineFactory,
					_textCorpusFactory, Substitute.For<ILogger<EngineRunner>>(), engineId);
				return new Owned<EngineRunner>(runner, runner);
			}

			private ISmtModelFactory CreateSmtModelFactory()
			{
				var factory = Substitute.For<ISmtModelFactory>();
				var smtModel = Substitute.For<IInteractiveSmtModel>();

				var smtEngine = Substitute.For<IInteractiveSmtEngine>();
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
						[0, 0] = AlignmentType.Aligned,
						[1, 1] = AlignmentType.Aligned,
						[2, 2] = AlignmentType.Aligned,
						[3, 3] = AlignmentType.Aligned,
						[4, 4] = AlignmentType.Aligned
					},
					new[] { new Phrase(Range<int>.Create(0, 5), 5, 1.0) });
				smtEngine.Translate(Arg.Any<IReadOnlyList<string>>()).Returns(translationResult);
				smtEngine.GetWordGraph(Arg.Any<IReadOnlyList<string>>()).Returns(new WordGraph(new[]
				{
					new WordGraphArc(0, 1, 1.0, "this is".Split(),
						new WordAlignmentMatrix(2, 2) {[0, 0] = AlignmentType.Aligned, [1, 1] = AlignmentType.Aligned},
						new[] {1.0, 1.0}, 0, 1, false),
					new WordGraphArc(1, 2, 1.0, "a test".Split(),
						new WordAlignmentMatrix(2, 2) {[0, 0] = AlignmentType.Aligned, [1, 1] = AlignmentType.Aligned},
						new[] {1.0, 1.0}, 2, 3, false),
					new WordGraphArc(2, 3, 1.0, new[] {"."}, new WordAlignmentMatrix(1, 1) {[0, 0] = AlignmentType.Aligned},
						new[] {1.0}, 4, 4, false)
				}, new[] {3}));
				smtEngine.GetBestPhraseAlignment(Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyList<string>>())
					.Returns(translationResult);
				smtModel.CreateInteractiveEngine().Returns(smtEngine);

				var batchTrainer = Substitute.For<ISmtBatchTrainer>();
				smtModel.CreateBatchTrainer(Arg.Any<Func<string, string>>(), Arg.Any<ITextCorpus>(),
					Arg.Any<Func<string, string>>(), Arg.Any<ITextCorpus>(), Arg.Any<ITextAlignmentCorpus>())
					.Returns(batchTrainer);

				factory.Create(Arg.Any<string>()).Returns(smtModel);
				return factory;
			}

			private IRuleEngineFactory CreateRuleEngineFactory()
			{
				var factory = Substitute.For<IRuleEngineFactory>();
				var engine = Substitute.For<ITranslationEngine>();
				engine.Translate(Arg.Any<IReadOnlyList<string>>()).Returns(new TranslationResult(
					"esto es una prueba .".Split(),
					"this is a test .".Split(),
					new[] {1.0, 1.0, 1.0, 1.0, 1.0},
					new[]
					{
						TranslationSources.Transfer,
						TranslationSources.Transfer,
						TranslationSources.Transfer,
						TranslationSources.Transfer,
						TranslationSources.Transfer
					},
					new WordAlignmentMatrix(5, 5)
					{
						[0, 0] = AlignmentType.Aligned,
						[1, 1] = AlignmentType.Aligned,
						[2, 2] = AlignmentType.Aligned,
						[3, 3] = AlignmentType.Aligned,
						[4, 4] = AlignmentType.Aligned
					},
					new[] { new Phrase(Range<int>.Create(0, 5), 5, 1.0) }));
				factory.Create(Arg.Any<string>()).Returns(engine);
				return factory;
			}

			private ITextCorpusFactory CreateTextCorpusFactory()
			{
				var factory = Substitute.For<ITextCorpusFactory>();
				factory.Create(Arg.Any<IEnumerable<string>>(), Arg.Any<TextCorpusType>())
					.Returns(new DictionaryTextCorpus(Enumerable.Empty<IText>()));
				return factory;
			}

			public async Task<Engine> CreateEngineAsync(string sourceLanguageTag, string targetLanguageTag,
				bool isShared)
			{
				var engine = new Engine
				{
					SourceLanguageTag = sourceLanguageTag,
					TargetLanguageTag = targetLanguageTag,
					IsShared = isShared,
					Projects = {"project1"}
				};
				await EngineRepository.InsertAsync(engine);

				var project = new Project
				{
					Id = "project1",
					SourceLanguageTag = sourceLanguageTag,
					TargetLanguageTag = targetLanguageTag,
					SourceSegmentType = "latin",
					TargetSegmentType = "latin",
					IsShared = isShared,
					Engine = engine.Id
				};
				await ProjectRepository.InsertAsync(project);
				return engine;
			}

			protected override void DisposeManagedResources()
			{
				DisposeEngineService();
			}
		}
	}
}
