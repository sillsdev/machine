using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class SegmentTranslator
	{
		public const float WordConfidenceThreshold = 0.03f;

		private readonly SmtSession _smtSession;
		private readonly TransferEngine _transferEngine;
		private readonly ReadOnlyList<string> _segment;
		private readonly List<string> _currentTranslation;
		private readonly ReadOnlyList<string> _readOnlyCurrentTranslation;
		private readonly List<float> _wordConfidences;
		private readonly ReadOnlyList<float> _readOnlyWordConfidences; 
		private readonly BulkObservableList<string> _prefix;

		internal SegmentTranslator(SmtSession smtSession, TransferEngine transferEngine, IEnumerable<string> segment)
		{
			_smtSession = smtSession;
			_transferEngine = transferEngine;
			_segment = new ReadOnlyList<string>(segment.ToArray());
			_prefix = new BulkObservableList<string>();
			_prefix.CollectionChanged += PrefixChanged;
			_currentTranslation = new List<string>();
			_readOnlyCurrentTranslation = new ReadOnlyList<string>(_currentTranslation);
			_wordConfidences = new List<float>();
			_readOnlyWordConfidences = new ReadOnlyList<float>(_wordConfidences);
			ProcessResult(_smtSession.TranslateInteractively(_segment));
		}

		private void PrefixChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			ProcessResult(_smtSession.SetPrefix(_prefix));
		}

		public IReadOnlyList<string> Segment
		{
			get { return _segment; }
		}

		public BulkObservableList<string> Prefix
		{
			get { return _prefix; }
		}

		public IReadOnlyList<string> CurrentTranslation
		{
			get { return _readOnlyCurrentTranslation; }
		}

		public IReadOnlyList<float> WordConfidences
		{
			get { return _readOnlyWordConfidences; }
		}

		public void Approve()
		{
			_smtSession.Train(_segment, _prefix);
		}

		private void ProcessResult(SmtResult result)
		{
			_currentTranslation.Clear();
			_wordConfidences.Clear();
			for (int i = 0; i < result.Translation.Count; i++)
			{
				float confidence;
				string targetWord;
				if (_transferEngine != null && result.WordConfidences[i] < WordConfidenceThreshold
					&& _transferEngine.TryTranslateWord(result.Translation[i], out targetWord))
				{
					confidence = 1.0f;
				}
				else
				{
					targetWord = result.Translation[i];
					confidence = result.WordConfidences[i];
				}
				_currentTranslation.Add(targetWord);
				_wordConfidences.Add(confidence);
			}
		}
	}
}
