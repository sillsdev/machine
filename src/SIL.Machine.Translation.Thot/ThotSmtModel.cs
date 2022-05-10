using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	public class ThotSmtModel : DisposableBase, IInteractiveTranslationModel, IThotSmtModelInternal
	{
		private readonly ThotWordAlignmentModel _directWordAlignmentModel;
		private readonly ThotWordAlignmentModel _inverseWordAlignmentModel;
		private readonly SymmetrizedWordAlignmentModel _symmetrizedWordAlignmentModel;
		private readonly HashSet<ThotSmtEngine> _engines = new HashSet<ThotSmtEngine>();
		private IntPtr _handle;
		private IWordAligner _wordAligner;

		public ThotSmtModel(ThotWordAlignmentModelType wordAlignmentModelType, string cfgFileName)
			: this(wordAlignmentModelType, ThotSmtParameters.Load(cfgFileName))
		{
			ConfigFileName = cfgFileName;
		}

		public ThotSmtModel(ThotWordAlignmentModelType wordAlignmentModelType, ThotSmtParameters parameters)
		{
			Parameters = parameters;
			Parameters.Freeze();

			WordAlignmentModelType = wordAlignmentModelType;
			_handle = Thot.LoadSmtModel(wordAlignmentModelType, Parameters);

			_directWordAlignmentModel = ThotWordAlignmentModel.Create(wordAlignmentModelType);
			_directWordAlignmentModel.SetHandle(Thot.smtModel_getSingleWordAlignmentModel(_handle), true);

			_inverseWordAlignmentModel = ThotWordAlignmentModel.Create(wordAlignmentModelType);
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

		public ThotWordAlignmentModel DirectWordAlignmentModel
		{
			get
			{
				CheckDisposed();

				return _directWordAlignmentModel;
			}
		}

		public ThotWordAlignmentModel InverseWordAlignmentModel
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

		public ThotWordAlignmentModelType WordAlignmentModelType { get; }

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

		public ThotSmtModelTrainer CreateTrainer(IParallelTextCorpus corpus)
		{
			CheckDisposed();

			return string.IsNullOrEmpty(ConfigFileName)
				? new Trainer(this, corpus, Parameters)
				: new Trainer(this, corpus, ConfigFileName);
		}

		ITrainer ITranslationModel.CreateTrainer(IParallelTextCorpus corpus)
		{
			return CreateTrainer(corpus);
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

		private class Trainer : ThotSmtModelTrainer
		{
			private readonly ThotSmtModel _smtModel;

			public Trainer(ThotSmtModel smtModel, IParallelTextCorpus corpus, string cfgFileName)
				: base(smtModel.WordAlignmentModelType, corpus, cfgFileName)
			{
				_smtModel = smtModel;
			}

			public Trainer(ThotSmtModel smtModel, IParallelTextCorpus corpus, ThotSmtParameters parameters)
				: base(smtModel.WordAlignmentModelType, corpus, parameters)
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
				_smtModel._handle = Thot.LoadSmtModel(_smtModel.WordAlignmentModelType, _smtModel.Parameters);
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
