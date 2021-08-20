using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	public class ThotSmtModel : ThotSmtModel<ThotHmmWordAlignmentModel>
	{
		public ThotSmtModel(string cfgFileName)
			: base(cfgFileName)
		{
		}

		public ThotSmtModel(ThotSmtParameters parameters)
			: base(parameters)
		{
		}
	}

	public class ThotSmtModel<TAlignModel> : DisposableBase, IInteractiveTranslationModel, IThotSmtModelInternal
		where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
	{
		private readonly TAlignModel _directWordAlignmentModel;
		private readonly TAlignModel _inverseWordAlignmentModel;
		private readonly SymmetrizedWordAlignmentModel _symmetrizedWordAlignmentModel;
		private readonly HashSet<ThotSmtEngine> _engines = new HashSet<ThotSmtEngine>();
		private readonly string _swAlignClassName;
		private IntPtr _handle;
		private IWordAligner _wordAligner;

		public ThotSmtModel(string cfgFileName)
			: this(ThotSmtParameters.Load(cfgFileName))
		{
			ConfigFileName = cfgFileName;
		}

		public ThotSmtModel(ThotSmtParameters parameters)
		{
			Parameters = parameters;
			Parameters.Freeze();

			_swAlignClassName = Thot.GetWordAlignmentClassName<TAlignModel>();
			_handle = Thot.LoadSmtModel(_swAlignClassName, Parameters);

			_directWordAlignmentModel = new TAlignModel();
			_directWordAlignmentModel.SetHandle(Thot.smtModel_getSingleWordAlignmentModel(_handle), true);

			_inverseWordAlignmentModel = new TAlignModel();
			_inverseWordAlignmentModel.SetHandle(Thot.smtModel_getInverseSingleWordAlignmentModel(_handle), true);

			_symmetrizedWordAlignmentModel = new SymmetrizedWordAlignmentModel(_directWordAlignmentModel,
				_inverseWordAlignmentModel);
			WordAligner = new FuzzyEditDistanceWordAlignmentMethod();
		}

		public string ConfigFileName { get; }
		public ThotSmtParameters Parameters { get; private set; }
		public IWordAligner WordAligner
		{
			get => _wordAligner;
			set
			{
				_wordAligner = value;
				if (_wordAligner is IWordAlignmentMethod method)
					method.ScoreSelector = GetWordAlignmentScore;
			}
		}
		IntPtr IThotSmtModelInternal.Handle => _handle;

		public TAlignModel DirectWordAlignmentModel
		{
			get
			{
				CheckDisposed();

				return _directWordAlignmentModel;
			}
		}

		public TAlignModel InverseWordAlignmentModel
		{
			get
			{
				CheckDisposed();

				return _inverseWordAlignmentModel;
			}
		}

		public SymmetrizedWordAlignmentModel SymmetrizedWordAlignmentModel
		{
			get
			{
				CheckDisposed();

				return _symmetrizedWordAlignmentModel;
			}
		}

		public ThotSmtEngine CreateEngine()
		{
			var engine = new ThotSmtEngine(this);
			_engines.Add(engine);
			return engine;
		}

		public ThotSmtEngine CreateInteractiveEngine()
		{
			return CreateEngine();
		}

		ITranslationEngine ITranslationModel.CreateEngine()
		{
			return CreateEngine();
		}

		IInteractiveTranslationEngine IInteractiveTranslationModel.CreateInteractiveEngine()
		{
			return CreateEngine();
		}

		public void Save()
		{
			Thot.smtModel_saveModels(_handle);
		}

		public Task SaveAsync()
		{
			Save();
			return Task.CompletedTask;
		}

		public ITrainer CreateTrainer(ITokenProcessor sourcePreprocessor,
			ITokenProcessor targetPreprocessor, ParallelTextCorpus corpus, int maxCorpusCount = int.MaxValue)
		{
			CheckDisposed();

			return string.IsNullOrEmpty(ConfigFileName)
				? new Trainer(this, Parameters, sourcePreprocessor, targetPreprocessor, corpus, maxCorpusCount)
				: new Trainer(this, ConfigFileName, sourcePreprocessor, targetPreprocessor, corpus, maxCorpusCount);
		}

		void IThotSmtModelInternal.RemoveEngine(ThotSmtEngine engine)
		{
			_engines.Remove(engine);
		}

		protected override void DisposeManagedResources()
		{
			foreach (ThotSmtEngine engine in _engines.ToArray())
				engine.Dispose();
			_directWordAlignmentModel.Dispose();
			_inverseWordAlignmentModel.Dispose();
		}

		protected override void DisposeUnmanagedResources()
		{
			Thot.smtModel_close(_handle);
		}

		private double GetWordAlignmentScore(IReadOnlyList<string> sourceSegment, int sourceIndex,
			IReadOnlyList<string> targetSegment, int targetIndex)
		{
			return _symmetrizedWordAlignmentModel.GetTranslationScore(
				sourceIndex == -1 ? null : sourceSegment[sourceIndex],
				targetIndex == -1 ? null : targetSegment[targetIndex]);
		}

		private class Trainer : ThotSmtModelTrainer<TAlignModel>
		{
			private readonly ThotSmtModel<TAlignModel> _smtModel;

			public Trainer(ThotSmtModel<TAlignModel> smtModel, string cfgFileName, ITokenProcessor sourcePreprocessor,
				ITokenProcessor targetPreprocessor, ParallelTextCorpus corpus, int maxCorpusCount)
				: base(cfgFileName, sourcePreprocessor, targetPreprocessor, corpus, maxCorpusCount)
			{
				_smtModel = smtModel;
			}

			public Trainer(ThotSmtModel<TAlignModel> smtModel, ThotSmtParameters parameters,
				ITokenProcessor sourcePreprocessor, ITokenProcessor targetPreprocessor,
				ParallelTextCorpus corpus, int maxCorpusCount)
				: base(parameters, sourcePreprocessor, targetPreprocessor, corpus, maxCorpusCount)
			{
				_smtModel = smtModel;
			}

			public override void Save()
			{
				foreach (ThotSmtEngine engine in _smtModel._engines)
					engine.CloseHandle();
				Thot.smtModel_close(_smtModel._handle);

				base.Save();

				_smtModel.Parameters = Parameters;
				_smtModel._handle = Thot.LoadSmtModel(_smtModel._swAlignClassName, _smtModel.Parameters);
				_smtModel._directWordAlignmentModel.SetHandle(Thot.smtModel_getSingleWordAlignmentModel(
					_smtModel._handle), true);
				_smtModel._inverseWordAlignmentModel.SetHandle(Thot.smtModel_getInverseSingleWordAlignmentModel(
					_smtModel._handle), true);
				foreach (ThotSmtEngine engine in _smtModel._engines)
					engine.LoadHandle();
			}
		}
	}
}
