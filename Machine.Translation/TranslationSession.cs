using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class TranslationSession : DisposableBase
	{
		public const float NullWordConfidenceThreshold = 0.03f;

		private readonly TranslationEngine _engine;
		private readonly ISmtSession _smtSession;
		private readonly TransferEngine _transferEngine;
		private readonly Dictionary<string, string> _transferCache;
		private readonly List<string> _sourceSegment; 
		private readonly ReadOnlyList<string> _readOnlySourceSegment;
		private readonly List<string> _translation;
		private readonly ReadOnlyList<string> _readOnlyTranslation;
		private readonly List<WordInfo> _wordInfos;
		private readonly List<string> _prefix;
		private readonly ReadOnlyList<string> _readOnlyPrefix; 
		private bool _isLastWordPartial;
		private readonly ISegmentAligner _segmentAligner;

		internal TranslationSession(TranslationEngine engine, ISegmentAligner segmentAligner, ISmtSession smtSession, TransferEngine transferEngine)
		{
			_engine = engine;
			_segmentAligner = segmentAligner;
			_smtSession = smtSession;
			_transferEngine = transferEngine;
			if (_transferEngine != null)
				_transferCache = new Dictionary<string, string>();
			_sourceSegment = new List<string>();
			_readOnlySourceSegment = new ReadOnlyList<string>(_sourceSegment);
			_prefix = new List<string>();
			_readOnlyPrefix = new ReadOnlyList<string>(_prefix);
			_translation = new List<string>();
			_readOnlyTranslation = new ReadOnlyList<string>(_translation);
			_wordInfos = new List<WordInfo>();
			_isLastWordPartial = true;
		}

		public IReadOnlyList<string> SourceSegment
		{
			get { return _readOnlySourceSegment; }
		}

		public IReadOnlyList<string> Prefix
		{
			get { return _readOnlyPrefix; }
		}

		public bool IsLastWordPartial
		{
			get { return _isLastWordPartial; }
		}

		public IReadOnlyList<string> Translation
		{
			get { return _readOnlyTranslation; }
		}

		public int GetSourceWordIndex(int index)
		{
			return _wordInfos[index].SourceWordIndex;
		}

		public double GetWordConfidence(int index)
		{
			return _wordInfos[index].Confidence;
		}

		public bool IsWordTransferred(int index)
		{
			return _wordInfos[index].IsTransferred;
		}

		public IEnumerable<string> Translate(IEnumerable<string> sourceSegment)
		{
			var translation = new List<string>();
			var wordInfos = new List<WordInfo>();
			ProcessResult(_smtSession.Translate(sourceSegment), translation, wordInfos);
			return translation;
		}

		public void TranslateInteractively(IEnumerable<string> sourceSegment)
		{
			Reset();
			_sourceSegment.AddRange(sourceSegment);
			ProcessResult(_smtSession.TranslateInteractively(_sourceSegment), _translation, _wordInfos);
		}

		public void Reset()
		{
			_sourceSegment.Clear();
			_translation.Clear();
			_wordInfos.Clear();
			_prefix.Clear();
			_isLastWordPartial = true;
		}

		public void SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial)
		{
			string[] prefixArray = prefix.ToArray();
			if (!_prefix.SequenceEqual(prefixArray) || _isLastWordPartial != isLastWordPartial)
			{
				_prefix.Clear();
				_prefix.AddRange(prefixArray);
				_isLastWordPartial = isLastWordPartial;
				ProcessResult(_smtSession.SetPrefix(_prefix, _isLastWordPartial), _translation, _wordInfos);
			}
		}

		public void AddToPrefix(string addition, bool isWordPartial)
		{
			_prefix.Add(addition);
			_isLastWordPartial = isWordPartial;
			ProcessResult(_smtSession.AddToPrefix(addition.ToEnumerable(), _isLastWordPartial), _translation, _wordInfos);
		}

		public void AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial)
		{
			string[] additionArray = addition.ToArray();
			_prefix.AddRange(additionArray);
			_isLastWordPartial = isLastWordPartial;
			ProcessResult(_smtSession.AddToPrefix(additionArray, _isLastWordPartial), _translation, _wordInfos);
		}

		public void Approve()
		{
			_smtSession.Train(_readOnlySourceSegment, _prefix);
		}

		private void ProcessResult(IEnumerable<string> result, List<string> translation, List<WordInfo> wordInfos)
		{
			translation.Clear();
			wordInfos.Clear();
			List<string> targetSegment = result.ToList();
			WordAlignmentMatrix waMatrix;
			_segmentAligner.GetBestAlignment(_readOnlySourceSegment, targetSegment, out waMatrix);
			for (int j = 0; j < targetSegment.Count; j++)
			{
				int sourceIndex = Enumerable.Range(0, waMatrix.I).First(i => waMatrix[i, j]);

				double confidence = _segmentAligner.GetTranslationProbability(_readOnlySourceSegment[sourceIndex], targetSegment[j]);

				string sourceWord = _readOnlySourceSegment[sourceIndex];
				bool transferred = false;
				string targetWord;
				if (confidence < NullWordConfidenceThreshold && sourceWord == targetSegment[j] && TryTransferWord(sourceWord, out targetWord))
				{
					confidence = _segmentAligner.GetTranslationProbability(sourceWord, targetWord);
					transferred = true;
					if (_translation.Count == 1 && targetWord.StartsWith(_translation[0]))
					{
						_translation.Clear();
						_wordInfos.Clear();
					}
				}
				else
				{
					targetWord = targetSegment[j];
					string word;
					if (j < _prefix.Count && TryTransferWord(sourceWord, out word) && word == targetWord)
						transferred = true;
				}
				translation.Add(targetWord);
				wordInfos.Add(new WordInfo(sourceIndex, confidence, transferred));
			}
		}

		private bool TryTransferWord(string sourceWord, out string targetWord)
		{
			if (_transferEngine != null)
			{
				if (_transferCache.TryGetValue(sourceWord, out targetWord))
					return true;

				if (_transferEngine.TryTranslateWord(sourceWord, out targetWord))
				{
					_transferCache[sourceWord] = targetWord;
					return true;
				}
			}

			targetWord = null;
			return false;
		}

		protected override void DisposeManagedResources()
		{
			_smtSession.Dispose();
			_engine.RemoveSession(this);
		}

		private class WordInfo
		{
			private readonly int _sourceWordIndex;
			private readonly double _confidence;
			private readonly bool _transferred;

			public WordInfo(int sourceWordIndex, double confidence, bool transfered)
			{
				_sourceWordIndex = sourceWordIndex;
				_confidence = confidence;
				_transferred = transfered;
			}

			public int SourceWordIndex
			{
				get { return _sourceWordIndex; }
			}

			public double Confidence
			{
				get { return _confidence; }
			}

			public bool IsTransferred
			{
				get { return _transferred; }
			}
		}
	}
}
