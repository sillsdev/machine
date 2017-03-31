using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using SIL.IO;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Services;
using SIL.ObjectModel;
using Xunit;

namespace SIL.Machine.WebApi.Tests.Models
{
	public class EngineTests
	{
		[Fact]
		public async Task StartRebuildAsync_BatchTrainerCalled()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngine();
				env.Engine.InitNew();
				await env.Engine.StartRebuildAsync();
				env.Engine.WaitForRebuildToComplete();
				env.BatchTrainer.Received().Train(Arg.Any<IProgress<SmtTrainProgress>>(), Arg.Any<Action>());
				env.BatchTrainer.Received().Save();
				EngineConfig config = env.LoadConfig();
				config.IsRebuildRequired.Should().BeFalse();
			}
		}

		[Fact]
		public async Task CancelRebuildAsync_BatchTrainerCalled()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngine();
				env.Engine.InitNew();
				env.BatchTrainer.Train(Arg.Any<IProgress<SmtTrainProgress>>(), Arg.Do<Action>(checkCanceled =>
					{
						while (true)
							checkCanceled();
					}));
				await env.Engine.StartRebuildAsync();
				await env.Engine.CancelRebuildAsync();
				env.Engine.WaitForRebuildToComplete();
				env.BatchTrainer.Received().Train(Arg.Any<IProgress<SmtTrainProgress>>(), Arg.Any<Action>());
				env.BatchTrainer.DidNotReceive().Save();
				EngineConfig config = env.LoadConfig();
				config.IsRebuildRequired.Should().BeFalse();
			}
		}

		[Fact]
		public void InitExisting_RebuildRequired_RebuildExecuted()
		{
			using (var env = new TestEnvironment())
			{
				var config = new EngineConfig
				{
					IsRebuildRequired = true
				};
				env.SaveConfig(config);
				env.CreateEngine();
				env.Engine.InitExisting();
				env.Engine.WaitForRebuildToComplete();
				env.BatchTrainer.Received().Train(Arg.Any<IProgress<SmtTrainProgress>>(), Arg.Any<Action>());
				env.BatchTrainer.Received().Save();
				config = env.LoadConfig();
				config.IsRebuildRequired.Should().BeFalse();
			}
		}

		[Fact]
		public async Task CommitAsync_LoadedInactive_Unloaded()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngine();
				env.Engine.InitNew();
				await env.Engine.TrainSegmentPairAsync("esto es una prueba .".Split(), "this is a test .".Split());
				await Task.Delay(10);
				await env.Engine.CommitAsync();
				env.SmtModel.Received().Save();
				env.Engine.IsLoaded.Should().BeFalse();
			}
		}

		[Fact]
		public async Task CommitAsync_LoadedActive_Loaded()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngine(TimeSpan.FromHours(1));
				env.Engine.InitNew();
				await env.Engine.TrainSegmentPairAsync("esto es una prueba .".Split(), "this is a test .".Split());
				await env.Engine.CommitAsync();
				env.SmtModel.Received().Save();
				env.Engine.IsLoaded.Should().BeTrue();
			}
		}

		private class TestEnvironment : DisposableBase
		{
			private readonly TempDirectory _tempDir;

			public TestEnvironment()
			{
				_tempDir = new TempDirectory("EngineTests");
				BatchTrainer = Substitute.For<ISmtBatchTrainer>();
				SmtModel = Substitute.For<IInteractiveSmtModel>();
			}

			private Project Project { get; set; }
			public Engine Engine { get; private set; }
			public ISmtBatchTrainer BatchTrainer { get; }
			public IInteractiveSmtModel SmtModel { get; }
			private string ConfigFileName => Path.Combine(_tempDir.Path, "config.json");

			public void SaveConfig(EngineConfig config)
			{
				File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(config, Formatting.Indented));
			}

			public EngineConfig LoadConfig()
			{
				return JsonConvert.DeserializeObject<EngineConfig>(File.ReadAllText(ConfigFileName));
			}

			public void CreateEngine(TimeSpan inactiveTimeout = default(TimeSpan))
			{
				Engine = new Engine(CreateSmtModelFactory(), CreateRuleEngineFactory(), CreateTextCorpusFactory(), inactiveTimeout,
					_tempDir.Path, _tempDir.Path, "es", "en");
				Project = new Project("project1", false, _tempDir.Path, Engine);
				Engine.AddProject(Project);
			}

			private ISmtModelFactory CreateSmtModelFactory()
			{
				var factory = Substitute.For<ISmtModelFactory>();

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
				SmtModel.CreateInteractiveEngine().Returns(smtEngine);

				SmtModel.CreateBatchTrainer(Arg.Any<Func<string, string>>(), Arg.Any<ITextCorpus>(), Arg.Any<Func<string, string>>(), Arg.Any<ITextCorpus>(),
					Arg.Any<ITextAlignmentCorpus>()).Returns(BatchTrainer);

				factory.Create(Arg.Any<string>()).Returns(SmtModel);
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

			protected override void DisposeManagedResources()
			{
				Engine.Dispose();
				_tempDir.Dispose();
			}
		}
	}
}
