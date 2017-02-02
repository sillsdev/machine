using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using SIL.IO;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Services;

namespace SIL.Machine.WebApi.Tests.Models
{
	[TestFixture]
	public class EngineServiceTests
	{
		[Test]
		public void GetAll_NoEngines_ReturnsEmpty()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				Assert.That(service.GetAll(), Is.Empty);
			}
		}

		[Test]
		public void GetAll_HasEngines_ReturnsEngineDtos()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				Directory.CreateDirectory(Path.Combine(tempDir.Path, "es_en"));
				Directory.CreateDirectory(Path.Combine(tempDir.Path, "fr_en"));
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				Assert.That(service.GetAll().Select(e => $"{e.SourceLanguageTag}_{e.TargetLanguageTag}"), Is.EquivalentTo(new[] {"es_en", "fr_en"}));
			}
		}

		[Test]
		public void TryGet_NoEngine_ReturnsFalse()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				EngineDto engineDto;
				Assert.That(service.TryGet("es", "en", out engineDto), Is.False);
			}
		}

		[Test]
		public void TryGet_HasEngine_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				Directory.CreateDirectory(Path.Combine(tempDir.Path, "es_en"));
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				EngineDto engineDto;
				Assert.That(service.TryGet("es", "en", out engineDto), Is.True);
				Assert.That(engineDto.SourceLanguageTag, Is.EqualTo("es"));
				Assert.That(engineDto.TargetLanguageTag, Is.EqualTo("en"));
			}
		}

		[Test]
		public void TryTranslate_NoEngine_ReturnsFalse()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				string result;
				Assert.That(service.TryTranslate("es", "en", "Esto es una prueba.", out result), Is.False);
			}
		}

		[Test]
		public void TryTranslate_HasEngine_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				Directory.CreateDirectory(Path.Combine(tempDir.Path, "es_en"));
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				string result;
				Assert.That(service.TryTranslate("es", "en", "Esto es una prueba.", out result), Is.True);
				Assert.That(result, Is.EqualTo("This is a test."));
			}
		}

		[Test]
		public void TryInteractiveTranslate_NoEngine_ReturnsFalse()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				InteractiveTranslationResultDto result;
				Assert.That(service.TryInteractiveTranslate("es", "en", "Esto es una prueba .".Split(), out result), Is.False);
			}
		}

		[Test]
		public void TryInteractiveTranslate_HasEngine_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				Directory.CreateDirectory(Path.Combine(tempDir.Path, "es_en"));
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				InteractiveTranslationResultDto result;
				Assert.That(service.TryInteractiveTranslate("es", "en", "Esto es una prueba .".Split(), out result), Is.True);
				Assert.That(result.RuleResult.Target, Is.EqualTo("This is a test .".Split()));
				Assert.That(result.WordGraph.Arcs.SelectMany(a => a.Words), Is.EqualTo("This is a test .".Split()));
			}
		}

		[Test]
		public void TryTrainSegment_NoEngine_ReturnsFalse()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				var pairDto = new SegmentPairDto
				{
					SourceSegment = "Esto es una prueba.",
					TargetSegment = "This is a test."
				};
				Assert.That(service.TryTrainSegment("es", "en", pairDto), Is.False);
			}
		}

		[Test]
		public void TryTrainSegment_HasEngine_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				Directory.CreateDirectory(Path.Combine(tempDir.Path, "es_en"));
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				var pairDto = new SegmentPairDto
				{
					SourceSegment = "Esto es una prueba.",
					TargetSegment = "This is a test."
				};
				Assert.That(service.TryTrainSegment("es", "en", pairDto), Is.True);
			}
		}

		[Test]
		public void Add_EngineDirectoryAdded_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				Directory.CreateDirectory(Path.Combine(tempDir.Path, "es_en"));
				EngineDto engineDto;
				Assert.That(service.Add("es", "en", out engineDto), Is.True);
				Assert.That(service.TryGet("es", "en", out engineDto), Is.True);
			}
		}

		[Test]
		public void Add_EngineDirectoryNotAdded_ReturnsFalse()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				EngineDto engineDto;
				Assert.That(service.Add("es", "en", out engineDto), Is.False);
				Assert.That(service.TryGet("es", "en", out engineDto), Is.False);
			}
		}

		[Test]
		public void Remove_EngineDirectoryRemoved_ReturnsTrue()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				Directory.CreateDirectory(Path.Combine(tempDir.Path, "es_en"));
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				EngineDto engineDto;
				Assert.That(service.TryGet("es", "en", out engineDto), Is.True);
				Directory.Delete(Path.Combine(tempDir.Path, "es_en"), true);
				Assert.That(service.Remove("es", "en"), Is.True);
				Assert.That(service.TryGet("es", "en", out engineDto), Is.False);
			}
		}

		[Test]
		public void Remove_EngineDoesNotExist_ReturnsFalse()
		{
			using (var tempDir = new TempDirectory("EngineServiceTests"))
			{
				var service = new EngineService(CreateOptions(tempDir.Path), CreateSmtModelFactory(), CreateRuleEngineFactory());
				Assert.That(service.Remove("es", "en"), Is.False);
			}
		}

		private IOptions<EngineOptions> CreateOptions(string rootDir)
		{
			var options = Substitute.For<IOptions<EngineOptions>>();
			options.Value.Returns(new EngineOptions
				{
					EngineUpdateFrequency = TimeSpan.FromMinutes(5),
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
			factory.Create(Arg.Any<EngineContext>()).Returns(smtModel);
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
			factory.Create(Arg.Any<EngineContext>()).Returns(engine);
			return factory;
		}
	}
}
