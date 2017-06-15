using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Models;
using SIL.ObjectModel;
using SIL.Threading;

namespace SIL.Machine.WebApi.Services
{
	public class EngineRunner : DisposableBase
	{
		private readonly IBuildRepository _buildRepo;
		private readonly ISmtModelFactory _smtModelFactory;
		private readonly IRuleEngineFactory _ruleEngineFactory;
		private readonly ITextCorpusFactory _textCorpusFactory;
		private readonly TimeSpan _inactiveTimeout;
		private readonly string _engineId;
		private readonly List<(IReadOnlyList<string> Source, IReadOnlyList<string> Target)> _trainedSegments;
		private readonly AsyncLock _lock;
		private readonly ITokenizer<string, int> _wordTokenizer;

		private IInteractiveSmtModel _smtModel;
		private IInteractiveSmtEngine _smtEngine;
		private ITranslationEngine _ruleEngine;
		private HybridTranslationEngine _hybridEngine;
		private CancellationTokenSource _cts;
		private ISmtBatchTrainer _batchTrainer;
		private Task _buildTask;
		private Build _build;
		private bool _isUpdated;
		private DateTime _lastUsedTime;

		public EngineRunner(IBuildRepository buildRepo, ISmtModelFactory smtModelFactory, IRuleEngineFactory ruleEngineFactory,
			ITextCorpusFactory textCorpusFactory, TimeSpan inactiveTimeout, string engineId)
		{
			_buildRepo = buildRepo;
			_smtModelFactory = smtModelFactory;
			_ruleEngineFactory = ruleEngineFactory;
			_textCorpusFactory = textCorpusFactory;
			_inactiveTimeout = inactiveTimeout;
			_engineId = engineId;
			_trainedSegments = new List<(IReadOnlyList<string> Source, IReadOnlyList<string> Target)>();
			_lock = new AsyncLock();
			_wordTokenizer = new LatinWordTokenizer();
			_lastUsedTime = DateTime.Now;
		}

		internal bool IsLoaded { get; private set; }
		private bool IsBuilding => _buildTask != null && !_buildTask.IsCompleted;

		public async Task InitNewAsync()
		{
			CheckDisposed();

			using (await _lock.LockAsync())
				_smtModelFactory.InitNewModel(_engineId);
		}

		public async Task<TranslationResult> TranslateAsync(IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				Load();

				TranslationResult result = _hybridEngine.Translate(segment);
				_lastUsedTime = DateTime.Now;
				return result;
			}
		}

		public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(int n, IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				Load();

				IEnumerable<TranslationResult> results = _hybridEngine.Translate(n, segment);
				_lastUsedTime = DateTime.Now;
				return results.ToArray();
			}
		}

		public async Task<InteractiveTranslationResult> InteractiveTranslateAsync(IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				Load();

				WordGraph smtWordGraph = _smtEngine.GetWordGraph(segment);
				TranslationResult ruleResult = _ruleEngine?.Translate(segment);
				_lastUsedTime = DateTime.Now;
				return new InteractiveTranslationResult(smtWordGraph, ruleResult);
			}
		}

		public async Task TrainSegmentPairAsync(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				Load();

				_hybridEngine.TrainSegment(sourceSegment, targetSegment);
				if (IsBuilding)
					_trainedSegments.Add((sourceSegment.ToArray(), targetSegment.ToArray()));
				_isUpdated = true;
				_lastUsedTime = DateTime.Now;
			}
		}

		public void RestartUnfinishedBuild(Build build, Engine engine)
		{
			CheckDisposed();

			using (_lock.Lock())
			{
				_build = build;
				StartBuildInternal(engine);
			}
		}

		public async Task<Build> StartBuildAsync(Engine engine)
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				if (IsBuilding)
					return null;

				_build = new Build {EngineId = _engineId};
				await _buildRepo.InsertAsync(_build);
				StartBuildInternal(engine);
				return _build;
			}
		}

		private void StartBuildInternal(Engine engine)
		{
			Load();

			ITextCorpus sourceCorpus = _textCorpusFactory.Create(engine.Projects, _wordTokenizer, TextCorpusType.Source);
			ITextCorpus targetCorpus = _textCorpusFactory.Create(engine.Projects, _wordTokenizer, TextCorpusType.Target);
			_batchTrainer = _smtModel.CreateBatchTrainer(Preprocessors.Lowercase, sourceCorpus, Preprocessors.Lowercase,
				targetCorpus);
			_cts?.Dispose();
			_cts = new CancellationTokenSource();
			CancellationToken token = _cts.Token;
			_buildTask = Task.Run(() => BuildAsync(token), token);
			_lastUsedTime = DateTime.Now;
		}

		public async Task CancelBuildAsync()
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				if (IsBuilding)
					_cts.Cancel();
				await _buildRepo.DeleteAsync(_build);
			}
		}

		public async Task CommitAsync()
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				if (!IsLoaded || IsBuilding)
					return;

				if (DateTime.Now - _lastUsedTime > _inactiveTimeout)
					Unload();
				else
					SaveModel();
			}
		}

		internal void WaitForBuildToComplete()
		{
			CheckDisposed();

			if (_buildTask != null)
				Task.WaitAny(_buildTask);
			_buildTask = null;
		}

		public async Task DeleteDataAsync()
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				if (IsBuilding)
					_cts.Cancel();
				await _buildRepo.DeleteAsync(_build);
				Unload();
				_smtModelFactory.Delete(_engineId);
			}
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

			_smtModel = _smtModelFactory.Create(_engineId);
			_smtEngine = _smtModel.CreateInteractiveEngine();

			_ruleEngine = _ruleEngineFactory.Create(_engineId);

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

		private async Task BuildAsync(CancellationToken token)
		{
			var progress = new BuildProgress(_buildRepo, _build);
			_batchTrainer.Train(progress, token.ThrowIfCancellationRequested);
			using (await _lock.LockAsync(token))
			{
				_batchTrainer.Save();
				foreach ((IReadOnlyList<string> Source, IReadOnlyList<string> Target) trainedSegment in _trainedSegments)
					_hybridEngine.TrainSegment(trainedSegment.Source, trainedSegment.Target);
				_trainedSegments.Clear();
				_batchTrainer.Dispose();
				_batchTrainer = null;
				await _buildRepo.DeleteAsync(_build);
			}
		}

		protected override void DisposeManagedResources()
		{
			WaitForBuildToComplete();

			_batchTrainer?.Dispose();
			_batchTrainer = null;
			_cts?.Dispose();
			_cts = null;
			Unload();
			_trainedSegments.Clear();
		}
	}
}
