using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.NgramModeling;
using SIL.ObjectModel;
using SIL.Progress;

namespace SIL.Machine.Translation
{
	public class ThotSmtEngine : DisposableBase, ISmtEngine
	{
		private const int TrainingStepCount = 17;

		public static void TrainModels(string cfgFileName, IEnumerable<IEnumerable<string>> sourceCorpus, IEnumerable<IEnumerable<string>> targetCorpus, IProgress progress = null)
		{
			if (progress == null)
				progress = new NullProgress();

			string cfgDir = Path.GetDirectoryName(cfgFileName);
			Debug.Assert(cfgDir != null);
			string tmPrefix = null, lmPrefix = null;
			foreach (string line in File.ReadAllLines(cfgFileName))
			{
				string l = line.Trim();
				if (l.StartsWith("-tm"))
				{
					tmPrefix = l.Substring(4).Trim();
					if (!Path.IsPathRooted(tmPrefix))
						tmPrefix = Path.Combine(cfgDir, tmPrefix);
				}
				else if (l.StartsWith("-lm"))
				{
					lmPrefix = l.Substring(4).Trim();
					if (!Path.IsPathRooted(lmPrefix))
						lmPrefix = Path.Combine(cfgDir, lmPrefix);
				}
			}

			if (string.IsNullOrEmpty(lmPrefix))
				throw new InvalidOperationException("The configuration file does not specify the language model parameter.");
			if (string.IsNullOrEmpty(tmPrefix))
				throw new InvalidOperationException("The configuration file does not specify the translation model parameter.");

			string lmDir = Path.GetDirectoryName(lmPrefix);
			Debug.Assert(lmDir != null);
			string tempLMDir = Path.Combine(lmDir, "temp");
			string tempLMPrefix = Path.Combine(tempLMDir, Path.GetFileName(lmPrefix));
			string tmDir = Path.GetDirectoryName(tmPrefix);
			Debug.Assert(tmDir != null);
			string tempTMDir = Path.Combine(tmDir, "temp");
			string tempTMPrefix = Path.Combine(tempTMDir, Path.GetFileName(tmPrefix));

			try
			{
				progress.WriteMessage("Training target language model...");
				TrainLanguageModel(targetCorpus, tempLMPrefix, 3);
				if (progress.CancelRequested)
					return;
				progress.ProgressIndicator.PercentCompleted += 100 / TrainingStepCount;
				TrainTranslationModel(sourceCorpus, targetCorpus, tempTMPrefix, progress);
				if (progress.CancelRequested)
					return;

				CopyFiles(tempLMDir, lmDir);
				CopyFiles(tempTMDir, tmDir);
				progress.ProgressIndicator.PercentCompleted += 100 / TrainingStepCount;
			}
			finally
			{
				Directory.Delete(tempLMDir, true);
				Directory.Delete(tempTMDir, true);
			}
		}

		private static void CopyFiles(string srcDir, string destDir)
		{
			foreach (string srcFile in Directory.EnumerateFiles(srcDir))
			{
				string fileName = Path.GetFileName(srcFile);
				Debug.Assert(fileName != null);
				File.Copy(srcFile, Path.Combine(destDir, fileName), true);
			}
		}

		private static void TrainLanguageModel(IEnumerable<IEnumerable<string>> targetCorpus, string lmPrefix, int ngramSize)
		{
			string dir = Path.GetDirectoryName(lmPrefix);
			Debug.Assert(dir != null);
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			WriteNgramCountsFile(targetCorpus, lmPrefix, ngramSize);
			WriteLanguageModelWeightsFile(lmPrefix, ngramSize);
			WriteWordPredictionFile(targetCorpus, lmPrefix);
		}

		private static void WriteNgramCountsFile(IEnumerable<IEnumerable<string>> targetCorpus, string lmPrefix, int ngramSize)
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

		private static void WriteLanguageModelWeightsFile(string lmPrefix, int ngramSize)
		{
			File.WriteAllText(lmPrefix + ".weights", string.Format("{0} 3 10 {1}\n", ngramSize, string.Join(" ", Enumerable.Repeat("0.5", ngramSize * 3))));
		}

		private static void WriteWordPredictionFile(IEnumerable<IEnumerable<string>> targetCorpus, string lmPrefix)
		{
			var rand = new Random(31415);
			using (var writer = new StreamWriter(lmPrefix + ".wp"))
			{
				foreach (IEnumerable<string> segment in targetCorpus.Where(s => s.Any()).Take(100000).OrderBy(i => rand.Next()))
					writer.Write("{0}\n", string.Join(" ", segment));
			}
		}

		private static void TrainTranslationModel(IEnumerable<IEnumerable<string>> sourceCorpus, IEnumerable<IEnumerable<string>> targetCorpus, string tmPrefix, IProgress progress)
		{
			string dir = Path.GetDirectoryName(tmPrefix);
			Debug.Assert(dir != null);
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			string swmPrefix = tmPrefix + "_swm";
			GenerateSingleWordAlignmentModel(swmPrefix, targetCorpus, sourceCorpus, progress, "source-to-target");

			string invswmPrefix = tmPrefix + "_invswm";
			GenerateSingleWordAlignmentModel(invswmPrefix, sourceCorpus, targetCorpus, progress, "target-to-source");

			progress.WriteMessage("Merging alignments...");
			Thot.giza_symmetr1(swmPrefix + ".bestal", invswmPrefix + ".bestal", tmPrefix + ".A3.final", true);
			progress.ProgressIndicator.PercentCompleted += 100 / TrainingStepCount;
			if (progress.CancelRequested)
				return;

			progress.WriteMessage("Generating phrase table...");
			Thot.phraseModel_generate(tmPrefix + ".A3.final", 7, tmPrefix + ".ttable");
			progress.ProgressIndicator.PercentCompleted += 100 / TrainingStepCount;
			if (progress.CancelRequested)
				return;

			progress.WriteMessage("Filtering phrase table...");
			FilterPhraseTable(tmPrefix + ".ttable", 20);
			progress.ProgressIndicator.PercentCompleted += 100 / TrainingStepCount;
			if (progress.CancelRequested)
				return;

			File.WriteAllText(tmPrefix + ".lambda", "0.01");
			File.WriteAllText(tmPrefix + ".srcsegmlentable", "Uniform");
			File.WriteAllText(tmPrefix + ".trgsegmlentable", "Geometric");
		}

		private static void GenerateSingleWordAlignmentModel(string swmPrefix, IEnumerable<IEnumerable<string>> sourceCorpus,
			IEnumerable<IEnumerable<string>> targetCorpus, IProgress progress, string name)
		{
			using (var swAlignModel = new ThotSingleWordAlignmentModel(swmPrefix, true))
			{
				progress.WriteMessage("Training {0} single word model...", name);
				TrainSingleWordAlignmentModel(swAlignModel, sourceCorpus, targetCorpus, progress);
				if (progress.CancelRequested)
					return;

				PruneLexTable(swmPrefix + ".hmm_lexnd", 0.00001);

				progress.WriteMessage("Generating best {0} alignments...", name);
				GenerateBestAlignments(swAlignModel, swmPrefix + ".bestal", sourceCorpus, targetCorpus, progress);
				progress.ProgressIndicator.PercentCompleted += 100 / TrainingStepCount;
			}
		}

		private static void PruneLexTable(string fileName, double threshold)
		{
			var entries = new List<Tuple<uint, uint, float, float>>();
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
					float denom = reader.ReadSingle();
					pos += sizeof(float);

					entries.Add(Tuple.Create(srcIndex, trgIndex, numer, denom));
				}
			}

			using (var writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
			{
				foreach (IGrouping<uint, Tuple<uint, uint, float, float>> g in entries.GroupBy(e => e.Item1).OrderBy(g => g.Key))
				{
					Tuple<uint, uint, float, float>[] groupEntries = g.OrderByDescending(e => e.Item3).ToArray();

					float lcSrc = groupEntries.Select(e => e.Item3).Skip(1).Aggregate(groupEntries[0].Item3, SumLog);

					float newLcSrc = -99;
					int count = 0;
					foreach (Tuple<uint, uint, float, float> entry in groupEntries)
					{
						float prob = (float) Math.Exp(entry.Item3 - lcSrc);
						if (prob < threshold)
							break;
						newLcSrc = SumLog(newLcSrc, entry.Item3);
						count++;
					}

					for (int i = 0; i < count; i++)
					{
						writer.Write(groupEntries[i].Item1);
						writer.Write(groupEntries[i].Item2);
						writer.Write(groupEntries[i].Item3);
						writer.Write(newLcSrc);
					}
				}
			}
		}

		private static float SumLog(float logx, float logy)
		{
			if (logx > logy)
				return (float) (logx + Math.Log(1 + Math.Exp(logy - logx)));
			return (float) (logy + Math.Log(1 + Math.Exp(logx - logy)));
		}

		private static void TrainSingleWordAlignmentModel(ThotSingleWordAlignmentModel swAlignModel, IEnumerable<IEnumerable<string>> sourceCorpus,
			IEnumerable<IEnumerable<string>> targetCorpus, IProgress progress)
		{
			foreach (Tuple<string[], string[]> pair in sourceCorpus.Select(s => s.ToArray()).Zip(targetCorpus.Select(s => s.ToArray()))
				.Where(p => p.Item1.Length > 0 && p.Item2.Length > 0))
			{
				swAlignModel.AddSegmentPair(pair.Item1, pair.Item2);
			}
			for (int i = 0; i < 5; i++)
			{
				swAlignModel.Train(1);
				progress.ProgressIndicator.PercentCompleted += 100 / TrainingStepCount;
				if (progress.CancelRequested)
					return;
			}
			swAlignModel.Save();
		}

		private static void GenerateBestAlignments(ThotSingleWordAlignmentModel swAlignModel, string fileName, IEnumerable<IEnumerable<string>> sourceCorpus,
			IEnumerable<IEnumerable<string>> targetCorpus, IProgress progress)
		{
			using (var writer = new StreamWriter(fileName))
			{
				foreach (Tuple<string[], string[]> pair in sourceCorpus.Select(s => s.ToArray()).Zip(targetCorpus.Select(s => s.ToArray()))
					.Where(p => p.Item1.Length > 0 && p.Item2.Length > 0))
				{
					WordAlignmentMatrix waMatrix;
					double prob = swAlignModel.GetBestAlignment(pair.Item1, pair.Item2, out waMatrix);
					writer.Write("# Alignment probability= {0}\n", prob);
					writer.Write(waMatrix.ToGizaFormat(pair.Item1, pair.Item2));
					if (progress.CancelRequested)
						return;
				}
			}
		}

		private static void FilterPhraseTable(string fileName, int n)
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
							writer.Write("{0} ||| {1} ||| {2:0.00000000} {3:0.00000000}\n", entry.Item1, entry.Item2, entry.Item3, entry.Item4);
						else
							remainder += entry.Item4;
					}

					if (remainder > 0)
					{
						writer.Write("<UNUSED_WORD> ||| {0} ||| 0 {1:0.00000000}\n", g.Key, remainder);
					}
				}
			}
		}

		private readonly string _cfgFileName;
		private IntPtr _handle;
		private readonly HashSet<ThotSmtSession> _sessions;
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

		public ISmtSession StartSession()
		{
			CheckDisposed();

			var session = new ThotSmtSession(this);
			lock (_sessions)
				_sessions.Add(session);
			return session;
		}

		public void SaveModels()
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
	}
}
