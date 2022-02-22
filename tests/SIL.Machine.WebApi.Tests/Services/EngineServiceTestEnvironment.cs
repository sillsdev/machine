using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Features.OwnedInstances;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SIL.Machine.Annotations;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Configuration;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Models;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Services
{
	internal class EngineServiceTestEnvironment : DisposableBase
	{
		private readonly MemoryStorage _memoryStorage;
		private readonly BackgroundJobClient _jobClient;
		private BackgroundJobServer _jobServer;
		private IComponentFactory<IInteractiveTranslationModel> _interactiveModelFactory;
		private IComponentFactory<ITranslationEngine> _ruleEngineFactory;
		private IComponentFactory<ITruecaser> _truecaserFactory;
		private ITextCorpusFactory _textCorpusFactory;

		public EngineServiceTestEnvironment()
		{
			EngineRepository = new MemoryRepository<Engine>();
			BuildRepository = new MemoryRepository<Build>();
			EngineOptions = new EngineOptions();
			_memoryStorage = new MemoryStorage();
			_jobClient = new BackgroundJobClient(_memoryStorage);
		}

		public IRepository<Engine> EngineRepository { get; }
		public IRepository<Build> BuildRepository { get; }
		public EngineService Service { get; private set; }
		public ITrainer SmtBatchTrainer { get; private set; }
		public IInteractiveTranslationModel InteractiveModel { get; private set; }
		public EngineOptions EngineOptions { get; }
		public IBuildHandler BuildHandler { get; } = new BuildHandler();
		public ITruecaser Truecaser { get; private set; }
		public ITrainer TruecaserTrainer { get; private set; }

		public EngineRuntime GetRuntime(string engineId)
		{
			return Service?.GetOrCreateRuntime(engineId);
		}

		public void CreateEngineService()
		{
			InteractiveModel = Substitute.For<IInteractiveTranslationModel>();
			SmtBatchTrainer = Substitute.For<ITrainer>();
			SmtBatchTrainer.Stats.Returns(new TrainStats { Metrics = { { "bleu", 0.0 }, { "perplexity", 0.0 } } });
			Truecaser = Substitute.For<ITruecaser>();
			TruecaserTrainer = Substitute.For<ITrainer>();
			TruecaserTrainer.SaveAsync().Returns(Task.CompletedTask);
			_interactiveModelFactory = CreateInteractiveModelFactory();
			_ruleEngineFactory = CreateRuleEngineFactory();
			_truecaserFactory = CreateTruecaserFactory();
			_textCorpusFactory = CreateTextCorpusFactory();

			Service = new EngineService(new OptionsWrapper<EngineOptions>(EngineOptions), EngineRepository,
				BuildRepository, CreateEngineRuntime);
			Service.Init();
			var jobServerOptions = new BackgroundJobServerOptions
			{
				Activator = new EnvActivator(this)
			};
			_jobServer = new BackgroundJobServer(jobServerOptions, _memoryStorage);
		}

		public void DisposeEngineService()
		{
			_jobServer?.Dispose();
			_jobServer = null;
			Service?.Dispose();
			Service = null;
		}

		private Owned<EngineRuntime> CreateEngineRuntime(string engineId)
		{
			var runtime = new EngineRuntime(new OptionsWrapper<EngineOptions>(EngineOptions), EngineRepository,
				BuildRepository, _interactiveModelFactory, _ruleEngineFactory, _truecaserFactory, _jobClient,
				_textCorpusFactory, Substitute.For<ILogger<EngineRuntime>>(), engineId);
			return new Owned<EngineRuntime>(runtime, runtime);
		}

		private IComponentFactory<IInteractiveTranslationModel> CreateInteractiveModelFactory()
		{
			var factory = Substitute.For<IComponentFactory<IInteractiveTranslationModel>>();

			var engine = Substitute.For<IInteractiveTranslationEngine>();
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
			engine.Translate(Arg.Any<IReadOnlyList<string>>()).Returns(translationResult);
			engine.GetWordGraph(Arg.Any<IReadOnlyList<string>>()).Returns(new WordGraph(new[]
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
			}, new[] { 3 }));
			InteractiveModel.CreateInteractiveEngine().Returns(engine);

			InteractiveModel.CreateTrainer(Arg.Any<ParallelTextCorpus>(), Arg.Any<ITokenProcessor>(),
				Arg.Any<ITokenProcessor>()).Returns(SmtBatchTrainer);

			factory.CreateAsync(Arg.Any<string>()).Returns(Task.FromResult(InteractiveModel));
			return factory;
		}

		private IComponentFactory<ITranslationEngine> CreateRuleEngineFactory()
		{
			var factory = Substitute.For<IComponentFactory<ITranslationEngine>>();
			var engine = Substitute.For<ITranslationEngine>();
			engine.Translate(Arg.Any<IReadOnlyList<string>>()).Returns(new TranslationResult(
				"esto es una prueba .".Split(),
				"this is a test .".Split(),
				new[] { 1.0, 1.0, 1.0, 1.0, 1.0 },
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
					[0, 0] = true,
					[1, 1] = true,
					[2, 2] = true,
					[3, 3] = true,
					[4, 4] = true
				},
				new[] { new Phrase(Range<int>.Create(0, 5), 5, 1.0) }));
			factory.CreateAsync(Arg.Any<string>()).Returns(Task.FromResult(engine));
			return factory;
		}

		private IComponentFactory<ITruecaser> CreateTruecaserFactory()
		{
			var factory = Substitute.For<IComponentFactory<ITruecaser>>();
			Truecaser.Truecase(Arg.Any<IReadOnlyList<string>>(), Arg.Any<TranslationResult>()).Returns(x =>
			{
				var sourceSegment = x.Arg<IReadOnlyList<string>>();
				var result = x.Arg<TranslationResult>();
				IReadOnlyList<string> targetSegment = result.TargetSegment.Select(t => t == "test" ? "TEST" : t)
					.ToArray();
				return new TranslationResult(sourceSegment, targetSegment, result.WordConfidences, result.WordSources,
					result.Alignment, result.Phrases);
			});
			Truecaser.Truecase(Arg.Any<IReadOnlyList<string>>(), Arg.Any<WordGraph>()).Returns(x =>
			{
				var graph = x.Arg<WordGraph>();
				var arcs = new List<WordGraphArc>();
				foreach (WordGraphArc arc in graph.Arcs)
				{
					IReadOnlyList<string> words = arc.Words.Select(t => t == "test" ? "TEST" : t).ToArray();
					arcs.Add(new WordGraphArc(arc.PrevState, arc.NextState, arc.Score, words, arc.Alignment,
						arc.SourceSegmentRange, arc.WordSources, arc.WordConfidences));
				}
				return new WordGraph(arcs, graph.FinalStates, graph.InitialStateScore);
			});
			Truecaser.CreateTrainer(Arg.Any<ITextCorpus>()).Returns(TruecaserTrainer);
			factory.CreateAsync(Arg.Any<string>()).Returns(Task.FromResult(Truecaser));
			return factory;
		}

		private ITextCorpusFactory CreateTextCorpusFactory()
		{
			var factory = Substitute.For<ITextCorpusFactory>();
			factory.CreateAsync(Arg.Any<string>(), Arg.Any<TextCorpusType>())
				.Returns(Task.FromResult<ITextCorpus>(new DictionaryTextCorpus(Enumerable.Empty<IText>())));
			return factory;
		}

		private static IEnumerable<TranslationSources> GetSources(int count, bool isUnknown)
		{
			var sources = new TranslationSources[count];
			for (int i = 0; i < count; i++)
				sources[i] = isUnknown ? TranslationSources.None : TranslationSources.Smt;
			return sources;
		}

		public async Task<Engine> CreateEngineAsync(string id = "engine1", string sourceLanguageTag = "es",
			string targetLanguageTag = "en")
		{
			var engine = new Engine
			{
				Id = id,
				SourceLanguageTag = sourceLanguageTag,
				TargetLanguageTag = targetLanguageTag,
			};
			await EngineRepository.InsertAsync(engine);
			return engine;
		}

		public Task WaitForBuildToFinishAsync(string buildId)
		{
			return WaitForBuildState(buildId, BuildStates.IsFinished);
		}

		public Task WaitForBuildToStartAsync(string buildId)
		{
			return WaitForBuildState(buildId, s => s != BuildStates.Pending);
		}

		private async Task WaitForBuildState(string buildId, Func<string, bool> predicate)
		{
			using (Subscription<Build> subscription = await BuildRepository.SubscribeAsync(b => b.Id == buildId))
			{
				while (true)
				{
					Build build = subscription.Change.Entity;
					if (build == null || predicate(build.State))
						break;
					await subscription.WaitForUpdateAsync();
				}
			}
		}

		protected override void DisposeManagedResources()
		{
			DisposeEngineService();
		}

		private class EnvActivator : JobActivator
		{
			private readonly EngineServiceTestEnvironment _env;

			public EnvActivator(EngineServiceTestEnvironment env)
			{
				_env = env;
			}

			public override object ActivateJob(Type jobType)
			{
				if (jobType == typeof(EngineRuntime.BuildRunner))
					return new EngineRuntime.BuildRunner(_env.Service, _env.BuildHandler);
				return base.ActivateJob(jobType);
			}
		}
	}
}
