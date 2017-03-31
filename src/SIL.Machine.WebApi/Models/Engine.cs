using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Services;
using SIL.ObjectModel;
using SIL.Threading;

namespace SIL.Machine.WebApi.Models
{
	public class Engine : DisposableBase
	{
		private readonly ISmtModelFactory _smtModelFactory;
		private readonly IRuleEngineFactory _ruleEngineFactory;
		private readonly ITextCorpusFactory _textCorpusFactory;
		private readonly TimeSpan _inactiveTimeout;
		private readonly string _trainProgressDir;
		private readonly string _configDir;
		private readonly List<(IReadOnlyList<string> Source, IReadOnlyList<string> Target)> _trainedSegments;
		private readonly AsyncLock _lock;
		private readonly HashSet<Project> _projects;
		private readonly ITokenizer<string, int> _wordTokenizer;

		private IInteractiveSmtModel _smtModel;
		private IInteractiveSmtEngine _smtEngine;
		private ITranslationEngine _ruleEngine;
		private HybridTranslationEngine _hybridEngine;
		private CancellationTokenSource _cts;
		private ISmtBatchTrainer _batchTrainer;
		private Task _rebuildTask;
		private bool _isUpdated;
		private DateTime _lastUsedTime;
		private bool _isRebuildRequired;

		public Engine(ISmtModelFactory smtModelFactory, IRuleEngineFactory ruleEngineFactory, ITextCorpusFactory textCorpusFactory, TimeSpan inactiveTimeout,
			string trainProgressDir, string configDir, string sourceLanguageTag, string targetLanguageTag)
		{
			_smtModelFactory = smtModelFactory;
			_ruleEngineFactory = ruleEngineFactory;
			_textCorpusFactory = textCorpusFactory;
			_inactiveTimeout = inactiveTimeout;
			_trainProgressDir = trainProgressDir;
			_configDir = configDir;
			SourceLanguageTag = sourceLanguageTag;
			TargetLanguageTag = targetLanguageTag;
			_trainedSegments = new List<(IReadOnlyList<string> Source, IReadOnlyList<string> Target)>();
			_lock = new AsyncLock();
			_projects = new HashSet<Project>();
			_wordTokenizer = new LatinWordTokenizer();
			_lastUsedTime = DateTime.Now;
		}

		public string SourceLanguageTag { get; }
		public string TargetLanguageTag { get; }
		internal bool IsLoaded { get; private set; }
		private bool IsTraining => _rebuildTask != null && !_rebuildTask.IsCompleted;

		public void InitExisting()
		{
			string configFileName = Path.Combine(_configDir, "config.json");
			EngineConfig config = JsonConvert.DeserializeObject<EngineConfig>(File.ReadAllText(configFileName));
			_isRebuildRequired = config.IsRebuildRequired;
			if (_isRebuildRequired)
				StartRebuildInternal();
		}

		public void InitNew()
		{
			if (!Directory.Exists(_configDir))
				Directory.CreateDirectory(_configDir);
			_smtModelFactory.InitNewModel(_configDir);
		}

		public async Task<TranslationResult> TranslateAsync(IReadOnlyList<string> segment)
		{
			using (await _lock.WaitAsync())
			{
				CheckDisposed();

				Load();

				TranslationResult result = _hybridEngine.Translate(segment);
				_lastUsedTime = DateTime.Now;
				return result;
			}
		}

		public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(int n, IReadOnlyList<string> segment)
		{
			using (await _lock.WaitAsync())
			{
				CheckDisposed();

				Load();

				IEnumerable<TranslationResult> results = _hybridEngine.Translate(n, segment);
				_lastUsedTime = DateTime.Now;
				return results.ToArray();
			}
		}

		public async Task<(WordGraph WordGraph, TranslationResult RuleResult)> InteractiveTranslateAsync(IReadOnlyList<string> segment)
		{
			using (await _lock.WaitAsync())
			{
				CheckDisposed();

				Load();

				WordGraph smtWordGraph = _smtEngine.GetWordGraph(segment);
				TranslationResult ruleResult = _ruleEngine?.Translate(segment);
				_lastUsedTime = DateTime.Now;
				return (smtWordGraph, ruleResult);
			}
		}

		public async Task TrainSegmentPairAsync(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			using (await _lock.WaitAsync())
			{
				CheckDisposed();

				Load();

				_hybridEngine.TrainSegment(sourceSegment, targetSegment);
				if (IsTraining)
					_trainedSegments.Add((sourceSegment.ToArray(), targetSegment.ToArray()));
				_isUpdated = true;
				_lastUsedTime = DateTime.Now;
			}
		}

		public async Task StartRebuildAsync()
		{
			using (await _lock.WaitAsync())
			{
				CheckDisposed();

				if (IsTraining)
					return;

				_isRebuildRequired = true;
				await SaveConfigAsync();

				StartRebuildInternal();
			}
		}

		public async Task CancelRebuildAsync()
		{
			using (await _lock.WaitAsync())
			{
				CheckDisposed();

				if (IsTraining)
					_cts.Cancel();

				_isRebuildRequired = false;
				await SaveConfigAsync();
			}
		}

		public void Commit()
		{
			using (_lock.Wait())
			{
				CheckDisposed();

				if (!IsLoaded || IsTraining)
					return;

				if (DateTime.Now - _lastUsedTime > _inactiveTimeout)
					Unload();
				else
					SaveModel();
			}
		}

		public void AddProject(Project project)
		{
			CheckDisposed();

			_projects.Add(project);
		}

		public IReadOnlyCollection<Project> GetProjects()
		{
			using (_lock.Wait())
			{
				CheckDisposed();

				return _projects.ToArray();
			}
		}

		public async Task AddProjectAsync(Project project)
		{
			using (await _lock.WaitAsync())
			{
				CheckDisposed();

				if (_projects.Contains(project))
					return;

				_projects.Add(project);

				_isRebuildRequired = true;
				await SaveConfigAsync();

				StartRebuildInternal();
			}
		}

		public async Task<bool> RemoveProjectAsync(Project project)
		{
			using (await _lock.WaitAsync())
			{
				CheckDisposed();

				_projects.Remove(project);
				if (_projects.Count == 0)
				{
					if (IsTraining)
						_cts.Cancel();
					Dispose();
					if (Directory.Exists(_configDir))
						Directory.Delete(_configDir, true);
					return true;
				}

				return false;
			}
		}

		public void WaitForRebuildToComplete()
		{
			if (_rebuildTask != null)
				Task.WaitAny(_rebuildTask);
			_rebuildTask = null;
		}

		private void StartRebuildInternal()
		{
			Load();

			ITextCorpus sourceCorpus = _textCorpusFactory.Create(_projects, _wordTokenizer, TextCorpusType.Source);
			ITextCorpus targetCorpus = _textCorpusFactory.Create(_projects, _wordTokenizer, TextCorpusType.Target);
			Func<string, string> preprocess = s => s.ToLowerInvariant();
			_batchTrainer = _smtModel.CreateBatchTrainer(preprocess, sourceCorpus, preprocess, targetCorpus);
			_cts?.Dispose();
			_cts = new CancellationTokenSource();
			CancellationToken token = _cts.Token;
			_rebuildTask = Task.Run(() => RebuildAsync(token), token);
			_lastUsedTime = DateTime.Now;
		}

		private async Task SaveConfigAsync()
		{
			var config = new EngineConfig
			{
				IsRebuildRequired = _isRebuildRequired
			};
			using (var writer = new StreamWriter(File.OpenWrite(Path.Combine(_configDir, "config.json"))))
				await writer.WriteAsync(JsonConvert.SerializeObject(config, Formatting.Indented));
		}

		private void SaveModel()
		{
			if (_isUpdated)
			{
				_smtModel.Save();
				_isUpdated = false;
			}
		}

		private void Load()
		{
			if (IsLoaded)
				return;

			_smtModel = _smtModelFactory.Create(_configDir);
			_smtEngine = _smtModel.CreateInteractiveEngine();

			_ruleEngine = _ruleEngineFactory.Create(_configDir);

			_hybridEngine = new HybridTranslationEngine(_smtEngine, _ruleEngine);
			IsLoaded = true;
		}

		private void Unload()
		{
			if (!IsLoaded)
				return;

			SaveModel();

			_hybridEngine.Dispose();
			_hybridEngine = null;

			if (_ruleEngine != null)
			{
				_ruleEngine.Dispose();
				_ruleEngine = null;
			}

			_smtEngine.Dispose();
			_smtEngine = null;

			_smtModel.Dispose();
			_smtModel = null;
			IsLoaded = false;
		}

		private async Task RebuildAsync(CancellationToken token)
		{
			using (var progress = new FileProgress(_trainProgressDir, this))
			{
				_batchTrainer.Train(progress, token.ThrowIfCancellationRequested);
				using (await _lock.WaitAsync(token))
				{
					_batchTrainer.Save();
					foreach ((IReadOnlyList<string> Source, IReadOnlyList<string> Target) trainedSegment in _trainedSegments)
						_hybridEngine.TrainSegment(trainedSegment.Source, trainedSegment.Target);
					_trainedSegments.Clear();
					_batchTrainer.Dispose();
					_batchTrainer = null;
					_isRebuildRequired = false;
					await SaveConfigAsync();
				}
			}
		}

		protected override void DisposeManagedResources()
		{
			WaitForRebuildToComplete();

			_batchTrainer?.Dispose();
			_batchTrainer = null;
			_cts?.Dispose();
			_cts = null;
			Unload();
			_trainedSegments.Clear();
			_projects.Clear();
		}
	}
}
