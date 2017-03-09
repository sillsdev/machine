using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NSubstitute;
using SIL.IO;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Services;
using Xunit;
using FluentAssertions;

namespace SIL.Machine.WebApi.Tests.Services
{
	public class EngineServiceTests
	{
		[Fact]
		public void GetAllLanguagePairs_NoLanguagePairs_ReturnsEmpty()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				service.GetAllLanguagePairs().Should().BeEmpty();
			}
		}

		[Fact]
		public void GetAllLanguagePairs_HasLanguagePairs_ReturnsLanguagePairDtos()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				CreateLanguagePair(tempDir.Path, "es", "en");
				CreateLanguagePair(tempDir.Path, "fr", "en");
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				service.GetAllLanguagePairs().Select(e => $"{e.SourceLanguageTag}_{e.TargetLanguageTag}").Should().BeEquivalentTo(new[] { "es_en", "fr_en" });
			}
		}

		[Fact]
		public void GetAllProjects_LanguagePairDoesNotExist_ReturnsFalse()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				IReadOnlyList<ProjectDto> projects;
				service.GetAllProjects("es", "en", out projects).Should().BeFalse();
			}
		}

		[Fact]
		public void GetAllProjects_LanguagePairExists_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				CreateLanguagePair(tempDir.Path, "es", "en");
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				IReadOnlyList<ProjectDto> projects;
				service.GetAllProjects("es", "en", out projects).Should().BeTrue();
				projects.Select(p => p.Id).Should().BeEquivalentTo(new[] {"project1", "project2"});
			}
		}

		[Fact]
		public void TryGetLanguagePair_LanguagePairDoesNotExist_ReturnsFalse()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				LanguagePairDto languagePair;
				service.TryGetLanguagePair("es", "en", out languagePair).Should().BeFalse();
			}
		}

		[Fact]
		public void TryGetLanguagePair_LanguagePairExists_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				CreateLanguagePair(tempDir.Path, "es", "en");
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				LanguagePairDto languagePair;
				service.TryGetLanguagePair("es", "en", out languagePair).Should().BeTrue();
				languagePair.SourceLanguageTag.Should().Be("es");
				languagePair.TargetLanguageTag.Should().Be("en");
			}
		}

		[Fact]
		public void TryGetProject_LanguagePairDoesNotExist_ReturnsFalse()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				ProjectDto project;
				service.TryGetProject("es", "en", "project1", out project).Should().BeFalse();
			}
		}

		[Fact]
		public void TryGetProject_ProjectDoesNotExist_ReturnsFalse()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				CreateLanguagePair(tempDir.Path, "es", "en");
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				ProjectDto project;
				service.TryGetProject("es", "en", "project3", out project).Should().BeFalse();
			}
		}

		[Fact]
		public void TryGetProject_ProjectExists_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				CreateLanguagePair(tempDir.Path, "es", "en");
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				ProjectDto project;
				service.TryGetProject("es", "en", "project1", out project).Should().BeTrue();
				project.Id.Should().Be("project1");
			}
		}

		[Fact]
		public void TryTranslate_LanguagePairDoesNotExist_ReturnsFalse()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				IReadOnlyList<string> result;
				service.TryTranslate("es", "en", null, "Esto es una prueba .".Split(), out result).Should().BeFalse();
			}
		}

		[Fact]
		public void TryTranslate_SharedEngine_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				CreateLanguagePair(tempDir.Path, "es", "en");
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				IReadOnlyList<string> result;
				service.TryTranslate("es", "en", null, "Esto es una prueba .".Split(), out result).Should().BeTrue();
				result.Should().Equal("This is a test .".Split());
			}
		}

		[Fact]
		public void TryTranslate_ProjectEngine_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				CreateLanguagePair(tempDir.Path, "es", "en");
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				IReadOnlyList<string> result;
				service.TryTranslate("es", "en", "project2", "Esto es una prueba .".Split(), out result).Should().BeTrue();
				result.Should().Equal("This is a test .".Split());
			}
		}

		[Fact]
		public void TryInteractiveTranslate_LanguagePairDoesNotExist_ReturnsFalse()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				InteractiveTranslationResultDto result;
				service.TryInteractiveTranslate("es", "en", null, "Esto es una prueba .".Split(), out result).Should().BeFalse();
			}
		}

		[Fact]
		public void TryInteractiveTranslate_SharedEngine_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				CreateLanguagePair(tempDir.Path, "es", "en");
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				InteractiveTranslationResultDto result;
				service.TryInteractiveTranslate("es", "en", null, "Esto es una prueba .".Split(), out result).Should().BeTrue();
				result.RuleResult.Target.Should().Equal("This is a test .".Split());
				result.WordGraph.Arcs.SelectMany(a => a.Words).Should().Equal("This is a test .".Split());
			}
		}

		[Fact]
		public void TryInteractiveTranslate_ProjectEngine_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				CreateLanguagePair(tempDir.Path, "es", "en");
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				InteractiveTranslationResultDto result;
				service.TryInteractiveTranslate("es", "en", "project2", "Esto es una prueba .".Split(), out result).Should().BeTrue();
				result.RuleResult.Target.Should().Equal("This is a test .".Split());
				result.WordGraph.Arcs.SelectMany(a => a.Words).Should().Equal("This is a test .".Split());
			}
		}

		[Fact]
		public void TryTrainSegment_LanguagePairDoesNotExist_ReturnsFalse()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				var pairDto = new SegmentPairDto
				{
					SourceSegment = "Esto es una prueba .".Split(),
					TargetSegment = "This is a test .".Split()
				};
				service.TryTrainSegment("es", "en", null, pairDto).Should().BeFalse();
			}
		}

		[Fact]
		public void TryTrainSegment_SharedEngine_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				CreateLanguagePair(tempDir.Path, "es", "en");
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				var pairDto = new SegmentPairDto
				{
					SourceSegment = "Esto es una prueba .".Split(),
					TargetSegment = "This is a test .".Split()
				};
				service.TryTrainSegment("es", "en", null, pairDto).Should().BeTrue();
			}
		}

		[Fact]
		public void TryTrainSegment_ProjectEngine_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				CreateLanguagePair(tempDir.Path, "es", "en");
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				var pairDto = new SegmentPairDto
				{
					SourceSegment = "Esto es una prueba .".Split(),
					TargetSegment = "This is a test .".Split()
				};
				service.TryTrainSegment("es", "en", "project2", pairDto).Should().BeTrue();
			}
		}

		[Fact]
		public void AddProject_LanguagePairDoesNotExist_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				service.AddProject("es", "en", new ProjectDto {Id = "project1", IsShared = true});
				ProjectDto project;
				service.TryGetProject("es", "en", "project1", out project).Should().BeTrue();
			}
		}

		[Fact]
		public void AddProject_LanguagePairExists_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				CreateLanguagePair(tempDir.Path, "es", "en");
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				service.AddProject("es", "en", new ProjectDto {Id = "project3", IsShared = true});
				ProjectDto project;
				service.TryGetProject("es", "en", "project3", out project).Should().BeTrue();
			}
		}

		[Fact]
		public void AddProject_ProjectExists_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				CreateLanguagePair(tempDir.Path, "es", "en");
				service.AddProject("es", "en", new ProjectDto {Id = "project1", IsShared = true});
				ProjectDto project;
				service.TryGetProject("es", "en", "project1", out project).Should().BeTrue();
			}
		}

		[Fact]
		public void RemoveProject_ProjectExists_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				CreateLanguagePair(tempDir.Path, "es", "en");
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				service.RemoveProject("es", "en", "project1").Should().BeTrue();
				ProjectDto project;
				service.TryGetProject("es", "en", "project1", out project).Should().BeFalse();
			}
		}

		[Fact]
		public void RemoveProject_ProjectDoesNotExist_ReturnsFalse()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				CreateLanguagePair(tempDir.Path, "es", "en");
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				service.RemoveProject("es", "en", "project3").Should().BeFalse();
			}
		}

		[Fact]
		public void RemoveProject_LanguagePairDoesNotExist_ReturnsFalse()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				service.RemoveProject("es", "en", "project2").Should().BeFalse();
			}
		}

		private void CreateLanguagePair(string rootDir, string sourceLanguageTag, string targetLanguageTag)
		{
			string configDir = Path.Combine(rootDir, $"{sourceLanguageTag}_{targetLanguageTag}");
			Directory.CreateDirectory(configDir);
			string json = JsonConvert.SerializeObject(new LanguagePairDto
				{
					SourceLanguageTag = sourceLanguageTag,
					TargetLanguageTag = targetLanguageTag,
					Projects = new[] {new ProjectDto {Id = "project1", IsShared = true}, new ProjectDto {Id = "project2", IsShared = false}}
				});
			File.WriteAllText(Path.Combine(configDir, "config.json"), json);
		}

		private IOptions<EngineOptions> CreateOptions(string rootDir)
		{
			var options = Substitute.For<IOptions<EngineOptions>>();
			options.Value.Returns(new EngineOptions
				{
					EngineCommitFrequency = TimeSpan.FromMinutes(5),
					InactiveEngineTimeout = TimeSpan.FromMinutes(10),
					RootDir = rootDir
				});
			return options;
		}

		private ISmtModelFactory CreateSmtModelFactory()
		{
			var factory = Substitute.For<ISmtModelFactory>();
			var smtModel = Substitute.For<IInteractiveSmtModel>();
			var smtEngine = Substitute.For<IInteractiveSmtEngine>();
			var translationResult = new TranslationResult("esto es una prueba .".Split(), "this is a test .".Split(),
				new[] {1.0, 1.0, 1.0, 1.0, 1.0}, new[] {TranslationSources.Smt, TranslationSources.Smt, TranslationSources.Smt, TranslationSources.Smt, TranslationSources.Smt},
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
				}, new[] {3}));
			smtEngine.GetBestPhraseAlignment(Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyList<string>>()).Returns(translationResult);
			smtModel.CreateInteractiveEngine().Returns(smtEngine);
			factory.Create(Arg.Any<Engine>()).Returns(smtModel);
			return factory;
		}

		private ITranslationEngineFactory CreateRuleEngineFactory()
		{
			var factory = Substitute.For<ITranslationEngineFactory>();
			var engine = Substitute.For<ITranslationEngine>();
			engine.Translate(Arg.Any<IReadOnlyList<string>>()).Returns(new TranslationResult("esto es una prueba .".Split(), "this is a test .".Split(),
				new[] {1.0, 1.0, 1.0, 1.0, 1.0}, new[] {TranslationSources.Transfer, TranslationSources.Transfer, TranslationSources.Transfer, TranslationSources.Transfer, TranslationSources.Transfer},
				new WordAlignmentMatrix(5, 5)
				{
					[0, 0] = AlignmentType.Aligned,
					[1, 1] = AlignmentType.Aligned,
					[2, 2] = AlignmentType.Aligned,
					[3, 3] = AlignmentType.Aligned,
					[4, 4] = AlignmentType.Aligned
				}));
			factory.Create(Arg.Any<Engine>()).Returns(engine);
			return factory;
		}
	}
}
