using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	public class ThotSmtModel : DisposableBase, IInteractiveSmtModel
	{
		private readonly ThotSingleWordAlignmentModel _singleWordAlignmentModel;
		private readonly ThotSingleWordAlignmentModel _inverseSingleWordAlignmentModel;
		private readonly HashSet<ThotSmtEngine> _engines = new HashSet<ThotSmtEngine>();

		public ThotSmtModel(string cfgFileName)
			: this(ThotSmtParameters.Load(cfgFileName))
		{
			ConfigFileName = cfgFileName;
		}

		public ThotSmtModel(ThotSmtParameters parameters)
		{
			Parameters = parameters;
			Parameters.Freeze();

			Handle = Thot.LoadSmtModel(Parameters);

			_singleWordAlignmentModel = new ThotSingleWordAlignmentModel(
				Thot.smtModel_getSingleWordAlignmentModel(Handle));
			_inverseSingleWordAlignmentModel = new ThotSingleWordAlignmentModel(
				Thot.smtModel_getInverseSingleWordAlignmentModel(Handle));
		}

		public string ConfigFileName { get; }
		public ThotSmtParameters Parameters { get; private set; }
		internal IntPtr Handle { get; private set; }

		public ThotSingleWordAlignmentModel SingleWordAlignmentModel
		{
			get
			{
				CheckDisposed();

				return _singleWordAlignmentModel;
			}
		}

		public ThotSingleWordAlignmentModel InverseSingleWordAlignmentModel
		{
			get
			{
				CheckDisposed();

				return _inverseSingleWordAlignmentModel;
			}
		}

		public ISmtEngine CreateEngine()
		{
			return CreateInteractiveEngine();
		}

		public IInteractiveSmtEngine CreateInteractiveEngine()
		{
			var engine = new ThotSmtEngine(this);
			_engines.Add(engine);
			return engine;
		}

		public void Save()
		{
			Thot.smtModel_saveModels(Handle);
		}

		public ISmtBatchTrainer CreateBatchTrainer(Func<string, string> sourcePreprocessor, ITextCorpus sourceCorpus,
			Func<string, string> targetPreprocessor, ITextCorpus targetCorpus,
			ITextAlignmentCorpus alignmentCorpus = null)
		{
			CheckDisposed();

			var corpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignmentCorpus);

			return string.IsNullOrEmpty(ConfigFileName)
				? new BatchTrainer(this, Parameters, sourcePreprocessor, targetPreprocessor, corpus)
				: new BatchTrainer(this, ConfigFileName, sourcePreprocessor, targetPreprocessor, corpus);
		}

		internal void RemoveEngine(ThotSmtEngine engine)
		{
			_engines.Remove(engine);
		}

		protected override void DisposeManagedResources()
		{
			foreach (ThotSmtEngine engine in _engines.ToArray())
				engine.Dispose();
			_singleWordAlignmentModel.Dispose();
			_inverseSingleWordAlignmentModel.Dispose();
		}

		protected override void DisposeUnmanagedResources()
		{
			Thot.smtModel_close(Handle);
		}

		private class BatchTrainer : ThotSmtBatchTrainer
		{
			private readonly ThotSmtModel _smtModel;

			public BatchTrainer(ThotSmtModel smtModel, string cfgFileName, Func<string, string> sourcePreprocessor,
				Func<string, string> targetPreprocessor, ParallelTextCorpus corpus)
				: base(cfgFileName, sourcePreprocessor, targetPreprocessor, corpus)
			{
				_smtModel = smtModel;
			}

			public BatchTrainer(ThotSmtModel smtModel, ThotSmtParameters parameters,
				Func<string, string> sourcePreprocessor, Func<string, string> targetPreprocessor,
				ParallelTextCorpus corpus)
				: base(parameters, sourcePreprocessor, targetPreprocessor, corpus)
			{
				_smtModel = smtModel;
			}

			public override void Save()
			{
				foreach (ThotSmtEngine engine in _smtModel._engines)
					engine.CloseHandle();
				Thot.smtModel_close(_smtModel.Handle);

				base.Save();

				_smtModel.Parameters = Parameters;
				_smtModel.Handle = Thot.LoadSmtModel(_smtModel.Parameters);
				_smtModel._singleWordAlignmentModel.Handle = Thot.smtModel_getSingleWordAlignmentModel(_smtModel.Handle);
				_smtModel._inverseSingleWordAlignmentModel.Handle =
					Thot.smtModel_getInverseSingleWordAlignmentModel(_smtModel.Handle);
				foreach (ThotSmtEngine engine in _smtModel._engines)
					engine.LoadHandle();
			}
		}
	}
}
