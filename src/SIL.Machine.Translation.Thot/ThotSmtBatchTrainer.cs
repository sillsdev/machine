using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SIL.Extensions;
using SIL.Machine.Corpora;
using SIL.Machine.NgramModeling;
using SIL.Machine.Optimization;
using SIL.ObjectModel;
using SIL.Machine.Statistics;

namespace SIL.Machine.Translation.Thot
{
	public class ThotSmtBatchTrainer : DisposableBase, ISmtBatchTrainer
	{
		private const int TrainingStepCount = 27;

		private readonly Func<string, string> _sourcePreprocessor;
		private readonly Func<string, string> _targetPreprocessor;
		private readonly ParallelTextCorpus _parallelCorpus;
		private readonly HashSet<int> _tuneCorpusIndices;
		private readonly IParameterTuner _modelWeightTuner;
		private readonly string _tempDir;
		private readonly string _lmFilePrefix;
		private readonly string _tmFilePrefix;
		private readonly string _trainLMDir;
		private readonly string _trainTMDir;
		private readonly int _maxCorpusCount;

		public ThotSmtBatchTrainer(string cfgFileName, Func<string, string> sourcePreprocessor,
			Func<string, string> targetPreprocessor, ParallelTextCorpus corpus, int maxCorpusCount = int.MaxValue)
			: this(ThotSmtParameters.Load(cfgFileName), sourcePreprocessor, targetPreprocessor, corpus, maxCorpusCount)
		{
			ConfigFileName = cfgFileName;
		}

		public ThotSmtBatchTrainer(ThotSmtParameters parameters, Func<string, string> sourcePreprocessor,
			Func<string, string> targetPreprocessor, ParallelTextCorpus corpus, int maxCorpusCount = int.MaxValue)
		{
			Parameters = parameters;
			Parameters.Freeze();
			_sourcePreprocessor = sourcePreprocessor;
			_targetPreprocessor = targetPreprocessor;
			_maxCorpusCount = maxCorpusCount;
			_parallelCorpus = corpus;
			//_modelWeightTuner = new MiraModelWeightTuner();
			_modelWeightTuner = new SimplexModelWeightTuner {ProgressIncrementInterval = 10};
			_tuneCorpusIndices = CreateTuneCorpus();

			do
			{
				_tempDir = Path.Combine(Path.GetTempPath(), "thot-train-" + Guid.NewGuid());
			} while (Directory.Exists(_tempDir));
			Directory.CreateDirectory(_tempDir);

			_lmFilePrefix = Path.GetFileName(Parameters.LanguageModelFileNamePrefix);
			_tmFilePrefix = Path.GetFileName(Parameters.TranslationModelFileNamePrefix);
			_trainLMDir = Path.Combine(_tempDir, "lm");
			_trainTMDir = Path.Combine(_tempDir, "tm_train");
		}

		public string ConfigFileName { get; }
		public ThotSmtParameters Parameters { get; private set; }
		public SmtBatchTrainStats Stats { get; } = new SmtBatchTrainStats();

		private HashSet<int> CreateTuneCorpus()
		{
			int corpusCount = 0;
			var emptyIndices = new HashSet<int>();
			int index = 0;
			foreach (ParallelTextSegment segment in _parallelCorpus.Segments)
			{
				if (segment.IsEmpty)
					emptyIndices.Add(index);
				else
					corpusCount++;
				index++;
				if (corpusCount == _maxCorpusCount)
					break;
			}
			Stats.TrainedSegmentCount = corpusCount;
			int tuneCorpusCount = Math.Min((int)(corpusCount * 0.1), 1000);
			var r = new Random(31415);
			return new HashSet<int>(Enumerable.Range(0, corpusCount + emptyIndices.Count)
				.Where(i => !emptyIndices.Contains(i))
				.OrderBy(i => r.Next()).Take(tuneCorpusCount));
		}

		public virtual void Train(IProgress<ProgressData> progress = null, Action checkCanceled = null)
		{
			var reporter = new ThotTrainProgressReporter(TrainingStepCount, progress, checkCanceled);

			Directory.CreateDirectory(_trainLMDir);
			string trainLMPrefix = Path.Combine(_trainLMDir, _lmFilePrefix);
			Directory.CreateDirectory(_trainTMDir);
			string trainTMPrefix = Path.Combine(_trainTMDir, _tmFilePrefix);

			TrainLanguageModel(trainLMPrefix, 3, reporter);

			reporter.CheckCanceled();

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
				tuneSourceCorpus.Add(segment.SourceSegment.Select(w => _sourcePreprocessor(w)).ToArray());
				tuneTargetCorpus.Add(segment.TargetSegment.Select(w => _targetPreprocessor(w)).ToArray());
			}

			TuneLanguageModel(trainLMPrefix, tuneTargetCorpus, 3, reporter);

			reporter.CheckCanceled();

			TuneTranslationModel(tuneTMPrefix, trainLMPrefix, tuneSourceCorpus, tuneTargetCorpus, reporter);

			reporter.CheckCanceled();

			TrainTuneCorpus(trainTMPrefix, trainLMPrefix, tuneSourceCorpus, tuneTargetCorpus, reporter);

			reporter.Step("Completed");
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

		private void TrainLanguageModel(string lmPrefix, int ngramSize, ThotTrainProgressReporter reporter)
		{
			reporter.Step("Training target language model");
			WriteNgramCountsFile(lmPrefix, ngramSize);
			WriteLanguageModelWeightsFile(lmPrefix, ngramSize, Enumerable.Repeat(0.5, ngramSize * 3));
			WriteWordPredictionFile(lmPrefix);
		}

		private void WriteNgramCountsFile(string lmPrefix, int ngramSize)
		{
			int wordCount = 0;
			var ngrams = new Dictionary<Ngram<string>, int>();
			var vocab = new HashSet<string>();
			foreach (ParallelTextSegment segment in GetTrainingSegments(_parallelCorpus))
			{
				var words = new List<string> { "<s>" };
				foreach (string word in segment.TargetSegment.Select(w => _targetPreprocessor(w)))
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
					writer.Write("{0}\n", string.Join(" ", segment.Segment.Select(w => _targetPreprocessor(w))));
				}
			}
		}

		private void TrainTranslationModel(string tmPrefix, ThotTrainProgressReporter reporter)
		{
			string invswmPrefix = tmPrefix + "_invswm";
			GenerateSingleWordAlignmentModel(invswmPrefix, _sourcePreprocessor, _targetPreprocessor, _parallelCorpus,
				"direct", reporter);

			string swmPrefix = tmPrefix + "_swm";
			GenerateSingleWordAlignmentModel(swmPrefix, _targetPreprocessor, _sourcePreprocessor,
				_parallelCorpus.Invert(), "inverse", reporter);

			reporter.Step("Merging alignments");

			Thot.giza_symmetr1(swmPrefix + ".bestal", invswmPrefix + ".bestal", tmPrefix + ".A3.final", true);

			reporter.Step("Generating phrase table");

			Thot.phraseModel_generate(tmPrefix + ".A3.final", 10, tmPrefix + ".ttable");

			reporter.Step("Filtering phrase table");

			FilterPhraseTableNBest(tmPrefix + ".ttable", 20);

			File.WriteAllText(tmPrefix + ".lambda", "0.7 0.7");
			File.WriteAllText(tmPrefix + ".srcsegmlentable", "Uniform");
			File.WriteAllText(tmPrefix + ".trgcutstable", "0.999");
			File.WriteAllText(tmPrefix + ".trgsegmlentable", "Geometric");
		}

		private void GenerateSingleWordAlignmentModel(string swmPrefix, Func<string, string> sourcePreprocessor,
			Func<string, string> targetPreprocessor, ParallelTextCorpus corpus, string name,
			ThotTrainProgressReporter reporter)
		{
			TrainSingleWordAlignmentModel(swmPrefix, sourcePreprocessor, targetPreprocessor, corpus, name, reporter);

			reporter.CheckCanceled();

			PruneLexTable(swmPrefix + ".hmm_lexnd", 0.00001);

			GenerateBestAlignments(swmPrefix, swmPrefix + ".bestal", sourcePreprocessor, targetPreprocessor, corpus,
				name, reporter);
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
				var length = (int) reader.BaseStream.Length;
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
						.Aggregate((double) groupEntries[0].Item3, (a, n) => LogSpace.Add(a, n));

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
						writer.Write((float) newLcSrc);
#endif
					}
				}
			}
		}

		private void TrainSingleWordAlignmentModel(string swmPrefix, Func<string, string> sourcePreprocessor,
			Func<string, string> targetPreprocessor, ParallelTextCorpus corpus, string name,
			ThotTrainProgressReporter reporter)
		{
			using (var swAlignModel = new ThotWordAlignmentModel(swmPrefix, true))
			{
				foreach (ParallelTextSegment segment in GetTrainingSegments(corpus))
					swAlignModel.AddSegmentPair(segment, true, sourcePreprocessor, targetPreprocessor);
				for (int i = 0; i < 5; i++)
				{
					reporter.Step($"Training {name} alignment model");

					swAlignModel.TrainingIteration();
				}
				swAlignModel.Save();
			}
		}

		private void GenerateBestAlignments(string swmPrefix, string fileName, Func<string, string> sourcePreprocessor,
			Func<string, string> targetPreprocessor, ParallelTextCorpus corpus, string name,
			ThotTrainProgressReporter reporter)
		{
			reporter.Step($"Generating best {name} alignments");

			using (var swAlignModel = new ThotWordAlignmentModel(swmPrefix))
			using (var writer = new StreamWriter(fileName))
			{
				foreach (ParallelTextSegment segment in GetTrainingSegments(corpus))
				{
					string[] sourceTokens = segment.SourceSegment.Select(sourcePreprocessor).ToArray();
					string[] targetTokens = segment.TargetSegment.Select(targetPreprocessor).ToArray();
					WordAlignmentMatrix waMatrix = swAlignModel.GetBestAlignment(sourceTokens, targetTokens,
						segment.CreateAlignmentMatrix(true));

					writer.Write($"# {segment.Text.Id} {segment.SegmentRef}\n");
					writer.Write(waMatrix.ToGizaFormat(sourceTokens, targetTokens));

					reporter.CheckCanceled();
				}
			}
		}

		private static void FilterPhraseTableNBest(string fileName, int n)
		{
			var entries = new List<Tuple<string, string, float, float>>();
			using (var reader = new StreamReader(fileName))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					string[] fields = line.Split(new[] { "|||" }, StringSplitOptions.RemoveEmptyEntries);
					string counts = fields[2].Trim();
					int index = counts.IndexOf(" ", StringComparison.Ordinal);
					entries.Add(Tuple.Create(fields[0].Trim(), fields[1].Trim(), float.Parse(counts.Substring(0, index),
						CultureInfo.InvariantCulture), float.Parse(counts.Substring(index + 1),
						CultureInfo.InvariantCulture)));
				}
			}

			//TODO: do not sort phrase table in memory
			using (var writer = new StreamWriter(fileName))
			{
				foreach (IGrouping<string, Tuple<string, string, float, float>> g in entries.GroupBy(e => e.Item2)
					.OrderBy(g => g.Key.Split(' ').Length).ThenBy(g => g.Key))
				{
					int count = 0;
					float remainder = 0;
					foreach (Tuple<string, string, float, float> entry in g.OrderByDescending(e => e.Item4)
						.ThenBy(e => e.Item1.Split(' ').Length))
					{
						count++;
						if (count <= n)
						{
							writer.Write("{0} ||| {1} ||| {2:0.########} {3:0.########}\n", entry.Item1, entry.Item2,
								entry.Item3, entry.Item4);
						}
						else
						{
							remainder += entry.Item4;
						}
					}

					if (remainder > 0)
					{
						writer.Write("<UNUSED_WORD> ||| {0} ||| 0 {1:0.########}\n", g.Key, remainder);
					}
				}
			}
		}

		private void TuneLanguageModel(string lmPrefix, IList<IReadOnlyList<string>> tuneTargetCorpus,
			int ngramSize, ThotTrainProgressReporter reporter)
		{
			reporter.Step("Tuning target language model");

			if (tuneTargetCorpus.Count == 0)
				return;

			var simplex = new NelderMeadSimplex(0.1, 200, 1.0);
			MinimizationResult result = simplex.FindMinimum(w =>
				CalculatePerplexity(tuneTargetCorpus, lmPrefix, ngramSize, w), Enumerable.Repeat(0.5, ngramSize * 3));
			WriteLanguageModelWeightsFile(lmPrefix, ngramSize, result.MinimizingPoint);
			Stats.LanguageModelPerplexity = result.ErrorValue;
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
					lp += lm.GetSegmentProbability(segment);
					wordCount += segment.Count;
				}
			}

			return Math.Exp(-(lp / (wordCount + tuneTargetCorpus.Count)) * Math.Log(10));
		}

		private void TuneTranslationModel(string tuneTMPrefix, string tuneLMPrefix,
			IReadOnlyList<IReadOnlyList<string>> tuneSourceCorpus,
			IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus, ThotTrainProgressReporter reporter)
		{
			reporter.Step("Tuning translation model");

			if (tuneSourceCorpus.Count == 0)
				return;

			string phraseTableFileName = tuneTMPrefix + ".ttable";
			FilterPhraseTableUsingCorpus(phraseTableFileName, tuneSourceCorpus);
			FilterPhraseTableNBest(phraseTableFileName, 20);

			ThotSmtParameters oldParameters = Parameters;
			ThotSmtParameters initialParameters = oldParameters.Clone();
			initialParameters.TranslationModelFileNamePrefix = tuneTMPrefix;
			initialParameters.LanguageModelFileNamePrefix = tuneLMPrefix;
			initialParameters.ModelWeights = new[] {1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0f};
			initialParameters.Freeze();

			ThotSmtParameters tunedParameters = _modelWeightTuner.Tune(initialParameters, tuneSourceCorpus,
				tuneTargetCorpus, reporter, Stats);
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
			IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus, ThotTrainProgressReporter reporter)
		{
			reporter.Step("Finalizing", TrainingStepCount - 1);

			if (tuneSourceCorpus.Count == 0)
				return;

			ThotSmtParameters parameters = Parameters.Clone();
			parameters.TranslationModelFileNamePrefix = trainTMPrefix;
			parameters.LanguageModelFileNamePrefix = trainLMPrefix;
			using (var smtModel = new ThotSmtModel(parameters))
			using (ISmtEngine engine = smtModel.CreateEngine())
			{
				for (int i = 0; i < tuneSourceCorpus.Count; i++)
					engine.TrainSegment(tuneSourceCorpus[i], tuneTargetCorpus[i]);
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
				if (!segment.IsEmpty)
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

		protected override void DisposeManagedResources()
		{
			Directory.Delete(_tempDir, true);
		}
	}
}
