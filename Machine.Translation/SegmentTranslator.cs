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
		private readonly ReadOnlyList<string> _sourceSegment;
		private readonly List<string> _translation;
		private readonly ReadOnlyList<string> _readOnlyTranslation;
		private readonly List<int> _sourceWordIndices;
		private readonly ReadOnlyList<int> _readOnlySourceWordIndices; 
		private readonly List<float> _wordConfidences;
		private readonly ReadOnlyList<float> _readOnlyWordConfidences; 
		private readonly BulkObservableList<string> _prefix;

		internal SegmentTranslator(SmtSession smtSession, TransferEngine transferEngine, IEnumerable<string> segment)
		{
			_smtSession = smtSession;
			_transferEngine = transferEngine;
			_sourceSegment = new ReadOnlyList<string>(segment.ToArray());
			_prefix = new BulkObservableList<string>();
			_prefix.CollectionChanged += PrefixChanged;
			_translation = new List<string>();
			_readOnlyTranslation = new ReadOnlyList<string>(_translation);
			_sourceWordIndices = new List<int>();
			_readOnlySourceWordIndices = new ReadOnlyList<int>(_sourceWordIndices);
			_wordConfidences = new List<float>();
			_readOnlyWordConfidences = new ReadOnlyList<float>(_wordConfidences);
			ProcessResult(_smtSession.TranslateInteractively(_sourceSegment));
		}

		private void PrefixChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			ProcessResult(_smtSession.SetPrefix(_prefix));
		}

		public IReadOnlyList<string> SourceSegment
		{
			get { return _sourceSegment; }
		}

		public BulkObservableList<string> Prefix
		{
			get { return _prefix; }
		}

		public IReadOnlyList<string> Translation
		{
			get { return _readOnlyTranslation; }
		}

		public IReadOnlyList<int> SourceWordIndices
		{
			get { return _readOnlySourceWordIndices; }
		}

		public IReadOnlyList<float> WordConfidences
		{
			get { return _readOnlyWordConfidences; }
		}

		public void Approve()
		{
			_smtSession.Train(_sourceSegment, _prefix);
		}

		private void ProcessResult(SmtResult result)
		{
			_translation.Clear();
			_sourceWordIndices.Clear();
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
				_translation.Add(targetWord);
				_sourceWordIndices.Add(result.SourceWordIndices[i]);
				_wordConfidences.Add(confidence);
			}
		}
	}
}
