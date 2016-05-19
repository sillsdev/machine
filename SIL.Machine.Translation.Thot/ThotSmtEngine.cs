using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SIL.Extensions;
using SIL.Machine.NgramModeling;
using SIL.Machine.Optimization;
using SIL.ObjectModel;
using SIL.Progress;

namespace SIL.Machine.Translation.Thot
{
	internal delegate int TranslateFunc(IntPtr sessionHandle, IntPtr sourceSegment, IntPtr result, int capacity, out IntPtr data);

	public class ThotSmtEngine : DisposableBase, IInteractiveSmtEngine
	{
		private const int TrainingStepCount = 17;
		private const int ProgressIncrement = 99 / TrainingStepCount + 1;
		private const int DefaultTranslationBufferLength = 1024;

		public static void TrainModels(string cfgFileName, IEnumerable<IEnumerable<string>> sourceCorpus, IEnumerable<IEnumerable<string>> targetCorpus, IProgress progress = null)
		{
			if (progress == null)
				progress = new NullProgress();

			List<IEnumerable<string>> trainSourceCorpus = sourceCorpus.ToList();
			List<IEnumerable<string>> trainTargetCorpus = targetCorpus.ToList();
			if (trainSourceCorpus.Count != trainTargetCorpus.Count)
				throw new ArgumentException("The source and target corpora are not the same size");

			int tuneCorpusCount = Math.Min((int) (trainSourceCorpus.Count * 0.1), 1000);
			var tuneSourceCorpus = new List<IEnumerable<string>>();
			var tuneTargetCorpus = new List<IEnumerable<string>>();
			var r = new Random(31415);
			//foreach (int index in Enumerable.Range(0, 66).Reverse())
			foreach (int index in Enumerable.Range(0, trainSourceCorpus.Count).OrderBy(i => r.Next()).Take(tuneCorpusCount).OrderByDescending(i => i))
			{
				tuneSourceCorpus.Add(trainSourceCorpus[index]);
				tuneTargetCorpus.Add(trainTargetCorpus[index]);
				trainSourceCorpus.RemoveAt(index);
				trainTargetCorpus.RemoveAt(index);
			}

			string cfgDir = Path.GetDirectoryName(cfgFileName);
			Debug.Assert(cfgDir != null);
			string tmPrefix = null, lmPrefix = null;
			foreach (string line in File.ReadAllLines(cfgFileName))
			{
				string l = line.Trim();
				if (l.StartsWith("-tm "))
					tmPrefix = l.Substring(4).Trim();
				else if (l.StartsWith("-lm "))
					lmPrefix = l.Substring(4).Trim();
			}

			if (string.IsNullOrEmpty(lmPrefix))
				throw new InvalidOperationException("The configuration file does not specify the language model parameter.");
			if (string.IsNullOrEmpty(tmPrefix))
				throw new InvalidOperationException("The configuration file does not specify the translation model parameter.");

			string tempDir;
			do
			{
				tempDir = Path.Combine(Path.GetTempPath(), "thot-train-" + Guid.NewGuid());
			} while (Directory.Exists(tempDir));
			Directory.CreateDirectory(tempDir);
			try
			{
				string lmFilePrefix = Path.GetFileName(lmPrefix);
				string tmFilePrefix = Path.GetFileName(tmPrefix);

				string fullLMPrefix = lmPrefix;
				if (!Path.IsPathRooted(lmPrefix))
					fullLMPrefix = Path.Combine(cfgDir, lmPrefix);
				string fullTMPrefix = tmPrefix;
				if (!Path.IsPathRooted(tmPrefix))
					fullTMPrefix = Path.Combine(cfgDir, tmPrefix);

				string lmDir = Path.GetDirectoryName(fullLMPrefix);
				string tmDir = Path.GetDirectoryName(fullTMPrefix);

				string trainLMDir = Path.Combine(tempDir, "lm");
				Directory.CreateDirectory(trainLMDir);
				string trainLMPrefix = Path.Combine(trainLMDir, lmFilePrefix);
				string trainTMDir = Path.Combine(tempDir, "tm_train");
				Directory.CreateDirectory(trainTMDir);
				string trainTMPrefix = Path.Combine(trainTMDir, tmFilePrefix);

				string trainCfgFileName = Path.Combine(tempDir, "train.cfg");
				File.Copy(cfgFileName, trainCfgFileName);
				UpdateConfigPaths(trainCfgFileName, trainLMPrefix, trainTMPrefix);

				progress.WriteMessage("Training target language model...");
				TrainLanguageModel(trainTargetCorpus, trainLMPrefix, 3);
				progress.ProgressIndicator.PercentCompleted += ProgressIncrement;
				if (progress.CancelRequested)
					return;

				TrainTranslationModel(trainSourceCorpus, trainTargetCorpus, trainTMPrefix, progress);
				if (progress.CancelRequested)
					return;

				string tuneTMDir = Path.Combine(tempDir, "tm_tune");
				Directory.CreateDirectory(tuneTMDir);
				string tuneTMPrefix = Path.Combine(tuneTMDir, tmFilePrefix);
				CopyFiles(trainTMDir, tuneTMDir, tmFilePrefix);

				string tuneCfgFileName = Path.Combine(tempDir, "tune.cfg");
				File.Copy(trainCfgFileName, tuneCfgFileName);
				UpdateConfigPaths(tuneCfgFileName, trainLMPrefix, tuneTMPrefix);
				progress.ProgressIndicator.PercentCompleted = 100;

				progress.ProgressIndicator.IndicateUnknownProgress();
				progress.WriteMessage("Tuning language model...");
				TuneLanguageModel(tuneTargetCorpus, trainLMPrefix, 3);
				if (progress.CancelRequested)
					return;

				progress.WriteMessage("Tuning translation model...");
				TuneTranslationModel(tuneSourceCorpus, tuneTargetCorpus, tuneCfgFileName, tuneTMPrefix);
				if (progress.CancelRequested)
					return;

				UpdateConfigPaths(tuneCfgFileName, lmPrefix, tmPrefix);

				CopyFiles(trainLMDir, lmDir, lmFilePrefix);
				CopyFiles(trainTMDir, tmDir, tmFilePrefix);
				File.Copy(tuneCfgFileName, cfgFileName, true);
			}
			finally
			{
				Directory.Delete(tempDir, true);
			}
		}

		private static void UpdateConfigPaths(string cfgFileName, string lmPrefix, string tmPrefix)
		{
			string[] lines = File.ReadAllLines(cfgFileName);
			using (var writer = new StreamWriter(cfgFileName))
			{
				foreach (string line in lines)
				{
					string l = line.Trim();
					if (l.StartsWith("-tm "))
						writer.Write("-tm {0}\n", tmPrefix);
					else if (l.StartsWith("-lm "))
						writer.Write("-lm {0}\n", lmPrefix);
					else
						writer.Write("{0}\n", line);
				}
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

		private static void TrainLanguageModel(IList<IEnumerable<string>> targetCorpus, string lmPrefix, int ngramSize)
		{
			WriteNgramCountsFile(targetCorpus, lmPrefix, ngramSize);
			WriteLanguageModelWeightsFile(lmPrefix, ngramSize, Enumerable.Repeat(0.5, ngramSize * 3));
			WriteWordPredictionFile(targetCorpus, lmPrefix);
		}

		private static void WriteNgramCountsFile(IList<IEnumerable<string>> targetCorpus, string lmPrefix, int ngramSize)
		{
			int wordCount = 0;
			var ngrams = new Dictionary<Ngram<string>, int>();
			var vocab = new HashSet<string>();
			foreach (IEnumerable<string> segment in targetCorpus)
			{
				var words = new List<string> {"<s>"};
				foreach (string word in segment)
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
				foreach (KeyValuePair<Ngram<string>, int> kvp in ngrams.OrderBy(kvp => kvp.Key.Length).ThenBy(kvp => string.Join(" ", kvp.Key)))
				{
					writer.Write("{0} {1} {2}\n", string.Join(" ", kvp.Key), kvp.Key.Length == 1 ? wordCount : ngrams[kvp.Key.TakeAllExceptLast()], kvp.Value);
				}
			}
		}

		private static void WriteLanguageModelWeightsFile(string lmPrefix, int ngramSize, IEnumerable<double> weights)
		{
			File.WriteAllText(lmPrefix + ".weights", string.Format("{0} 3 10 {1}\n", ngramSize, string.Join(" ", weights)));
		}

		private static void WriteWordPredictionFile(IList<IEnumerable<string>> targetCorpus, string lmPrefix)
		{
			var rand = new Random(31415);
			using (var writer = new StreamWriter(lmPrefix + ".wp"))
			{
				foreach (IEnumerable<string> segment in targetCorpus.Where(s => s.Any()).Take(100000).OrderBy(i => rand.Next()))
					writer.Write("{0}\n", string.Join(" ", segment));
			}
		}

		private static void TrainTranslationModel(IList<IEnumerable<string>> sourceCorpus, IList<IEnumerable<string>> targetCorpus, string tmPrefix,
			IProgress progress)
		{
			string swmPrefix = tmPrefix + "_swm";
			GenerateSingleWordAlignmentModel(swmPrefix, targetCorpus, sourceCorpus, progress, "source-to-target");

			string invswmPrefix = tmPrefix + "_invswm";
			GenerateSingleWordAlignmentModel(invswmPrefix, sourceCorpus, targetCorpus, progress, "target-to-source");

			progress.WriteMessage("Merging alignments...");
			Thot.giza_symmetr1(swmPrefix + ".bestal", invswmPrefix + ".bestal", tmPrefix + ".A3.final", true);
			progress.ProgressIndicator.PercentCompleted += ProgressIncrement;
			if (progress.CancelRequested)
				return;

			progress.WriteMessage("Generating phrase table...");
			Thot.phraseModel_generate(tmPrefix + ".A3.final", 7, tmPrefix + ".ttable");
			progress.ProgressIndicator.PercentCompleted += ProgressIncrement;
			if (progress.CancelRequested)
				return;

			progress.WriteMessage("Filtering phrase table...");
			FilterPhraseTableNBest(tmPrefix + ".ttable", 20);

			File.WriteAllText(tmPrefix + ".lambda", "0.9");
			File.WriteAllText(tmPrefix + ".srcsegmlentable", "Uniform");
			File.WriteAllText(tmPrefix + ".trgsegmlentable", "Geometric");
			progress.ProgressIndicator.PercentCompleted += ProgressIncrement;
		}

		private static void GenerateSingleWordAlignmentModel(string swmPrefix, IList<IEnumerable<string>> sourceCorpus,
			IList<IEnumerable<string>> targetCorpus, IProgress progress, string name)
		{
			progress.WriteMessage("Training {0} single word model...", name);
			TrainSingleWordAlignmentModel(swmPrefix, sourceCorpus, targetCorpus, progress);
			if (progress.CancelRequested)
				return;

			PruneLexTable(swmPrefix + ".hmm_lexnd", 0.00001);
			if (progress.CancelRequested)
				return;

			progress.WriteMessage("Generating best {0} alignments...", name);
			GenerateBestAlignments(swmPrefix, swmPrefix + ".bestal", sourceCorpus, targetCorpus, progress);
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
				int length = (int) reader.BaseStream.Length;
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

					double lcSrc = groupEntries.Select(e => e.Item3).Skip(1).Aggregate((double) groupEntries[0].Item3, (a, n) => SumLog(a, n));

					double newLcSrc = -99;
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
						writer.Write((float) newLcSrc);
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

		private static void TrainSingleWordAlignmentModel(string swmPrefix, IList<IEnumerable<string>> sourceCorpus,
			IList<IEnumerable<string>> targetCorpus, IProgress progress)
		{
			using (var swAlignModel = new ThotSingleWordAlignmentModel(swmPrefix, true))
			{
				foreach (Tuple<string[], string[]> pair in sourceCorpus.Select(s => s.ToArray()).Zip(targetCorpus.Select(s => s.ToArray()))
					.Where(p => p.Item1.Length > 0 && p.Item2.Length > 0))
				{
					swAlignModel.AddSegmentPair(pair.Item1, pair.Item2);
				}
				for (int i = 0; i < 5; i++)
				{
					swAlignModel.Train(1);
					progress.ProgressIndicator.PercentCompleted += ProgressIncrement;
					if (progress.CancelRequested)
						return;
				}
				swAlignModel.Save();
			}
		}

		private static void GenerateBestAlignments(string swmPrefix, string fileName, IList<IEnumerable<string>> sourceCorpus,
			IList<IEnumerable<string>> targetCorpus, IProgress progress)
		{
			using (var swAlignModel = new ThotSingleWordAlignmentModel(swmPrefix))
			using (var writer = new StreamWriter(fileName))
			{
				foreach (Tuple<string[], string[]> pair in sourceCorpus.Select(s => s.ToArray()).Zip(targetCorpus.Select(s => s.ToArray()))
					.Where(p => p.Item1.Length > 0 && p.Item2.Length > 0))
				{
					WordAlignmentMatrix waMatrix;
					double prob = swAlignModel.GetBestAlignment(pair.Item1, pair.Item2, out waMatrix);
					writer.Write("# Alignment probability= {0:0.######}\n", prob);
					writer.Write(waMatrix.ToGizaFormat(pair.Item1, pair.Item2));
					if (progress.CancelRequested)
						return;
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
					string[] fields = line.Split(new[] {"|||"}, StringSplitOptions.RemoveEmptyEntries);
					string counts = fields[2].Trim();
					int index = counts.IndexOf(" ", StringComparison.Ordinal);
					entries.Add(Tuple.Create(fields[0].Trim(), fields[1].Trim(), float.Parse(counts.Substring(0, index), CultureInfo.InvariantCulture),
						float.Parse(counts.Substring(index + 1), CultureInfo.InvariantCulture)));
				}
			}

			using (var writer = new StreamWriter(fileName))
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

		private static void TuneLanguageModel(IList<IEnumerable<string>> targetCorpus, string lmPrefix, int ngramSize)
		{
			var simplex = new NelderMeadSimplex(0.1, 200, 1.0);
			var initialGuess = new Vector(Enumerable.Repeat(0.5, ngramSize * 3));
			MinimizationResult result = simplex.FindMinimum(w => CalculatePerplexity(targetCorpus, lmPrefix, ngramSize, w), initialGuess);
			WriteLanguageModelWeightsFile(lmPrefix, ngramSize, result.MinimizingPoint);
		}

		private static double CalculatePerplexity(IList<IEnumerable<string>> targetCorpus, string lmPrefix, int ngramSize, Vector weights)
		{
			if (weights.Any(w => w < 0 || w >= 1.0))
				return 999999;

			WriteLanguageModelWeightsFile(lmPrefix, ngramSize, weights);
			double lp = 0;
			int wordCount = 0;
			using (var lm = new ThotLanguageModel(lmPrefix))
			{
				foreach (IEnumerable<string> segment in targetCorpus)
				{
					string[] segmentArray = segment.ToArray();
					lp += lm.GetSegmentProbability(segmentArray);
					wordCount += segmentArray.Length;
				}
			}

			return Math.Exp(-(lp / (wordCount + targetCorpus.Count)) * Math.Log(10));
		}

		private static void TuneTranslationModel(IList<IEnumerable<string>> sourceCorpus, IList<IEnumerable<string>> targetCorpus, string cfgFileName, string tmPrefix)
		{
			string phraseTableFileName = tmPrefix + ".ttable";
			FilterPhraseTableUsingCorpus(phraseTableFileName, sourceCorpus);
			FilterPhraseTableNBest(phraseTableFileName, 20);

			var simplex = new NelderMeadSimplex(0.001, 200, 1.0);
			var initialGuess = new Vector(new[] {1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0});
			MinimizationResult result = simplex.FindMinimum(w => CalculateBleu(sourceCorpus, targetCorpus, cfgFileName, w), initialGuess);
			UpdateConfigWeights(cfgFileName, result.MinimizingPoint);
		}

		private static void FilterPhraseTableUsingCorpus(string fileName, IList<IEnumerable<string>> sourceCorpus)
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
					string[] fields = line.Split(new[] {"|||"}, StringSplitOptions.RemoveEmptyEntries);
					string phrase = fields[1].Trim();
					if (phrases.Contains(phrase))
						writer.Write("{0}\n", line);
				}
			}
			File.Copy(tempFileName, fileName, true);
			File.Delete(tempFileName);
		}

		private static double CalculateBleu(IList<IEnumerable<string>> sourceCorpus, IList<IEnumerable<string>> targetCorpus, string cfgFileName, Vector weights)
		{
			UpdateConfigWeights(cfgFileName, weights);
			IntPtr decoderHandle = IntPtr.Zero, sessionHandle = IntPtr.Zero;
			try
			{
				decoderHandle = Thot.decoder_open(cfgFileName);
				sessionHandle = Thot.decoder_openSession(decoderHandle);
				double bleu = Evaluation.CalculateBleu(GenerateTranslations(sessionHandle, sourceCorpus), targetCorpus);
				double penalty = 0;
				for (int i = 0; i < weights.Count; i++)
				{
					if (i == 0 || i == 2)
						continue;

					if (weights[i] < 0)
						penalty += weights[i] * 1000 * -1;
				}
				return (1.0 - bleu) + penalty;
			}
			finally
			{
				if (sessionHandle != IntPtr.Zero)
					Thot.session_close(sessionHandle);
				if (decoderHandle != IntPtr.Zero)
					Thot.decoder_close(decoderHandle);
			}
		}

		private static void UpdateConfigWeights(string cfgFileName, IEnumerable<double> weights)
		{
			string[] lines = File.ReadAllLines(cfgFileName);
			using (var writer = new StreamWriter(cfgFileName))
			{
				bool weightsWritten = false;
				foreach (string line in lines)
				{
					string l = line.Trim();
					if (l.Contains("-tmw"))
					{
						WriteWeights(writer, weights);
						weightsWritten = true;
					}
					else
					{
						writer.Write("{0}\n", line);
					}
				}

				if (!weightsWritten)
					WriteWeights(writer, weights);
			}
		}

		private static void WriteWeights(StreamWriter writer, IEnumerable<double> weights)
		{
			writer.Write("-tmw {0} 0\n", string.Join(" ", weights.Select(w => w.ToString("0.######"))));
		}

		private static IEnumerable<IEnumerable<string>> GenerateTranslations(IntPtr sessionHandle, IList<IEnumerable<string>> sourceCorpus)
		{
			foreach (IEnumerable<string> segment in sourceCorpus)
			{
				string[] segmentArray = segment.ToArray();
				yield return DoTranslate(sessionHandle, Thot.session_translate, segmentArray, false, segmentArray, (s, t, d) => t);
			}
		}

		internal static T DoTranslate<T>(IntPtr sessionHandle, TranslateFunc translateFunc, IEnumerable<string> input, bool addTrailingSpace,
			IList<string> sourceSegment, Func<IList<string>, IList<string>, IntPtr, T> createResult)
		{
			IntPtr inputPtr = Thot.ConvertStringToNativeUtf8(string.Join(" ", input) + (addTrailingSpace ? " " : ""));
			IntPtr translationPtr = Marshal.AllocHGlobal(DefaultTranslationBufferLength);
			IntPtr data = IntPtr.Zero;
			try
			{
				int len = translateFunc(sessionHandle, inputPtr, translationPtr, DefaultTranslationBufferLength, out data);
				if (len > DefaultTranslationBufferLength)
				{
					Thot.tdata_destroy(data);
					translationPtr = Marshal.ReAllocHGlobal(translationPtr, (IntPtr) len);
					len = translateFunc(sessionHandle, inputPtr, translationPtr, len, out data);
				}
				string translation = Thot.ConvertNativeUtf8ToString(translationPtr, len);
				string[] targetSegment = translation.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
				return createResult(sourceSegment, targetSegment, data);
			}
			finally
			{
				if (data != IntPtr.Zero)
					Thot.tdata_destroy(data);
				Marshal.FreeHGlobal(translationPtr);
				Marshal.FreeHGlobal(inputPtr);
			}
		}

		private readonly string _cfgFileName;
		private IntPtr _handle;
		private readonly HashSet<ThotSmtSession> _sessions;
		private ThotSmtSession _globalSession;
		private readonly ThotSingleWordAlignmentModel _singleWordAlignmentModel;
		private readonly ThotSingleWordAlignmentModel _inverseSingleWordAlignmentModel;

		public ThotSmtEngine(string cfgFileName)
		{
			_cfgFileName = cfgFileName;
			_sessions = new HashSet<ThotSmtSession>();
			_handle = Thot.decoder_open(_cfgFileName);
			_singleWordAlignmentModel = new ThotSingleWordAlignmentModel(Thot.decoder_getSingleWordAlignmentModel(_handle));
			_inverseSingleWordAlignmentModel = new ThotSingleWordAlignmentModel(Thot.decoder_getInverseSingleWordAlignmentModel(_handle));
		}

		public void Train(IEnumerable<IEnumerable<string>> sourceCorpus, IEnumerable<IEnumerable<string>> targetCorpus, IProgress progress = null)
		{
			lock (_sessions)
			{
				if (_sessions.Count > 0)
					throw new InvalidOperationException("The engine cannot be trained while there are active sessions open.");

				Thot.decoder_close(_handle);
				TrainModels(_cfgFileName, sourceCorpus, targetCorpus, progress);
				_handle = Thot.decoder_open(_cfgFileName);
				_singleWordAlignmentModel.Handle = Thot.decoder_getSingleWordAlignmentModel(_handle);
				_inverseSingleWordAlignmentModel.Handle = Thot.decoder_getInverseSingleWordAlignmentModel(_handle);
			}
		}

		public TranslationResult Translate(IEnumerable<string> segment)
		{
			lock (_sessions)
			{
				if (_globalSession == null)
				{
					_globalSession = new ThotSmtSession(this);
					_sessions.Add(_globalSession);
				}
			}
			return _globalSession.Translate(segment);
		}

		public IInteractiveSmtSession StartSession()
		{
			CheckDisposed();
			lock (_sessions)
			{
				var session = new ThotSmtSession(this);
				_sessions.Add(session);
				return session;
			}
		}

		public void Save()
		{
			Thot.decoder_saveModels(_handle);
		}

		public ISegmentAligner SingleWordAlignmentModel
		{
			get { return _singleWordAlignmentModel; }
		}

		public ISegmentAligner InverseSingleWordAlignmentModel
		{
			get { return _inverseSingleWordAlignmentModel; }
		}

		internal IntPtr Handle
		{
			get { return _handle; }
		}

		internal void RemoveSession(ThotSmtSession session)
		{
			lock (_sessions)
				_sessions.Remove(session);
		}

		protected override void DisposeManagedResources()
		{
			lock (_sessions)
			{
				foreach (ThotSmtSession session in _sessions.ToArray())
					session.Dispose();
			}
		}

		protected override void DisposeUnmanagedResources()
		{
			Thot.decoder_close(_handle);
		}

		IInteractiveTranslationSession IInteractiveTranslationEngine.StartSession()
		{
			return StartSession();
		}
	}
}
