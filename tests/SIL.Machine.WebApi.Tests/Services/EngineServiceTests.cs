using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Options;
using SIL.ObjectModel;
using Xunit;

namespace SIL.Machine.WebApi.Services
{
	public class EngineServiceTests
	{
		[Fact]
		public async Task TranslateAsync_EngineDoesNotExist_ReturnsNull()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				TranslationResult result = await env.Service.TranslateAsync(EngineLocatorType.Id, "engine1",
					"Esto es una prueba .".Split());
				result.Should().BeNull();
			}
		}

		[Fact]
		public async Task TranslateAsync_EngineExists_ReturnsResult()
		{
			using (var env = new TestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				TranslationResult result = await env.Service.TranslateAsync(EngineLocatorType.Id, engineId,
					"Esto es una prueba .".Split());
				result.TargetSegment.Should().Equal("this is a test .".Split());
			}
		}

		[Fact]
		public async Task InteractiveTranslateAsync_EngineDoesNotExist_ReturnsNull()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				InteractiveTranslationResult result = await env.Service.InteractiveTranslateAsync(EngineLocatorType.Id,
					"engine1", "Esto es una prueba .".Split());
				result.Should().BeNull();
			}
		}

		[Fact]
		public async Task InteractiveTranslateAsync_EngineExists_ReturnsResult()
		{
			using (var env = new TestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				InteractiveTranslationResult result = await env.Service.InteractiveTranslateAsync(EngineLocatorType.Id,
					engineId, "Esto es una prueba .".Split());
				result.RuleResult.TargetSegment.Should().Equal("this is a test .".Split());
				result.SmtWordGraph.Arcs.SelectMany(a => a.Words).Should().Equal("this is a test .".Split());
			}
		}

		[Fact]
		public async Task TrainSegmentAsync_EngineDoesNotExist_ReturnsFalse()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				bool result = await env.Service.TrainSegmentAsync(EngineLocatorType.Id, "engine1",
					"Esto es una prueba .".Split(), "This is a test .".Split());
				result.Should().BeFalse();
			}

		}

		[Fact]
		public async Task TrainSegmentAsync_EngineExists_ReturnsTrue()
		{
			using (var env = new TestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				bool result = await env.Service.TrainSegmentAsync(EngineLocatorType.Id, engineId,
					"Esto es una prueba .".Split(), "This is a test .".Split());
				result.Should().BeTrue();
			}
		}

		[Fact]
		public async Task AddProjectAsync_EngineDoesNotExist_EngineCreated()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				(Engine Engine, bool ProjectAdded) result = await env.Service.AddProjectAsync("es", "en", "project1", true);
				result.ProjectAdded.Should().BeTrue();

				Engine engine = await env.EngineRepository.GetAsync(result.Engine.Id);
				engine.Projects.Should().Contain("project1");
			}
		}

		[Fact]
		public async Task AddProjectAsync_SharedEngineExists_ProjectAdded()
		{
			using (var env = new TestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				(Engine Engine, bool ProjectAdded) result = await env.Service.AddProjectAsync("es", "en", "project2", true);
				result.ProjectAdded.Should().BeTrue();

				Engine engine = await env.EngineRepository.GetAsync(result.Engine.Id);
				engine.Id.Should().Be(engineId);
				engine.Projects.Should().Contain("project2");
			}
		}

		[Fact]
		public async Task AddProjectAsync_ProjectEngineExists_EngineCreated()
		{
			using (var env = new TestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", false)).Id;
				env.CreateEngineService();
				(Engine Engine, bool ProjectAdded) result = await env.Service.AddProjectAsync("es", "en", "project2", true);
				result.ProjectAdded.Should().BeTrue();

				Engine engine = await env.EngineRepository.GetAsync(result.Engine.Id);
				engine.Id.Should().NotBe(engineId);
				engine.Projects.Should().Contain("project2");
			}
		}

		[Fact]
		public async Task AddProjectAsync_SharedProjectExists_ReturnsProjectNotAdded()
		{
			using (var env = new TestEnvironment())
			{
				await env.CreateEngineAsync("es", "en", true);
				env.CreateEngineService();
				(Engine Engine, bool ProjectAdded) result = await env.Service.AddProjectAsync("es", "en", "project1", true);
				result.ProjectAdded.Should().BeFalse();
			}
		}

		[Fact]
		public async Task AddProjectAsync_NonsharedProjectExists_ReturnsProjectNotAdded()
		{
			using (var env = new TestEnvironment())
			{
				await env.CreateEngineAsync("es", "en", false);
				env.CreateEngineService();
				(Engine Engine, bool ProjectAdded) result = await env.Service.AddProjectAsync("es", "en", "project1", false);
				result.ProjectAdded.Should().BeFalse();
			}
		}

		[Fact]
		public async Task RemoveProjectAsync_NonsharedProjectExists_EngineRemoved()
		{
			using (var env = new TestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", false)).Id;
				env.CreateEngineService();
				(await env.Service.RemoveProjectAsync("project1")).Should().BeTrue();
				Engine engine = await env.EngineRepository.GetAsync(engineId);
				engine.Should().BeNull();
			}
		}

		[Fact]
		public async Task RemoveProjectAsync_ProjectDoesNotExist_ReturnsFalse()
		{
			using (var env = new TestEnvironment())
			{
				await env.CreateEngineAsync("es", "en", false);
				env.CreateEngineService();
				(await env.Service.RemoveProjectAsync("project3")).Should().BeFalse();
			}
		}

		[Fact]
		public async Task StartBuildAsync_EngineExists_ReturnsSuccess()
		{
			using (var env = new TestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				(Build Build, StartBuildStatus Status) result = await env.Service.StartBuildAsync(EngineLocatorType.Id,
					engineId);
				result.Status.Should().Be(StartBuildStatus.Success);
			}
		}

		[Fact]
		public async Task CancelBuildAsync_ProjectExistsNotBuilding_ReturnsTrue()
		{
			using (var env = new TestEnvironment())
			{
				string engineId = (await env.CreateEngineAsync("es", "en", true)).Id;
				env.CreateEngineService();
				(await env.Service.CancelBuildAsync(BuildLocatorType.Engine, engineId)).Should().BeFalse();
			}
		}

		[Fact]
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
				build.Should().BeNull();
			}
		}

		private class TestEnvironment : DisposableBase
		{
			public TestEnvironment()
			{
				EngineRepository = new MemoryEngineRepository();
				BuildRepository = new MemoryBuildRepository();
			}

			public IEngineRepository EngineRepository { get; }
			public IBuildRepository BuildRepository { get; }
			public EngineService Service { get; private set; }

			public void CreateEngineService()
			{
				Service = new EngineService(CreateOptions(), EngineRepository, BuildRepository, CreateSmtModelFactory(),
					CreateRuleEngineFactory(), CreateTextCorpusFactory());
			}

			public void DisposeEngineService()
			{
				Service?.Dispose();
				Service = null;
			}

			private IOptions<EngineOptions> CreateOptions()
			{
				var options = Substitute.For<IOptions<EngineOptions>>();
				options.Value.Returns(new EngineOptions
				{
					EngineCommitFrequency = TimeSpan.FromMinutes(5),
					InactiveEngineTimeout = TimeSpan.FromMinutes(10)
				});
				return options;
			}

			private ISmtModelFactory CreateSmtModelFactory()
			{
				var factory = Substitute.For<ISmtModelFactory>();
				var smtModel = Substitute.For<IInteractiveSmtModel>();

				var smtEngine = Substitute.For<IInteractiveSmtEngine>();
				var translationResult = new TranslationResult("esto es una prueba .".Split(), "this is a test .".Split(),
					new[] { 1.0, 1.0, 1.0, 1.0, 1.0 }, new[] { TranslationSources.Smt, TranslationSources.Smt, TranslationSources.Smt, TranslationSources.Smt, TranslationSources.Smt },
					new WordAlignmentMatrix(5, 5)
					{
						[0, 0] = AlignmentType.Aligned,
						[1, 1] = AlignmentType.Aligned,
						[2, 2] = AlignmentType.Aligned,
						[3, 3] = AlignmentType.Aligned,
						[4, 4] = AlignmentType.Aligned
					});
				smtEngine.Translate(Arg.Any<IReadOnlyList<string>>()).Returns(translationResult);
				smtEngine.GetWordGraph(Arg.Any<IReadOnlyList<string>>()).Returns(new WordGraph(new[]
				{
					new WordGraphArc(0, 1, 1.0, "this is".Split(), new WordAlignmentMatrix(2, 2) {[0, 0] = AlignmentType.Aligned, [1, 1] = AlignmentType.Aligned}, new[] {1.0, 1.0}, 0, 1, false),
					new WordGraphArc(1, 2, 1.0, "a test".Split(), new WordAlignmentMatrix(2, 2) {[0, 0] = AlignmentType.Aligned, [1, 1] = AlignmentType.Aligned}, new[] {1.0, 1.0}, 2, 3, false),
					new WordGraphArc(2, 3, 1.0, new[] {"."}, new WordAlignmentMatrix(1, 1) {[0, 0] = AlignmentType.Aligned}, new[] {1.0}, 4, 4, false)
				}, new[] { 3 }));
				smtEngine.GetBestPhraseAlignment(Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyList<string>>()).Returns(translationResult);
				smtModel.CreateInteractiveEngine().Returns(smtEngine);

				var batchTrainer = Substitute.For<ISmtBatchTrainer>();
				smtModel.CreateBatchTrainer(Arg.Any<Func<string, string>>(), Arg.Any<ITextCorpus>(), Arg.Any<Func<string, string>>(), Arg.Any<ITextCorpus>(),
					Arg.Any<ITextAlignmentCorpus>()).Returns(batchTrainer);

				factory.Create(Arg.Any<string>()).Returns(smtModel);
				return factory;
			}

			private IRuleEngineFactory CreateRuleEngineFactory()
			{
				var factory = Substitute.For<IRuleEngineFactory>();
				var engine = Substitute.For<ITranslationEngine>();
				engine.Translate(Arg.Any<IReadOnlyList<string>>()).Returns(new TranslationResult("esto es una prueba .".Split(), "this is a test .".Split(),
					new[] { 1.0, 1.0, 1.0, 1.0, 1.0 }, new[] { TranslationSources.Transfer, TranslationSources.Transfer, TranslationSources.Transfer, TranslationSources.Transfer, TranslationSources.Transfer },
					new WordAlignmentMatrix(5, 5)
					{
						[0, 0] = AlignmentType.Aligned,
						[1, 1] = AlignmentType.Aligned,
						[2, 2] = AlignmentType.Aligned,
						[3, 3] = AlignmentType.Aligned,
						[4, 4] = AlignmentType.Aligned
					}));
				factory.Create(Arg.Any<string>()).Returns(engine);
				return factory;
			}

			private ITextCorpusFactory CreateTextCorpusFactory()
			{
				var factory = Substitute.For<ITextCorpusFactory>();
				factory.Create(Arg.Any<IEnumerable<string>>(), Arg.Any<ITokenizer<string, int>>(), Arg.Any<TextCorpusType>())
					.Returns(new DictionaryTextCorpus(Enumerable.Empty<IText>()));
				return factory;
			}

			public async Task<Engine> CreateEngineAsync(string sourceLanguageTag, string targetLanguageTag, bool isShared)
			{
				var engine = new Engine
				{
					SourceLanguageTag = sourceLanguageTag,
					TargetLanguageTag = targetLanguageTag,
					IsShared = isShared,
					Projects = {"project1"}
				};
				await EngineRepository.InsertAsync(engine);
				return engine;
			}

			protected override void DisposeManagedResources()
			{
				DisposeEngineService();
			}
		}
	}
}
