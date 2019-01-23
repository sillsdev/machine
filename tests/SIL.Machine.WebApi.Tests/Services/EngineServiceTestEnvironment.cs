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
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.DataAccess.Memory;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Options;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Services
{
	public class EngineServiceTestEnvironment : DisposableBase
	{
		private readonly MemoryStorage _memoryStorage;
		private readonly BackgroundJobClient _jobClient;
		private BackgroundJobServer _jobServer;
		private ISmtModelFactory _smtModelFactory;
		private IRuleEngineFactory _ruleEngineFactory;
		private ITextCorpusFactory _textCorpusFactory;

		public EngineServiceTestEnvironment()
		{
			EngineRepository = new MemoryEngineRepository();
			BuildRepository = new MemoryBuildRepository();
			ProjectRepository = new MemoryRepository<Project>();
			MachineOptions = new MachineOptions();
			_memoryStorage = new MemoryStorage();
			_jobClient = new BackgroundJobClient(_memoryStorage);
		}

		public IEngineRepository EngineRepository { get; }
		public IBuildRepository BuildRepository { get; }
		public IRepository<Project> ProjectRepository { get; }
		public EngineService Service { get; private set; }
		public ISmtBatchTrainer BatchTrainer { get; private set; }
		public IInteractiveSmtModel SmtModel { get; private set; }
		public MachineOptions MachineOptions { get; }

		public EngineRuntime GetRuntime(string engineId)
		{
			return Service?.GetOrCreateRuntime(engineId);
		}

		public void CreateEngineService()
		{
			SmtModel = Substitute.For<IInteractiveSmtModel>();
			BatchTrainer = Substitute.For<ISmtBatchTrainer>();
			BatchTrainer.Stats.Returns(new SmtBatchTrainStats());
			_smtModelFactory = CreateSmtModelFactory();
			_ruleEngineFactory = CreateRuleEngineFactory();
			_textCorpusFactory = CreateTextCorpusFactory();

			Service = new EngineService(new OptionsWrapper<MachineOptions>(MachineOptions), EngineRepository,
				BuildRepository, ProjectRepository, CreateEngineRuntime);
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
			var runtime = new EngineRuntime(new OptionsWrapper<MachineOptions>(MachineOptions), EngineRepository,
				BuildRepository, _smtModelFactory, _ruleEngineFactory, _jobClient, _textCorpusFactory,
				Substitute.For<ILogger<EngineRuntime>>(), engineId);
			return new Owned<EngineRuntime>(runtime, runtime);
		}

		private ISmtModelFactory CreateSmtModelFactory()
		{
			var factory = Substitute.For<ISmtModelFactory>();

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
					[0, 0] = true,
					[1, 1] = true,
					[2, 2] = true,
					[3, 3] = true,
					[4, 4] = true
				},
				new[] { new Phrase(Range<int>.Create(0, 5), 5, 1.0) });
			smtEngine.Translate(Arg.Any<IReadOnlyList<string>>()).Returns(translationResult);
			smtEngine.GetWordGraph(Arg.Any<IReadOnlyList<string>>()).Returns(new WordGraph(new[]
			{
					new WordGraphArc(0, 1, 1.0, "this is".Split(),
						new WordAlignmentMatrix(2, 2)
						{
							[0, 0] = true, [1, 1] = true
						},
						Range<int>.Create(0, 2), false, new[] { 1.0, 1.0 }),
					new WordGraphArc(1, 2, 1.0, "a test".Split(),
						new WordAlignmentMatrix(2, 2)
						{
							[0, 0] = true, [1, 1] = true
						},
						Range<int>.Create(2, 4), false, new[] { 1.0, 1.0 }),
					new WordGraphArc(2, 3, 1.0, new[] { "." },
						new WordAlignmentMatrix(1, 1) { [0, 0] = true },
						Range<int>.Create(4, 5), false, new[] { 1.0 })
				}, new[] { 3 }));
			smtEngine.GetBestPhraseAlignment(Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyList<string>>())
				.Returns(translationResult);
			SmtModel.CreateInteractiveEngine().Returns(smtEngine);

			SmtModel.CreateBatchTrainer(Arg.Any<Func<string, string>>(), Arg.Any<ITextCorpus>(),
				Arg.Any<Func<string, string>>(), Arg.Any<ITextCorpus>(), Arg.Any<ITextAlignmentCorpus>())
				.Returns(BatchTrainer);

			factory.Create(Arg.Any<string>()).Returns(SmtModel);
			return factory;
		}

		private IRuleEngineFactory CreateRuleEngineFactory()
		{
			var factory = Substitute.For<IRuleEngineFactory>();
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
			factory.Create(Arg.Any<string>()).Returns(engine);
			return factory;
		}

		private ITextCorpusFactory CreateTextCorpusFactory()
		{
			var factory = Substitute.For<ITextCorpusFactory>();
			factory.CreateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<TextCorpusType>())
				.Returns(Task.FromResult<ITextCorpus>(new DictionaryTextCorpus(Enumerable.Empty<IText>())));
			return factory;
		}

		public async Task<Engine> CreateEngineAsync(string sourceLanguageTag = "es", string targetLanguageTag = "en",
			bool isShared = false)
		{
			var engine = new Engine
			{
				SourceLanguageTag = sourceLanguageTag,
				TargetLanguageTag = targetLanguageTag,
				IsShared = isShared,
				Projects = { "project1" }
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
				EngineRef = engine.Id
			};
			await ProjectRepository.InsertAsync(project);
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
			using (Subscription<Build> subscription = await BuildRepository.SubscribeAsync(buildId))
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
					return new EngineRuntime.BuildRunner(_env.Service);
				return base.ActivateJob(jobType);
			}
		}
	}
}
