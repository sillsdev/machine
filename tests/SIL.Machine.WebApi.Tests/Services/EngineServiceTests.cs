using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NSubstitute;
using SIL.IO;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Services;
using Xunit;
using FluentAssertions;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.WebApi.Options;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Tests.Services
{
	public class EngineServiceTests
	{
		[Fact]
		public async Task GetAllLanguagePairsAsync_NoLanguagePairs_ReturnsEmpty()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				(await env.Service.GetLanguagePairsAsync()).Should().BeEmpty();
			}
		}

		[Fact]
		public async Task GetAllLanguagePairsAsync_HasLanguagePairs_ReturnsLanguagePairDtos()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateLanguagePair("es", "en");
				env.CreateLanguagePair("fr", "en");
				env.CreateEngineService();
				(await env.Service.GetLanguagePairsAsync()).Select(e => $"{e.SourceLanguageTag}_{e.TargetLanguageTag}").Should().BeEquivalentTo("es_en", "fr_en");
			}
		}

		[Fact]
		public async Task GetAllProjectsAsync_LanguagePairDoesNotExist_ReturnsNull()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				(await env.Service.GetProjectsAsync("es", "en")).Should().BeNull();
			}
		}

		[Fact]
		public async Task GetAllProjectsAsync_LanguagePairExists_ReturnsProjectDtos()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateLanguagePair("es", "en");
				env.CreateEngineService();
				IReadOnlyCollection<ProjectDto> projects = await env.Service.GetProjectsAsync("es", "en");
				projects.Select(p => p.Id).Should().BeEquivalentTo("project1", "project2");
			}
		}

		[Fact]
		public async Task GetLanguagePairAsync_LanguagePairDoesNotExist_ReturnsNull()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				(await env.Service.GetLanguagePairAsync("es", "en")).Should().BeNull();
			}
		}

		[Fact]
		public async Task GetLanguagePairAsync_LanguagePairExists_ReturnsLanguagePairDto()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateLanguagePair("es", "en");
				env.CreateEngineService();
				LanguagePairDto languagePair = await env.Service.GetLanguagePairAsync("es", "en");
				languagePair.SourceLanguageTag.Should().Be("es");
				languagePair.TargetLanguageTag.Should().Be("en");
			}
		}

		[Fact]
		public async Task GetProjectAsync_LanguagePairDoesNotExist_ReturnsNull()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				(await env.Service.GetProjectAsync("es", "en", "project1")).Should().BeNull();
			}
		}

		[Fact]
		public async Task GetProjectAsync_ProjectDoesNotExist_ReturnsNull()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateLanguagePair("es", "en");
				env.CreateEngineService();
				(await env.Service.GetProjectAsync("es", "en", "project3")).Should().BeNull();
			}
		}

		[Fact]
		public async Task GetProjectAsync_ProjectExists_ReturnsProjectDto()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateLanguagePair("es", "en");
				env.CreateEngineService();
				ProjectDto project = await env.Service.GetProjectAsync("es", "en", "project1");
				project.Id.Should().Be("project1");
			}
		}

		[Fact]
		public async Task TranslateAsync_LanguagePairDoesNotExist_ReturnsNull()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				(await env.Service.TranslateAsync("es", "en", null, "Esto es una prueba .".Split())).Should().BeNull();
			}
		}

		[Fact]
		public async Task TranslateAsync_SharedEngine_ReturnsResultDto()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateLanguagePair("es", "en");
				env.CreateEngineService();
				TranslationResultDto result = await env.Service.TranslateAsync("es", "en", null, "Esto es una prueba .".Split());
				result.Target.Should().Equal("This is a test .".Split());
			}
		}

		[Fact]
		public async Task TranslateAsync_ProjectEngine_ReturnsResultDto()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateLanguagePair("es", "en");
				env.CreateEngineService();
				TranslationResultDto result = await env.Service.TranslateAsync("es", "en", "project2", "Esto es una prueba .".Split());
				result.Target.Should().Equal("This is a test .".Split());
			}
		}

		[Fact]
		public async Task InteractiveTranslateAsync_LanguagePairDoesNotExist_ReturnsNull()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				(await env.Service.InteractiveTranslateAsync("es", "en", null, "Esto es una prueba .".Split())).Should().BeNull();
			}
		}

		[Fact]
		public async Task InteractiveTranslateAsync_SharedEngine_ReturnsResultDto()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateLanguagePair("es", "en");
				env.CreateEngineService();
				InteractiveTranslationResultDto result = await env.Service.InteractiveTranslateAsync("es", "en", null, "Esto es una prueba .".Split());
				result.RuleResult.Target.Should().Equal("This is a test .".Split());
				result.WordGraph.Arcs.SelectMany(a => a.Words).Should().Equal("This is a test .".Split());
			}
		}

		[Fact]
		public async Task InteractiveTranslateAsync_ProjectEngine_ReturnsResultDto()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateLanguagePair("es", "en");
				env.CreateEngineService();
				InteractiveTranslationResultDto result = await env.Service.InteractiveTranslateAsync("es", "en", "project2", "Esto es una prueba .".Split());
				result.RuleResult.Target.Should().Equal("This is a test .".Split());
				result.WordGraph.Arcs.SelectMany(a => a.Words).Should().Equal("This is a test .".Split());
			}
		}

		[Fact]
		public async Task TrainSegmentAsync_LanguagePairDoesNotExist_ReturnsFalse()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				var pairDto = new SegmentPairDto
				{
					SourceSegment = "Esto es una prueba .".Split(),
					TargetSegment = "This is a test .".Split()
				};
				(await env.Service.TrainSegmentAsync("es", "en", null, pairDto)).Should().BeFalse();
			}

		}

		[Fact]
		public async Task TrainSegmentAsync_SharedEngine_ReturnsTrue()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateLanguagePair("es", "en");
				env.CreateEngineService();
				var pairDto = new SegmentPairDto
				{
					SourceSegment = "Esto es una prueba .".Split(),
					TargetSegment = "This is a test .".Split()
				};
				(await env.Service.TrainSegmentAsync("es", "en", null, pairDto)).Should().BeTrue();
			}
		}

		[Fact]
		public async Task TrainSegmentAsync_ProjectEngine_ReturnsTrue()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateLanguagePair("es", "en");
				env.CreateEngineService();
				var pairDto = new SegmentPairDto
				{
					SourceSegment = "Esto es una prueba .".Split(),
					TargetSegment = "This is a test .".Split()
				};
				(await env.Service.TrainSegmentAsync("es", "en", "project2", pairDto)).Should().BeTrue();
			}
		}

		[Fact]
		public async Task AddProjectAsync_LanguagePairDoesNotExist_ProjectAdded()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				await env.Service.AddProjectAsync("es", "en", new ProjectDto {Id = "project1", IsShared = true});
				ProjectDto project = await env.Service.GetProjectAsync("es", "en", "project1");
				project.Should().NotBeNull();
			}
		}

		[Fact]
		public async Task AddProjectAsync_LanguagePairExists_ProjectAdded()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateLanguagePair("es", "en");
				env.CreateEngineService();
				await env.Service.AddProjectAsync("es", "en", new ProjectDto {Id = "project3", IsShared = true});
				ProjectDto project = await env.Service.GetProjectAsync("es", "en", "project3");
				project.Should().NotBeNull();
			}
		}

		[Fact]
		public async Task AddProjectAsync_ProjectExists_NothingChanged()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				env.CreateLanguagePair("es", "en");
				await env.Service.AddProjectAsync("es", "en", new ProjectDto {Id = "project1", IsShared = true});
				ProjectDto project = await env.Service.GetProjectAsync("es", "en", "project1");
				project.Should().NotBeNull();
			}
		}

		[Fact]
		public async Task RemoveProjectAsync_ProjectExists_ProjectRemoved()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateLanguagePair("es", "en");
				env.CreateEngineService();
				(await env.Service.RemoveProjectAsync("es", "en", "project1")).Should().BeTrue();
				ProjectDto project = await env.Service.GetProjectAsync("es", "en", "project1");
				project.Should().BeNull();
			}
		}

		[Fact]
		public async Task RemoveProjectAsync_ProjectDoesNotExist_ReturnsFalse()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateLanguagePair("es", "en");
				env.CreateEngineService();
				(await env.Service.RemoveProjectAsync("es", "en", "project3")).Should().BeFalse();
			}
		}

		[Fact]
		public async Task RemoveProjectAsync_LanguagePairDoesNotExist_ReturnsFalse()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngineService();
				(await env.Service.RemoveProjectAsync("es", "en", "project2")).Should().BeFalse();
			}
		}

		[Fact]
		public async Task StartRebuildAsync_ProjectExists_ReturnsTrue()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateLanguagePair("es", "en");
				env.CreateEngineService();
				(await env.Service.StartRebuildAsync("es", "en", "project1")).Should().BeTrue();
			}
		}

		[Fact]
		public async Task CancelRebuildAsync_ProjectExists_ReturnsTrue()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateLanguagePair("es", "en");
				env.CreateEngineService();
				(await env.Service.CancelRebuildAsync("es", "en", "project1")).Should().BeTrue();
			}
		}

		private class TestEnvironment : DisposableBase
		{
			private readonly TempDirectory _tempDir;

			public TestEnvironment()
			{
				_tempDir = new TempDirectory("EngineServiceTests");
			}

			public EngineService Service { get; private set; }

			public void CreateEngineService()
			{
				Service = new EngineService(CreateOptions(), CreateSmtModelFactory(), CreateRuleEngineFactory(), CreateTextCorpusFactory());
			}

			private IOptions<EngineOptions> CreateOptions()
			{
				var options = Substitute.For<IOptions<EngineOptions>>();
				options.Value.Returns(new EngineOptions
				{
					EngineCommitFrequency = TimeSpan.FromMinutes(5),
					InactiveEngineTimeout = TimeSpan.FromMinutes(10),
					RootDir = _tempDir.Path
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
				factory.Create(Arg.Any<IEnumerable<Project>>(), Arg.Any<ITokenizer<string, int>>(), Arg.Any<TextCorpusType>())
					.Returns(new DictionaryTextCorpus(Enumerable.Empty<IText>()));
				return factory;
			}

			public void CreateLanguagePair(string sourceLanguageTag, string targetLanguageTag)
			{
				string configDir = Path.Combine(_tempDir.Path, $"{sourceLanguageTag}_{targetLanguageTag}");
				Directory.CreateDirectory(configDir);
				string json = JsonConvert.SerializeObject(new LanguagePairConfig
				{
					SourceLanguageTag = sourceLanguageTag,
					TargetLanguageTag = targetLanguageTag,
					Projects = new[] {new ProjectConfig {Id = "project1", IsShared = true}, new ProjectConfig {Id = "project2", IsShared = false}}
				});
				File.WriteAllText(Path.Combine(configDir, "config.json"), json);
				CreateEngineDirectory(Path.Combine(configDir, "shared-engine"));
				CreateEngineDirectory(Path.Combine(configDir, "project2"));
			}

			private void CreateEngineDirectory(string dir)
			{
				Directory.CreateDirectory(dir);
				File.WriteAllText(Path.Combine(dir, "config.json"), JsonConvert.SerializeObject(new EngineConfig()));
			}

			protected override void DisposeManagedResources()
			{
				Service?.Dispose();
				_tempDir.Dispose();
			}
		}
	}
}
