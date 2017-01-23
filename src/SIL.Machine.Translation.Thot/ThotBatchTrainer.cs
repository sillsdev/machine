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
using SIL.Progress;

namespace SIL.Machine.Translation.Thot
{
	public class ThotBatchTrainer
	{
		private const int TrainingStepCount = 27;
		private const double ProgressIncrement = 100.0 / TrainingStepCount;

		private readonly Func<string, string> _sourcePreprocessor;
		private readonly Func<string, string> _targetPreprocessor;
		private readonly ParallelTextCorpus _parallelCorpus;
		private readonly HashSet<int> _tuneCorpusIndices;
		private readonly ILLWeightTuner _llWeightTuner;

		public ThotBatchTrainer(string tmFileNamePrefix, string lmFileNamePrefix, ThotSmtParameters parameters, Func<string, string> sourcePreprocessor,
			ITextCorpus sourceCorpus, Func<string, string> targetPreprocessor, ITextCorpus targetCorpus, ITextAlignmentCorpus alignmentCorpus = null)
		{
			TranslationModelFileNamePrefix = tmFileNamePrefix;
			LanguageModelFileNamePrefix = lmFileNamePrefix;
			Parameters = parameters;
			_sourcePreprocessor = sourcePreprocessor;
			_targetPreprocessor = targetPreprocessor;
			_parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus, alignmentCorpus);
			_llWeightTuner = new MiraLLWeightTuner {ProgressIncrement = ProgressIncrement};
			_tuneCorpusIndices = CreateTuneCorpus();
		}

		public string TranslationModelFileNamePrefix { get; }
		public string LanguageModelFileNamePrefix { get; }
		public ThotSmtParameters Parameters { get; private set; }

		private HashSet<int> CreateTuneCorpus()
		{
			int corpusCount = 0;
			var emptyIndices = new HashSet<int>();
			int index = 0;
			foreach (ParallelTextSegment segment in _parallelCorpus.Texts.SelectMany(t => t.Segments))
			{
				if (segment.IsEmpty)
					emptyIndices.Add(index);
				else
					corpusCount++;
				index++;
			}
			int tuneCorpusCount = Math.Min((int)(corpusCount * 0.1), 1000);
			var r = new Random(31415);
			return new HashSet<int>(Enumerable.Range(0, corpusCount + emptyIndices.Count).Where(i => !emptyIndices.Contains(i))
				.OrderBy(i => r.Next()).Take(tuneCorpusCount));
		}

		public void Train(IProgress progress = null)
		{
			if (progress == null)
				progress = new NullProgress();

			string tempDir;
			do
			{
				tempDir = Path.Combine(Path.GetTempPath(), "thot-train-" + Guid.NewGuid());
			} while (Directory.Exists(tempDir));
			Directory.CreateDirectory(tempDir);
			try
			{
				string lmFilePrefix = Path.GetFileName(LanguageModelFileNamePrefix);
				Debug.Assert(lmFilePrefix != null);
				string tmFilePrefix = Path.GetFileName(TranslationModelFileNamePrefix);
				Debug.Assert(tmFilePrefix != null);

				string lmDir = Path.GetDirectoryName(LanguageModelFileNamePrefix);
				Debug.Assert(lmDir != null);
				string tmDir = Path.GetDirectoryName(TranslationModelFileNamePrefix);
				Debug.Assert(tmDir != null);

				string trainLMDir = Path.Combine(tempDir, "lm");
				Directory.CreateDirectory(trainLMDir);
				string trainLMPrefix = Path.Combine(trainLMDir, lmFilePrefix);
				string trainTMDir = Path.Combine(tempDir, "tm_train");
				Directory.CreateDirectory(trainTMDir);
				string trainTMPrefix = Path.Combine(trainTMDir, tmFilePrefix);

				if (progress.CancelRequested)
					return;

				progress.WriteMessage("Training target language model...");
				TrainLanguageModel(trainLMPrefix, 3);

				if (progress.CancelRequested)
					return;
				progress.ProgressIndicator.PercentCompleted += ProgressIncrement;

				TrainTranslationModel(trainTMPrefix, progress);

				if (progress.CancelRequested)
					return;

				string tuneTMDir = Path.Combine(tempDir, "tm_tune");
				Directory.CreateDirectory(tuneTMDir);
				string tuneTMPrefix = Path.Combine(tuneTMDir, tmFilePrefix);
				CopyFiles(trainTMDir, tuneTMDir, tmFilePrefix);

				var tuneSourceCorpus = new List<IReadOnlyList<string>>(_tuneCorpusIndices.Count);
				var tuneTargetCorpus = new List<IReadOnlyList<string>>(_tuneCorpusIndices.Count);
				foreach (ParallelTextSegment segment in _parallelCorpus.Segments.Where((s, i) => _tuneCorpusIndices.Contains(i)))
				{
					tuneSourceCorpus.Add(segment.SourceSegment.Select(w => _sourcePreprocessor(w)).ToArray());
					tuneTargetCorpus.Add(segment.TargetSegment.Select(w => _targetPreprocessor(w)).ToArray());
				}

				if (progress.CancelRequested)
					return;

				progress.WriteMessage("Tuning language model...");
				TuneLanguageModel(trainLMPrefix, tuneTargetCorpus, 3);

				if (progress.CancelRequested)
					return;
				progress.ProgressIndicator.PercentCompleted += ProgressIncrement;

				progress.WriteMessage("Tuning translation model...");
				TuneTranslationModel(tuneTMPrefix, trainLMPrefix, tuneSourceCorpus, tuneTargetCorpus, progress);

				if (progress.CancelRequested)
					return;
				progress.ProgressIndicator.PercentCompleted = ProgressIncrement * (TrainingStepCount - 1);

				progress.WriteMessage("Finalizing...");
				TrainTuneCorpus(trainTMPrefix, trainLMPrefix, tuneSourceCorpus, tuneTargetCorpus);

				if (progress.CancelRequested)
					return;

				if (!Directory.Exists(lmDir))
					Directory.CreateDirectory(lmDir);
				CopyFiles(trainLMDir, lmDir, lmFilePrefix);
				if (!Directory.Exists(tmDir))
					Directory.CreateDirectory(tmDir);
				CopyFiles(trainTMDir, tmDir, tmFilePrefix);
				progress.ProgressIndicator.PercentCompleted = 100;
			}
			finally
			{
				Directory.Delete(tempDir, true);
			}
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
			foreach (TextSegment segment in _parallelCorpus.TargetSegments.Where((s, i) => !_tuneCorpusIndices.Contains(i) && !s.IsEmpty))
			{
				var words = new List<string> { "<s>" };
				foreach (string word in segment.Segment.Select(w => _targetPreprocessor(w)))
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

			using (var writer = new StreamWriter(File.Open(lmPrefix, FileMode.Create)))
			{
				foreach (KeyValuePair<Ngram<string>, int> kvp in ngrams.OrderBy(kvp => kvp.Key.Length).ThenBy(kvp => string.Join(" ", kvp.Key)))
				{
					writer.Write("{0} {1} {2}\n", string.Join(" ", kvp.Key), kvp.Key.Length == 1 ? wordCount : ngrams[kvp.Key.TakeAllExceptLast()], kvp.Value);
				}
			}
		}

		private static void WriteLanguageModelWeightsFile(string lmPrefix, int ngramSize, IEnumerable<double> weights)
		{
			File.WriteAllText(lmPrefix + ".weights", string.Format("{0} 3 10 {1}\n", ngramSize, string.Join(" ", weights.Select(w => w.ToString("0.######")))));
		}

		private void WriteWordPredictionFile(string lmPrefix)
		{
			var rand = new Random(31415);
			using (var writer = new StreamWriter(File.Open(lmPrefix + ".wp", FileMode.Create)))
			{
				foreach (TextSegment segment in _parallelCorpus.TargetSegments.Where((s, i) => !_tuneCorpusIndices.Contains(i) && !s.IsEmpty)
					.Take(100000).OrderBy(i => rand.Next()))
				{
					writer.Write("{0}\n", string.Join(" ", segment.Segment.Select(w => _targetPreprocessor(w))));
				}
			}
		}

		private void TrainTranslationModel(string tmPrefix, IProgress progress)
		{
			string swmPrefix = tmPrefix + "_swm";
			GenerateSingleWordAlignmentModel(swmPrefix, _targetPreprocessor, _sourcePreprocessor, _parallelCorpus.Invert(),
				"source-to-target", progress);

			string invswmPrefix = tmPrefix + "_invswm";
			GenerateSingleWordAlignmentModel(invswmPrefix, _sourcePreprocessor, _targetPreprocessor, _parallelCorpus,
				"target-to-source", progress);

			if (progress.CancelRequested)
				return;

			progress.WriteMessage("Merging alignments...");
			Thot.giza_symmetr1(swmPrefix + ".bestal", invswmPrefix + ".bestal", tmPrefix + ".A3.final", true);

			if (progress.CancelRequested)
				return;
			progress.ProgressIndicator.PercentCompleted += ProgressIncrement;

			progress.WriteMessage("Generating phrase table...");
			Thot.phraseModel_generate(tmPrefix + ".A3.final", 10, tmPrefix + ".ttable");

			if (progress.CancelRequested)
				return;
			progress.ProgressIndicator.PercentCompleted += ProgressIncrement;

			progress.WriteMessage("Filtering phrase table...");
			FilterPhraseTableNBest(tmPrefix + ".ttable", 20);

			File.WriteAllText(tmPrefix + ".lambda", "0.7 0.7");
			File.WriteAllText(tmPrefix + ".srcsegmlentable", "Uniform");
			File.WriteAllText(tmPrefix + ".trgcutstable", "0.999");
			File.WriteAllText(tmPrefix + ".trgsegmlentable", "Geometric");

			if (progress.CancelRequested)
				return;
			progress.ProgressIndicator.PercentCompleted += ProgressIncrement;
		}

		private void GenerateSingleWordAlignmentModel(string swmPrefix, Func<string, string> sourcePreprocessor, Func<string, string> targetPreprocessor,
			ParallelTextCorpus corpus, string name, IProgress progress)
		{
			if (progress.CancelRequested)
				return;

			progress.WriteMessage("Training {0} single word model...", name);
			TrainSingleWordAlignmentModel(swmPrefix, sourcePreprocessor, targetPreprocessor, corpus, progress);

			if (progress.CancelRequested)
				return;

			PruneLexTable(swmPrefix + ".hmm_lexnd", 0.00001);

			if (progress.CancelRequested)
				return;

			progress.WriteMessage("Generating best {0} alignments...", name);
			GenerateBestAlignments(swmPrefix, swmPrefix + ".bestal", sourcePreprocessor, targetPreprocessor, corpus, progress);

			if (progress.CancelRequested)
				return;
			progress.ProgressIndicator.PercentCompleted += ProgressIncrement;
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
					entries.Add(Tuple.Create(uint.Parse(fields[0], CultureInfo.InvariantCulture), uint.Parse(fields[1], CultureInfo.InvariantCulture),
						float.Parse(fields[2], CultureInfo.InvariantCulture)));
				}
			}
#else
			using (var reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
			{
				int pos = 0;
				int length = (int)reader.BaseStream.Length;
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

					double lcSrc = groupEntries.Select(e => e.Item3).Skip(1).Aggregate((double)groupEntries[0].Item3, (a, n) => SumLog(a, n));

					double newLcSrc = -99999;
					int count = 0;
					foreach (Tuple<uint, uint, float> entry in groupEntries)
					{
						double prob = Math.Exp(entry.Item3 - lcSrc);
						if (prob < threshold)
							break;
						newLcSrc = SumLog(newLcSrc, entry.Item3);
						count++;
					}

					for (int i = 0; i < count; i++)
					{
#if THOT_TEXT_FORMAT
						writer.Write("{0} {1} {2:0.######} {3:0.######}\n", groupEntries[i].Item1, groupEntries[i].Item2, groupEntries[i].Item3, newLcSrc);
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

		private static double SumLog(double logx, double logy)
		{
			if (logx > logy)
				return logx + Math.Log(1 + Math.Exp(logy - logx));
			return logy + Math.Log(1 + Math.Exp(logx - logy));
		}

		private void TrainSingleWordAlignmentModel(string swmPrefix, Func<string, string> sourcePreprocessor, Func<string, string> targetPreprocessor, 
			ParallelTextCorpus corpus, IProgress progress)
		{
			using (var swAlignModel = new ThotSingleWordAlignmentModel(swmPrefix, true))
			{
				foreach (ParallelTextSegment segment in corpus.Segments.Where((s, i) => !_tuneCorpusIndices.Contains(i) && !s.IsEmpty))
				{
					IEnumerable<string> sourceTokens = segment.SourceSegment.Select(sourcePreprocessor);
					IEnumerable<string> targetTokens = segment.TargetSegment.Select(targetPreprocessor);
					swAlignModel.AddSegmentPair(sourceTokens, targetTokens, segment.Alignment);
				}
				for (int i = 0; i < 5; i++)
				{
					swAlignModel.Train(1);

					if (progress.CancelRequested)
						return;
					progress.ProgressIndicator.PercentCompleted += ProgressIncrement;
				}
				swAlignModel.Save();
			}
		}

		private void GenerateBestAlignments(string swmPrefix, string fileName, Func<string, string> sourcePreprocessor, Func<string, string> targetPreprocessor,
			ParallelTextCorpus corpus, IProgress progress)
		{
			using (var swAlignModel = new ThotSingleWordAlignmentModel(swmPrefix))
			using (var writer = new StreamWriter(File.Open(fileName, FileMode.Create)))
			{
				foreach (ParallelTextSegment segment in corpus.Segments.Where((s, i) => !_tuneCorpusIndices.Contains(i) && !s.IsEmpty))
				{
					string[] sourceTokens = segment.SourceSegment.Select(sourcePreprocessor).ToArray();
					string[] targetTokens = segment.TargetSegment.Select(targetPreprocessor).ToArray();

					WordAlignmentMatrix waMatrix;
					double prob = swAlignModel.GetBestAlignment(sourceTokens, targetTokens, out waMatrix);
					writer.Write("# Alignment probability= {0:0.######}\n", prob);
					writer.Write(waMatrix.ToGizaFormat(sourceTokens, targetTokens));

					if (progress.CancelRequested)
						return;
				}
			}
		}

		private static void FilterPhraseTableNBest(string fileName, int n)
		{
			var entries = new List<Tuple<string, string, float, float>>();
			using (var reader = new StreamReader(File.Open(fileName, FileMode.Open)))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					string[] fields = line.Split(new[] { "|||" }, StringSplitOptions.RemoveEmptyEntries);
					string counts = fields[2].Trim();
					int index = counts.IndexOf(" ", StringComparison.Ordinal);
					entries.Add(Tuple.Create(fields[0].Trim(), fields[1].Trim(), float.Parse(counts.Substring(0, index), CultureInfo.InvariantCulture),
						float.Parse(counts.Substring(index + 1), CultureInfo.InvariantCulture)));
				}
			}

			//TODO: do not sort phrase table in memory
			using (var writer = new StreamWriter(File.Open(fileName, FileMode.Create)))
			{
				foreach (IGrouping<string, Tuple<string, string, float, float>> g in entries.GroupBy(e => e.Item2).OrderBy(g => g.Key.Split(' ').Length).ThenBy(g => g.Key))
				{
					int count = 0;
					float remainder = 0;
					foreach (Tuple<string, string, float, float> entry in g.OrderByDescending(e => e.Item4).ThenBy(e => e.Item1.Split(' ').Length))
					{
						count++;
						if (count <= n)
							writer.Write("{0} ||| {1} ||| {2:0.########} {3:0.########}\n", entry.Item1, entry.Item2, entry.Item3, entry.Item4);
						else
							remainder += entry.Item4;
					}

					if (remainder > 0)
					{
						writer.Write("<UNUSED_WORD> ||| {0} ||| 0 {1:0.########}\n", g.Key, remainder);
					}
				}
			}
		}

		private static void TuneLanguageModel(string lmPrefix, IList<IReadOnlyList<string>> tuneTargetCorpus, int ngramSize)
		{
			if (tuneTargetCorpus.Count == 0)
				return;

			var simplex = new NelderMeadSimplex(0.1, 200, 1.0);
			MinimizationResult result = simplex.FindMinimum(w => CalculatePerplexity(tuneTargetCorpus, lmPrefix, ngramSize, w), Enumerable.Repeat(0.5, ngramSize * 3));
			WriteLanguageModelWeightsFile(lmPrefix, ngramSize, result.MinimizingPoint);
		}

		private static double CalculatePerplexity(IList<IReadOnlyList<string>> tuneTargetCorpus, string lmPrefix, int ngramSize, Vector weights)
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

		private void TuneTranslationModel(string tuneTMPrefix, string tuneLMPrefix, IReadOnlyList<IReadOnlyList<string>> tuneSourceCorpus,
			IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus, IProgress progress)
		{
			if (tuneSourceCorpus.Count == 0)
				return;

			string phraseTableFileName = tuneTMPrefix + ".ttable";
			FilterPhraseTableUsingCorpus(phraseTableFileName, tuneSourceCorpus);
			FilterPhraseTableNBest(phraseTableFileName, 20);

			float[] initialWeights = {1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0f};
			IReadOnlyList<float> bestWeights = _llWeightTuner.Tune(tuneTMPrefix, tuneLMPrefix, Parameters, tuneSourceCorpus, tuneTargetCorpus, initialWeights, progress);
			Parameters = Parameters.Clone();
			Parameters.ModelWeights = bestWeights;
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
			using (var reader = new StreamReader(File.Open(fileName, FileMode.Open)))
			using (var writer = new StreamWriter(File.Open(tempFileName, FileMode.Create)))
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

		private void TrainTuneCorpus(string trainTMPrefix, string trainLMPrefix, IReadOnlyList<IReadOnlyList<string>> tuneSourceCorpus,
			IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus)
		{
			if (tuneSourceCorpus.Count == 0)
				return;

			using (var smtModel = new ThotSmtModel(trainTMPrefix, trainLMPrefix, Parameters))
			using (ISmtEngine engine = smtModel.CreateEngine())
			{
				for (int i = 0; i < tuneSourceCorpus.Count; i++)
					engine.TrainSegment(tuneSourceCorpus[i], tuneTargetCorpus[i]);
			}
		}
	}
}
