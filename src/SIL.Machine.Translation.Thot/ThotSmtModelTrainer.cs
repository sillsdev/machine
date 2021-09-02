using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using SIL.Extensions;
using SIL.Machine.Corpora;
using SIL.Machine.NgramModeling;
using SIL.Machine.Optimization;
using SIL.ObjectModel;
using SIL.Machine.Statistics;
using System.Threading.Tasks;
using SIL.Machine.Utils;

namespace SIL.Machine.Translation.Thot
{
	public class ThotSmtModelTrainer : DisposableBase, ITrainer
	{
		private readonly ITokenProcessor _sourcePreprocessor;
		private readonly ITokenProcessor _targetPreprocessor;
		private readonly ParallelTextCorpus _parallelCorpus;
		private readonly HashSet<int> _tuneCorpusIndices;
		private readonly IParameterTuner _modelWeightTuner;
		private readonly string _tempDir;
		private readonly string _lmFilePrefix;
		private readonly string _tmFilePrefix;
		private readonly string _trainLMDir;
		private readonly string _trainTMDir;
		private readonly int _maxCorpusCount;
		private readonly ThotWordAlignmentModelType _wordAlignmentModelType;

		public ThotSmtModelTrainer(ThotWordAlignmentModelType wordAlignmentModelType, string cfgFileName,
			ITokenProcessor sourcePreprocessor, ITokenProcessor targetPreprocessor, ParallelTextCorpus corpus,
			int maxCorpusCount = int.MaxValue)
			: this(wordAlignmentModelType, ThotSmtParameters.Load(cfgFileName), sourcePreprocessor, targetPreprocessor,
				  corpus, maxCorpusCount)
		{
			ConfigFileName = cfgFileName;
		}

		public ThotSmtModelTrainer(ThotWordAlignmentModelType wordAlignmentModelType, ThotSmtParameters parameters,
			ITokenProcessor sourcePreprocessor, ITokenProcessor targetPreprocessor, ParallelTextCorpus corpus,
			int maxCorpusCount = int.MaxValue)
		{
			Parameters = parameters;
			Parameters.Freeze();
			_sourcePreprocessor = sourcePreprocessor;
			_targetPreprocessor = targetPreprocessor;
			_maxCorpusCount = maxCorpusCount;
			_parallelCorpus = corpus;
			_wordAlignmentModelType = wordAlignmentModelType;
			// _modelWeightTuner = new MiraModelWeightTuner(swAlignClassName);
			_modelWeightTuner = new SimplexModelWeightTuner(wordAlignmentModelType);
			_tuneCorpusIndices = CreateTuneCorpus();

			do
			{
				_tempDir = Path.Combine(Path.GetTempPath(), "thot-smt-train-" + Guid.NewGuid());
			} while (Directory.Exists(_tempDir));
			Directory.CreateDirectory(_tempDir);

			_lmFilePrefix = Path.GetFileName(Parameters.LanguageModelFileNamePrefix);
			_tmFilePrefix = Path.GetFileName(Parameters.TranslationModelFileNamePrefix);
			_trainLMDir = Path.Combine(_tempDir, "lm");
			_trainTMDir = Path.Combine(_tempDir, "tm_train");
		}

		public string ConfigFileName { get; }
		public ThotSmtParameters Parameters { get; private set; }
		public TrainStats Stats { get; } = new TrainStats();

		private HashSet<int> CreateTuneCorpus()
		{
			int corpusCount = 0;
			var invalidIndices = new HashSet<int>();
			int index = 0;
			foreach (ParallelTextSegment segment in _parallelCorpus.Segments)
			{
				if (IsSegmentValid(segment))
					corpusCount++;
				else
					invalidIndices.Add(index);
				index++;
				if (corpusCount == _maxCorpusCount)
					break;
			}
			Stats.TrainedSegmentCount = corpusCount;
			int tuneCorpusCount = Math.Min((int)(corpusCount * 0.1), 1000);
			var r = new Random(31415);
			return new HashSet<int>(Enumerable.Range(0, corpusCount + invalidIndices.Count)
				.Where(i => !invalidIndices.Contains(i))
				.OrderBy(i => r.Next()).Take(tuneCorpusCount));
		}

		public virtual void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null)
		{
			var reporter = new ThotTrainProgressReporter(progress, checkCanceled);

			Directory.CreateDirectory(_trainLMDir);
			string trainLMPrefix = Path.Combine(_trainLMDir, _lmFilePrefix);
			Directory.CreateDirectory(_trainTMDir);
			string trainTMPrefix = Path.Combine(_trainTMDir, _tmFilePrefix);

			using (PhaseProgress phaseProgress = reporter.StartNextPhase())
				TrainLanguageModel(trainLMPrefix, 3);

			TrainTranslationModel(trainTMPrefix, reporter);

			reporter.CheckCanceled();

			string tuneTMDir = Path.Combine(_tempDir, "tm_tune");
			Directory.CreateDirectory(tuneTMDir);
			string tuneTMPrefix = Path.Combine(tuneTMDir, _tmFilePrefix);
			CopyFiles(_trainTMDir, tuneTMDir, _tmFilePrefix);

			var tuneSourceCorpus = new List<IReadOnlyList<string>>(_tuneCorpusIndices.Count);
			var tuneTargetCorpus = new List<IReadOnlyList<string>>(_tuneCorpusIndices.Count);
			foreach (ParallelTextSegment segment in GetTuningSegments(_parallelCorpus))
			{
				tuneSourceCorpus.Add(_sourcePreprocessor.Process(segment.SourceSegment));
				tuneTargetCorpus.Add(_targetPreprocessor.Process(segment.TargetSegment));
			}

			using (PhaseProgress phaseProgress = reporter.StartNextPhase())
				TuneLanguageModel(trainLMPrefix, tuneTargetCorpus, 3);

			using (PhaseProgress phaseProgress = reporter.StartNextPhase())
				TuneTranslationModel(tuneTMPrefix, trainLMPrefix, tuneSourceCorpus, tuneTargetCorpus, phaseProgress);

			using (PhaseProgress phaseProgress = reporter.StartNextPhase())
				TrainTuneCorpus(trainTMPrefix, trainLMPrefix, tuneSourceCorpus, tuneTargetCorpus, phaseProgress);
		}

		public virtual void Save()
		{
			SaveParameters();

			string lmDir = Path.GetDirectoryName(Parameters.LanguageModelFileNamePrefix);
			Debug.Assert(lmDir != null);
			string tmDir = Path.GetDirectoryName(Parameters.TranslationModelFileNamePrefix);
			Debug.Assert(tmDir != null);

			if (!Directory.Exists(lmDir))
				Directory.CreateDirectory(lmDir);
			CopyFiles(_trainLMDir, lmDir, _lmFilePrefix);
			if (!Directory.Exists(tmDir))
				Directory.CreateDirectory(tmDir);
			CopyFiles(_trainTMDir, tmDir, _tmFilePrefix);
		}

		public Task SaveAsync()
		{
			Save();
			return Task.CompletedTask;
		}

		private void SaveParameters()
		{
			if (string.IsNullOrEmpty(ConfigFileName) || Parameters.ModelWeights == null)
				return;

			string[] lines = File.ReadAllLines(ConfigFileName);
			using (var writer = new StreamWriter(ConfigFileName))
			{
				bool weightsWritten = false;
				foreach (string line in lines)
				{
					string name, value;
					if (ThotSmtParameters.GetConfigParameter(line, out name, out value) && name == "tmw")
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

		private static void CopyFiles(string srcDir, string destDir, string filePrefix)
		{
			foreach (string srcFile in Directory.EnumerateFiles(srcDir, filePrefix + "*"))
			{
				string fileName = Path.GetFileName(srcFile);
				Debug.Assert(fileName != null);
				File.Copy(srcFile, Path.Combine(destDir, fileName), true);
			}
		}

		private void TrainLanguageModel(string lmPrefix, int ngramSize)
		{
			WriteNgramCountsFile(lmPrefix, ngramSize);
			WriteLanguageModelWeightsFile(lmPrefix, ngramSize, Enumerable.Repeat(0.5, ngramSize * 3));
			WriteWordPredictionFile(lmPrefix);
		}

		private void WriteNgramCountsFile(string lmPrefix, int ngramSize)
		{
			int wordCount = 0;
			var ngrams = new Dictionary<Ngram<string>, int>();
			var vocab = new HashSet<string>();
			foreach (TextSegment segment in _parallelCorpus.TargetSegments
				.Where((s, i) => !_tuneCorpusIndices.Contains(i) && !s.IsEmpty))
			{
				var words = new List<string> { "<s>" };
				foreach (string word in _targetPreprocessor.Process(segment.Segment).Select(Thot.EscapeToken))
				{
					if (vocab.Contains(word))
					{
						words.Add(word);
					}
					else
					{
						vocab.Add(word);
						words.Add("<unk>");
					}
				}
				words.Add("</s>");
				if (words.Count == 2)
					continue;
				wordCount += words.Count;
				for (int n = 1; n <= ngramSize; n++)
				{
					for (int i = 0; i <= words.Count - n; i++)
					{
						var ngram = new Ngram<string>(Enumerable.Range(i, n).Select(j => words[j]));
						ngrams.UpdateValue(ngram, () => 0, c => c + 1);
					}
				}
			}

			using (var writer = new StreamWriter(lmPrefix))
			{
				foreach (KeyValuePair<Ngram<string>, int> kvp in ngrams.OrderBy(kvp => kvp.Key.Length)
					.ThenBy(kvp => string.Join(" ", kvp.Key)))
				{
					writer.Write("{0} {1} {2}\n", string.Join(" ", kvp.Key),
						kvp.Key.Length == 1 ? wordCount : ngrams[kvp.Key.TakeAllExceptLast()], kvp.Value);
				}
			}
		}

		private static void WriteLanguageModelWeightsFile(string lmPrefix, int ngramSize, IEnumerable<double> weights)
		{
			File.WriteAllText(lmPrefix + ".weights",
				$"{ngramSize} 3 10 {string.Join(" ", weights.Select(w => w.ToString("0.######")))}\n");
		}

		private void WriteWordPredictionFile(string lmPrefix)
		{
			var rand = new Random(31415);
			using (var writer = new StreamWriter(lmPrefix + ".wp"))
			{
				foreach (TextSegment segment in _parallelCorpus.TargetSegments
					.Where((s, i) => !_tuneCorpusIndices.Contains(i) && !s.IsEmpty)
					.Take(100000).OrderBy(i => rand.Next()))
				{
					string segmentStr = string.Join(" ", _targetPreprocessor.Process(segment.Segment)
						.Select(Thot.EscapeToken));
					writer.Write("{0}\n", segmentStr);
				}
			}
		}

		private void TrainTranslationModel(string tmPrefix, ThotTrainProgressReporter reporter)
		{
			string invswmPrefix = tmPrefix + "_invswm";
			GenerateWordAlignmentModel(invswmPrefix, _sourcePreprocessor, _targetPreprocessor, _parallelCorpus,
				reporter);

			string swmPrefix = tmPrefix + "_swm";
			GenerateWordAlignmentModel(swmPrefix, _targetPreprocessor, _sourcePreprocessor, _parallelCorpus.Invert(),
				reporter);

			using (PhaseProgress phaseProgress = reporter.StartNextPhase())
				Thot.giza_symmetr1(swmPrefix + ".bestal", invswmPrefix + ".bestal", tmPrefix + ".A3.final", true);

			using (PhaseProgress phaseProgress = reporter.StartNextPhase())
				Thot.phraseModel_generate(tmPrefix + ".A3.final", 10, tmPrefix + ".ttable", 20);

			File.WriteAllText(tmPrefix + ".lambda", "0.7 0.7");
			File.WriteAllText(tmPrefix + ".srcsegmlentable", "Uniform");
			File.WriteAllText(tmPrefix + ".trgcutstable", "0.999");
			File.WriteAllText(tmPrefix + ".trgsegmlentable", "Geometric");
		}

		private void GenerateWordAlignmentModel(string swmPrefix, ITokenProcessor sourcePreprocessor,
			ITokenProcessor targetPreprocessor, ParallelTextCorpus corpus, ThotTrainProgressReporter reporter)
		{
			using (PhaseProgress phaseProgress = reporter.StartNextPhase())
			{
				TrainWordAlignmentModel(swmPrefix, sourcePreprocessor, targetPreprocessor, corpus, phaseProgress);
			}

			reporter.CheckCanceled();

			string ext = null;
			switch (_wordAlignmentModelType)
			{
				case ThotWordAlignmentModelType.Hmm:
					ext = ".hmm_lexnd";
					break;
				case ThotWordAlignmentModelType.Ibm1:
				case ThotWordAlignmentModelType.Ibm2:
					ext = ".ibm_lexnd";
					break;
				case ThotWordAlignmentModelType.FastAlign:
					ext = ".fa_lexnd";
					break;
			}
			Debug.Assert(ext != null);

			PruneLexTable(swmPrefix + ext, 0.00001);

			using (PhaseProgress phaseProgress = reporter.StartNextPhase())
			{
				GenerateBestAlignments(swmPrefix, swmPrefix + ".bestal", sourcePreprocessor, targetPreprocessor, corpus,
					phaseProgress);
			}
		}

		private static void PruneLexTable(string fileName, double threshold)
		{
			var entries = new List<Tuple<uint, uint, float>>();
#if THOT_TEXT_FORMAT
			using (var reader = new StreamReader(fileName))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					string[] fields = line.Split(' ');
					entries.Add(Tuple.Create(uint.Parse(fields[0], CultureInfo.InvariantCulture),
						uint.Parse(fields[1], CultureInfo.InvariantCulture),
						float.Parse(fields[2], CultureInfo.InvariantCulture)));
				}
			}
#else
			using (var reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
			{
				int pos = 0;
				var length = (int)reader.BaseStream.Length;
				while (pos < length)
				{
					uint srcIndex = reader.ReadUInt32();
					pos += sizeof(uint);
					uint trgIndex = reader.ReadUInt32();
					pos += sizeof(uint);
					float numer = reader.ReadSingle();
					pos += sizeof(float);
					reader.ReadSingle();
					pos += sizeof(float);

					entries.Add(Tuple.Create(srcIndex, trgIndex, numer));
				}
			}
#endif

#if THOT_TEXT_FORMAT
			using (var writer = new StreamWriter(fileName))
#else
			using (var writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
#endif
			{
				foreach (IGrouping<uint, Tuple<uint, uint, float>> g in entries.GroupBy(e => e.Item1).OrderBy(g => g.Key))
				{
					Tuple<uint, uint, float>[] groupEntries = g.OrderByDescending(e => e.Item3).ToArray();

					double lcSrc = groupEntries.Select(e => e.Item3).Skip(1)
						.Aggregate((double)groupEntries[0].Item3, (a, n) => LogSpace.Add(a, n));

					double newLcSrc = -99999;
					int count = 0;
					foreach (Tuple<uint, uint, float> entry in groupEntries)
					{
						double prob = Math.Exp(entry.Item3 - lcSrc);
						if (prob < threshold)
							break;
						newLcSrc = LogSpace.Add(newLcSrc, entry.Item3);
						count++;
					}

					for (int i = 0; i < count; i++)
					{
#if THOT_TEXT_FORMAT
						writer.Write("{0} {1} {2:0.######} {3:0.######}\n", groupEntries[i].Item1,
							groupEntries[i].Item2, groupEntries[i].Item3, newLcSrc);
#else
						writer.Write(groupEntries[i].Item1);
						writer.Write(groupEntries[i].Item2);
						writer.Write(groupEntries[i].Item3);
						writer.Write((float)newLcSrc);
#endif
					}
				}
			}
		}

		private void TrainWordAlignmentModel(string swmPrefix, ITokenProcessor sourcePreprocessor,
			ITokenProcessor targetPreprocessor, ParallelTextCorpus corpus, IProgress<ProgressStatus> progress)
		{
			using (var trainer = new ThotWordAlignmentModelTrainer(_wordAlignmentModelType, swmPrefix,
				sourcePreprocessor, targetPreprocessor, corpus, _maxCorpusCount)
			{ TrainingIterationCount = (int)Parameters.LearningEMIters })
			{
				trainer.SegmentFilter = (s, i) => !_tuneCorpusIndices.Contains(i);
				trainer.Train(progress);
				trainer.Save();
			}
		}

		private void GenerateBestAlignments(string swmPrefix, string fileName, ITokenProcessor sourcePreprocessor,
			ITokenProcessor targetPreprocessor, ParallelTextCorpus corpus, IProgress<ProgressStatus> progress)
		{
			var escapeTokenProcessor = new EscapeTokenProcessor();
			sourcePreprocessor = TokenProcessors.Pipeline(sourcePreprocessor, escapeTokenProcessor);
			targetPreprocessor = TokenProcessors.Pipeline(targetPreprocessor, escapeTokenProcessor);
			using (var model = ThotWordAlignmentModel.Create(_wordAlignmentModelType))
			using (var writer = new StreamWriter(fileName))
			{
				model.Load(swmPrefix);
				int i = 0;
				foreach (ParallelTextSegment segment in GetTrainingSegments(corpus))
				{
					progress.Report(new ProgressStatus(i, Stats.TrainedSegmentCount));

					writer.Write($"# {segment.TextId} {segment.SegmentRef}\n");
					writer.Write(model.GetGizaFormatString(segment, sourcePreprocessor, targetPreprocessor));
					i++;
				}
				progress.Report(new ProgressStatus(1.0));
			}
		}

		private void TuneLanguageModel(string lmPrefix, IList<IReadOnlyList<string>> tuneTargetCorpus, int ngramSize)
		{
			if (tuneTargetCorpus.Count == 0)
				return;

			var simplex = new NelderMeadSimplex(0.1, 200, 1.0);
			MinimizationResult result = simplex.FindMinimum(w =>
				CalculatePerplexity(tuneTargetCorpus, lmPrefix, ngramSize, w), Enumerable.Repeat(0.5, ngramSize * 3));
			WriteLanguageModelWeightsFile(lmPrefix, ngramSize, result.MinimizingPoint);
			Stats.Metrics["perplexity"] = result.ErrorValue;
		}

		private static double CalculatePerplexity(IList<IReadOnlyList<string>> tuneTargetCorpus, string lmPrefix,
			int ngramSize, Vector weights)
		{
			if (weights.Any(w => w < 0 || w >= 1.0))
				return 999999;

			WriteLanguageModelWeightsFile(lmPrefix, ngramSize, weights);
			double lp = 0;
			int wordCount = 0;
			using (var lm = new ThotLanguageModel(lmPrefix))
			{
				foreach (IReadOnlyList<string> segment in tuneTargetCorpus)
				{
					lp += lm.GetSegmentLog10Probability(segment);
					wordCount += segment.Count;
				}
			}

			return Math.Exp(-(lp / (wordCount + tuneTargetCorpus.Count)) * Math.Log(10));
		}

		private void TuneTranslationModel(string tuneTMPrefix, string tuneLMPrefix,
			IReadOnlyList<IReadOnlyList<string>> tuneSourceCorpus,
			IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus, IProgress<ProgressStatus> progress)
		{
			if (tuneSourceCorpus.Count == 0)
				return;

			string phraseTableFileName = tuneTMPrefix + ".ttable";
			FilterPhraseTableUsingCorpus(phraseTableFileName, tuneSourceCorpus);

			ThotSmtParameters oldParameters = Parameters;
			ThotSmtParameters initialParameters = oldParameters.Clone();
			initialParameters.TranslationModelFileNamePrefix = tuneTMPrefix;
			initialParameters.LanguageModelFileNamePrefix = tuneLMPrefix;
			initialParameters.ModelWeights = new[] { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0f };
			initialParameters.Freeze();

			ThotSmtParameters tunedParameters = _modelWeightTuner.Tune(initialParameters, tuneSourceCorpus,
				tuneTargetCorpus, Stats, progress);
			Parameters = tunedParameters.Clone();
			Parameters.TranslationModelFileNamePrefix = oldParameters.TranslationModelFileNamePrefix;
			Parameters.LanguageModelFileNamePrefix = oldParameters.LanguageModelFileNamePrefix;
			Parameters.Freeze();
		}

		private static void FilterPhraseTableUsingCorpus(string fileName, IEnumerable<IEnumerable<string>> sourceCorpus)
		{
			var phrases = new HashSet<string>();
			foreach (IEnumerable<string> segment in sourceCorpus)
			{
				string[] segmentArray = segment.ToArray();
				for (int i = 0; i < segmentArray.Length; i++)
				{
					for (int j = 0; j < segmentArray.Length && j + i < segmentArray.Length; j++)
					{
						var phrase = new StringBuilder();
						for (int k = i; k <= i + j; k++)
						{
							if (k != i)
								phrase.Append(" ");
							phrase.Append(segmentArray[k]);
						}
						phrases.Add(phrase.ToString());
					}
				}
			}

			string tempFileName = fileName + ".temp";
			using (var reader = new StreamReader(fileName))
			using (var writer = new StreamWriter(tempFileName))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					string[] fields = line.Split(new[] { "|||" }, StringSplitOptions.RemoveEmptyEntries);
					string phrase = fields[1].Trim();
					if (phrases.Contains(phrase))
						writer.Write("{0}\n", line);
				}
			}
			File.Copy(tempFileName, fileName, true);
			File.Delete(tempFileName);
		}

		private void TrainTuneCorpus(string trainTMPrefix, string trainLMPrefix,
			IReadOnlyList<IReadOnlyList<string>> tuneSourceCorpus,
			IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus, IProgress<ProgressStatus> progress)
		{
			if (tuneSourceCorpus.Count == 0)
				return;

			ThotSmtParameters parameters = Parameters.Clone();
			parameters.TranslationModelFileNamePrefix = trainTMPrefix;
			parameters.LanguageModelFileNamePrefix = trainLMPrefix;
			using (var smtModel = new ThotSmtModel(_wordAlignmentModelType, parameters))
			using (IInteractiveTranslationEngine engine = smtModel.CreateInteractiveEngine())
			{
				for (int i = 0; i < tuneSourceCorpus.Count; i++)
				{
					progress.Report(new ProgressStatus(i, tuneSourceCorpus.Count));
					engine.TrainSegment(tuneSourceCorpus[i], tuneTargetCorpus[i]);
				}
				progress.Report(new ProgressStatus(1.0));
			}
		}

		private IEnumerable<ParallelTextSegment> GetTrainingSegments(ParallelTextCorpus corpus)
		{
			return GetSegments(corpus, i => !_tuneCorpusIndices.Contains(i));
		}

		private IEnumerable<ParallelTextSegment> GetTuningSegments(ParallelTextCorpus corpus)
		{
			return GetSegments(corpus, i => _tuneCorpusIndices.Contains(i));
		}

		private IEnumerable<ParallelTextSegment> GetSegments(ParallelTextCorpus corpus, Func<int, bool> filter)
		{
			int corpusCount = 0;
			int index = 0;
			foreach (ParallelTextSegment segment in corpus.Segments)
			{
				if (IsSegmentValid(segment))
				{
					if (filter(index))
						yield return segment;
					corpusCount++;
				}
				index++;
				if (corpusCount == _maxCorpusCount)
					break;
			}
		}

		private static bool IsSegmentValid(ParallelTextSegment segment)
		{
			return !segment.IsEmpty && segment.SourceSegment.Count <= TranslationConstants.MaxSegmentLength
				&& segment.TargetSegment.Count <= TranslationConstants.MaxSegmentLength;
		}

		protected override void DisposeManagedResources()
		{
			Directory.Delete(_tempDir, true);
		}

		private class EscapeTokenProcessor : ITokenProcessor
		{
			public IReadOnlyList<string> Process(IReadOnlyList<string> tokens)
			{
				return tokens.Select(Thot.EscapeToken).ToArray();
			}
		}
	}
}
