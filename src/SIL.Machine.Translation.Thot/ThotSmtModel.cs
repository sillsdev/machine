using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SIL.Machine.Corpora;
using SIL.ObjectModel;
using SIL.Progress;

namespace SIL.Machine.Translation.Thot
{
	public class ThotSmtModel : DisposableBase, IInteractiveSmtModel
	{
		private readonly string _cfgFileName;
		private readonly ThotSingleWordAlignmentModel _singleWordAlignmentModel;
		private readonly ThotSingleWordAlignmentModel _inverseSingleWordAlignmentModel;
		private readonly HashSet<ThotSmtEngine> _engines = new HashSet<ThotSmtEngine>();

		public ThotSmtModel(string cfgFileName)
		{
			_cfgFileName = cfgFileName;
			Parameters = new ThotSmtParameters();
			string cfgDirPath = Path.GetDirectoryName(cfgFileName);
			foreach (string line in File.ReadAllLines(cfgFileName))
			{
				string name, value;
				if (!GetConfigParameter(line, out name, out value))
					continue;

				switch (name)
				{
					case "tm":
						if (string.IsNullOrEmpty(value))
							throw new ArgumentException("The -tm parameter does not have a value.", nameof(cfgFileName));
						TranslationModelFileNamePrefix = value;
						if (!Path.IsPathRooted(TranslationModelFileNamePrefix) && !string.IsNullOrEmpty(cfgDirPath))
							TranslationModelFileNamePrefix = Path.Combine(cfgDirPath, TranslationModelFileNamePrefix);
						break;
					case "lm":
						if (string.IsNullOrEmpty(value))
							throw new ArgumentException("The -lm parameter does not have a value.", nameof(cfgFileName));
						LanguageModelFileNamePrefix = value;
						if (!Path.IsPathRooted(LanguageModelFileNamePrefix) && !string.IsNullOrEmpty(cfgDirPath))
							LanguageModelFileNamePrefix = Path.Combine(cfgDirPath, LanguageModelFileNamePrefix);
						break;
					case "W":
						if (string.IsNullOrEmpty(value))
							throw new ArgumentException("The -W parameter does not have a value.", nameof(cfgFileName));
						Parameters.ModelW = float.Parse(value, CultureInfo.InvariantCulture);
						break;
					case "S":
						if (string.IsNullOrEmpty(value))
							throw new ArgumentException("The -S parameter does not have a value.", nameof(cfgFileName));
						Parameters.DecoderS = uint.Parse(value);
						break;
					case "A":
						if (string.IsNullOrEmpty(value))
							throw new ArgumentException("The -A parameter does not have a value.", nameof(cfgFileName));
						Parameters.ModelA = uint.Parse(value);
						break;
					case "E":
						if (string.IsNullOrEmpty(value))
							throw new ArgumentException("The -E parameter does not have a value.", nameof(cfgFileName));
						Parameters.ModelE = uint.Parse(value);
						break;
					case "nomon":
						if (string.IsNullOrEmpty(value))
							throw new ArgumentException("The -nomon parameter does not have a value.", nameof(cfgFileName));
						Parameters.ModelNonMonotonicity = uint.Parse(value);
						break;
					case "be":
						Parameters.DecoderBreadthFirst = false;
						break;
					case "G":
						if (string.IsNullOrEmpty(value))
							throw new ArgumentException("The -G parameter does not have a value.", nameof(cfgFileName));
						Parameters.DecoderG = uint.Parse(value);
						break;
					case "h":
						if (string.IsNullOrEmpty(value))
							throw new ArgumentException("The -h parameter does not have a value.", nameof(cfgFileName));
						Parameters.ModelHeuristic = (ModelHeuristic) uint.Parse(value);
						break;
					case "olp":
						if (string.IsNullOrEmpty(value))
							throw new ArgumentException("The -olp parameter does not have a value.", nameof(cfgFileName));
						string[] tokens = value.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
						if (tokens.Length >= 1)
							Parameters.LearningAlgorithm = (LearningAlgorithm) uint.Parse(tokens[0]);
						if (tokens.Length >= 2)
							Parameters.LearningRatePolicy = (LearningRatePolicy) uint.Parse(tokens[1]);
						if (tokens.Length >= 3)
							Parameters.LearningStepSize = float.Parse(tokens[2], CultureInfo.InvariantCulture);
						if (tokens.Length >= 4)
							Parameters.LearningEMIters = uint.Parse(tokens[3]);
						if (tokens.Length >= 5)
							Parameters.LearningE = uint.Parse(tokens[4]);
						if (tokens.Length >= 6)
							Parameters.LearningR = uint.Parse(tokens[5]);
						break;
					case "tmw":
						if (string.IsNullOrEmpty(value))
							throw new ArgumentException("The -tmw parameter does not have a value.", nameof(cfgFileName));

						Parameters.ModelWeights = value.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries)
							.Select(t => float.Parse(t, CultureInfo.InvariantCulture)).ToArray();
						break;
				}
			}

			if (string.IsNullOrEmpty(TranslationModelFileNamePrefix))
				throw new ArgumentException("The config file does not have a -tm parameter specified.", nameof(cfgFileName));
			if (string.IsNullOrEmpty(LanguageModelFileNamePrefix))
				throw new ArgumentException("The config file does not have a -lm parameter specified.", nameof(cfgFileName));
			Parameters.Freeze();

			Handle = Thot.LoadSmtModel(TranslationModelFileNamePrefix, LanguageModelFileNamePrefix, Parameters);

			_singleWordAlignmentModel = new ThotSingleWordAlignmentModel(Thot.smtModel_getSingleWordAlignmentModel(Handle));
			_inverseSingleWordAlignmentModel = new ThotSingleWordAlignmentModel(Thot.smtModel_getInverseSingleWordAlignmentModel(Handle));
		}

		public ThotSmtModel(string tmFileNamePrefix, string lmFileNamePrefix, ThotSmtParameters parameters)
		{
			TranslationModelFileNamePrefix = tmFileNamePrefix;
			LanguageModelFileNamePrefix = lmFileNamePrefix;
			Parameters = parameters;
			Parameters.Freeze();

			Handle = Thot.LoadSmtModel(TranslationModelFileNamePrefix, LanguageModelFileNamePrefix, Parameters);

			_singleWordAlignmentModel = new ThotSingleWordAlignmentModel(Thot.smtModel_getSingleWordAlignmentModel(Handle));
			_inverseSingleWordAlignmentModel = new ThotSingleWordAlignmentModel(Thot.smtModel_getInverseSingleWordAlignmentModel(Handle));
		}

		public string TranslationModelFileNamePrefix { get; }
		public string LanguageModelFileNamePrefix { get; }
		public ThotSmtParameters Parameters { get; private set; }
		internal IntPtr Handle { get; private set; }

		public ISegmentAligner SingleWordAlignmentModel
		{
			get
			{
				CheckDisposed();

				return _singleWordAlignmentModel;
			}
		}

		public ISegmentAligner InverseSingleWordAlignmentModel
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

		public void Train(Func<string, string> sourcePreprocessor, ITextCorpus sourceCorpus, Func<string, string> targetPreprocessor,
			ITextCorpus targetCorpus, ITextAlignmentCorpus alignmentCorpus = null, IProgress progress = null)
		{
			CheckDisposed();

			if (_engines.Count > 0)
				throw new InvalidOperationException("The model cannot be trained while there are active engines open.");

			Thot.smtModel_close(Handle);

			var trainer = new ThotBatchTrainer(TranslationModelFileNamePrefix, LanguageModelFileNamePrefix, Parameters, sourcePreprocessor,
				sourceCorpus, targetPreprocessor, targetCorpus, alignmentCorpus);
			trainer.Train(progress);
			Parameters = trainer.Parameters;
			SaveParameters();

			Handle = Thot.LoadSmtModel(TranslationModelFileNamePrefix, LanguageModelFileNamePrefix, Parameters);
			_singleWordAlignmentModel.Handle = Thot.smtModel_getSingleWordAlignmentModel(Handle);
			_inverseSingleWordAlignmentModel.Handle = Thot.smtModel_getInverseSingleWordAlignmentModel(Handle);
		}

		private void SaveParameters()
		{
			if (string.IsNullOrEmpty(_cfgFileName))
				return;

			string[] lines = File.ReadAllLines(_cfgFileName);
			using (var writer = new StreamWriter(File.Open(_cfgFileName, FileMode.Create)))
			{
				bool weightsWritten = false;
				foreach (string line in lines)
				{
					string name, value;
					if (GetConfigParameter(line, out name, out value) && name == "tmw")
					{
						WriteModelWeights(writer);
						weightsWritten = true;
					}
					else
					{
						writer.Write($"{line}\n");
					}
				}

				if (!weightsWritten)
					WriteModelWeights(writer);
			}
		}

		private void WriteModelWeights(StreamWriter writer)
		{
			writer.Write($"-tmw {string.Join(" ", Parameters.ModelWeights.Select(w => w.ToString("0.######")))}\n");
		}

		private static bool GetConfigParameter(string line, out string name, out string value)
		{
			name = null;
			value = null;
			string l = line.Trim();
			if (l.StartsWith("#"))
				return false;

			int index = l.IndexOf(' ');
			if (index == -1)
			{
				name = l;
			}
			else
			{
				name = l.Substring(0, index);
				value = l.Substring(index + 1).Trim();
			}

			if (name.StartsWith("-"))
				name = name.Substring(1);
			return true;
		}

		internal void RemoveEngine(ThotSmtEngine engine)
		{
			_engines.Remove(engine);
		}

		protected override void DisposeManagedResources()
		{
			foreach (ThotSmtEngine engine in _engines.ToArray())
				engine.Dispose();
		}

		protected override void DisposeUnmanagedResources()
		{
			_singleWordAlignmentModel.Dispose();
			_inverseSingleWordAlignmentModel.Dispose();
			Thot.smtModel_close(Handle);
		}
	}
}
