using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	public class ThotWordAlignmentModelTrainer : DisposableBase, ITrainer
	{
		private readonly string _prefFileName;
		private readonly ITokenProcessor _sourcePreprocessor;
		private readonly ITokenProcessor _targetPreprocessor;
		private readonly ParallelTextCorpus _parallelCorpus;
		private readonly string _tempDir;
		private readonly int _maxCorpusCount;
		private readonly string _filePrefix;

		public ThotWordAlignmentModelTrainer(string prefFileName, ITokenProcessor sourcePreprocessor,
			ITokenProcessor targetPreprocessor, ParallelTextCorpus corpus, int maxCorpusCount = int.MaxValue)
		{
			_prefFileName = prefFileName;
			_sourcePreprocessor = sourcePreprocessor;
			_targetPreprocessor = targetPreprocessor;
			_parallelCorpus = corpus;
			_maxCorpusCount = maxCorpusCount;

			do
			{
				_tempDir = Path.Combine(Path.GetTempPath(), "thot-wa-train-" + Guid.NewGuid());
			} while (Directory.Exists(_tempDir));
			Directory.CreateDirectory(_tempDir);
			_filePrefix = Path.GetFileName(_prefFileName);
		}

		public void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null)
		{
			using (var model = new ThotWordAlignmentModel(Path.Combine(_tempDir, _filePrefix), createNew: true))
			{
				model.AddSegmentPairs(_parallelCorpus, _sourcePreprocessor, _targetPreprocessor, _maxCorpusCount);
				model.Train(progress);
				model.Save();
			}
		}

		public virtual void Save()
		{
			string dir = Path.GetDirectoryName(_prefFileName);
			Debug.Assert(dir != null);

			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			CopyFiles(_tempDir, dir, _filePrefix);
		}

		public Task SaveAsync()
		{
			Save();
			return Task.CompletedTask;
		}

		protected override void DisposeManagedResources()
		{
			Directory.Delete(_tempDir, true);
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
	}
}
