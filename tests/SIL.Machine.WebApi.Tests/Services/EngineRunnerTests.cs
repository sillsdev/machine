using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Models;
using SIL.ObjectModel;
using Xunit;

namespace SIL.Machine.WebApi.Services
{
	public class EngineRunnerTests
	{
		[Fact]
		public async Task StartBuildAsync_BatchTrainerCalled()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngine();
				await env.EngineRunner.InitNewAsync();
				Build build = await env.EngineRunner.StartBuildAsync(env.Engine);
				env.EngineRunner.WaitForBuildToComplete();
				env.BatchTrainer.Received().Train(Arg.Any<IProgress<SmtTrainProgress>>(), Arg.Any<Action>());
				env.BatchTrainer.Received().Save();
				build = await env.BuildRepository.GetAsync(build.Id);
				build.Should().BeNull();
			}
		}

		[Fact]
		public async Task CancelBuildAsync_BatchTrainerCalled()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngine();
				await env.EngineRunner.InitNewAsync();
				env.BatchTrainer.Train(Arg.Any<IProgress<SmtTrainProgress>>(), Arg.Do<Action>(checkCanceled =>
					{
						while (true)
							checkCanceled();
					}));
				Build build = await env.EngineRunner.StartBuildAsync(env.Engine);
				await Task.Delay(10);
				await env.EngineRunner.CancelBuildAsync();
				env.EngineRunner.WaitForBuildToComplete();
				env.BatchTrainer.Received().Train(Arg.Any<IProgress<SmtTrainProgress>>(), Arg.Any<Action>());
				env.BatchTrainer.DidNotReceive().Save();
				build = await env.BuildRepository.GetAsync(build.Id);
				build.Should().BeNull();
			}
		}

		[Fact]
		public async Task CommitAsync_LoadedInactive_Unloaded()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngine();
				await env.EngineRunner.InitNewAsync();
				await env.EngineRunner.TrainSegmentPairAsync("esto es una prueba .".Split(), "this is a test .".Split());
				await Task.Delay(10);
				await env.EngineRunner.CommitAsync();
				env.SmtModel.Received().Save();
				env.EngineRunner.IsLoaded.Should().BeFalse();
			}
		}

		[Fact]
		public async Task CommitAsync_LoadedActive_Loaded()
		{
			using (var env = new TestEnvironment())
			{
				env.CreateEngine(TimeSpan.FromHours(1));
				await env.EngineRunner.InitNewAsync();
				await env.EngineRunner.TrainSegmentPairAsync("esto es una prueba .".Split(), "this is a test .".Split());
				await env.EngineRunner.CommitAsync();
				env.SmtModel.Received().Save();
				env.EngineRunner.IsLoaded.Should().BeTrue();
			}
		}

		private class TestEnvironment : DisposableBase
		{
			public TestEnvironment()
			{
				BuildRepository = new MemoryBuildRepository();
				BatchTrainer = Substitute.For<ISmtBatchTrainer>();
				SmtModel = Substitute.For<IInteractiveSmtModel>();
			}

			public Engine Engine { get; private set; }
			public EngineRunner EngineRunner { get; private set; }
			public IBuildRepository BuildRepository { get; }
			public ISmtBatchTrainer BatchTrainer { get; }
			public IInteractiveSmtModel SmtModel { get; }

			public void CreateEngine(TimeSpan inactiveTimeout = default(TimeSpan))
			{
				Engine = new Engine
				{
					Id = "engine1",
					SourceLanguageTag = "es",
					TargetLanguageTag = "en",
					IsShared = false,
					Projects = {"project1"}
				};
				EngineRunner = new EngineRunner(BuildRepository, CreateSmtModelFactory(), CreateRuleEngineFactory(),
					CreateTextCorpusFactory(), inactiveTimeout, Engine.Id);
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
				factory.Create(Arg.Any<IEnumerable<string>>(), Arg.Any<ITokenizer<string, int>>(), Arg.Any<TextCorpusType>())
					.Returns(new DictionaryTextCorpus(Enumerable.Empty<IText>()));
				return factory;
			}

			protected override void DisposeManagedResources()
			{
				EngineRunner.Dispose();
			}
		}
	}
}
