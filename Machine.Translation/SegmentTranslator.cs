using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class SegmentTranslator
	{
		public const float NullWordConfidenceThreshold = 0.03f;
		private const float Alpha = 0.75f;

		private readonly ISmtEngine _smtEngine;
		private readonly ISmtSession _smtSession;
		private readonly TransferEngine _transferEngine;
		private readonly Dictionary<string, string> _transferCache; 
		private readonly ReadOnlyList<string> _sourceSegment;
		private readonly List<string> _translation;
		private readonly ReadOnlyList<string> _readOnlyTranslation;
		private readonly List<WordInfo> _wordInfos;
		private readonly List<string> _prefix;
		private readonly ReadOnlyList<string> _readOnlyPrefix; 
		private bool _isLastWordPartial;

		internal SegmentTranslator(ISmtEngine smtEngine, ISmtSession smtSession, TransferEngine transferEngine, Dictionary<string, string> transferCache,
			IEnumerable<string> segment)
		{
			_smtEngine = smtEngine;
			_smtSession = smtSession;
			_transferEngine = transferEngine;
			_transferCache = transferCache;
			_sourceSegment = new ReadOnlyList<string>(segment.ToArray());
			_prefix = new List<string>();
			_readOnlyPrefix = new ReadOnlyList<string>(_prefix);
			_translation = new List<string>();
			_readOnlyTranslation = new ReadOnlyList<string>(_translation);
			_wordInfos = new List<WordInfo>();
			_isLastWordPartial = true;
			ProcessResult(_smtSession.TranslateInteractively(_sourceSegment));
		}

		public IReadOnlyList<string> SourceSegment
		{
			get { return _sourceSegment; }
		}

		public IReadOnlyList<string> Prefix
		{
			get { return _readOnlyPrefix; }
		}

		public void SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial)
		{
			string[] prefixArray = prefix.ToArray();
			if (!_prefix.SequenceEqual(prefixArray) || _isLastWordPartial != isLastWordPartial)
			{
				_prefix.Clear();
				_prefix.AddRange(prefixArray);
				_isLastWordPartial = isLastWordPartial;
				ProcessResult(_smtSession.SetPrefix(_prefix, _isLastWordPartial));
			}
		}

		public void AddToPrefix(string addition, bool isWordPartial)
		{
			_prefix.Add(addition);
			_isLastWordPartial = isWordPartial;
			ProcessResult(_smtSession.AddToPrefix(addition.ToEnumerable(), _isLastWordPartial));
		}

		public void AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial)
		{
			string[] additionArray = addition.ToArray();
			_prefix.AddRange(additionArray);
			_isLastWordPartial = isLastWordPartial;
			ProcessResult(_smtSession.AddToPrefix(additionArray, _isLastWordPartial));
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

		public float GetWordConfidence(int index)
		{
			return _wordInfos[index].Confidence;
		}

		public bool IsWordTransferred(int index)
		{
			return _wordInfos[index].IsTransferred;
		}

		public void Approve()
		{
			_smtSession.Train(_sourceSegment, _prefix);
			for (int i = 0; i < _prefix.Count; i++)
			{
				if (_wordInfos[i].IsTransferred)
					_smtSession.Train(new[] {_sourceSegment[_wordInfos[i].SourceWordIndex], "."}, new[] {_prefix[i], "."});
			}
		}

		private void ProcessResult(IEnumerable<string> translation)
		{
			_translation.Clear();
			_wordInfos.Clear();
			List<string> translationWords = translation.ToList();
			for (int i = 0; i < translationWords.Count; i++)
			{
				float bestConfidence = 0;
				int bestIndex = 0;
				float bestAlignmentScore = 0;
				for (int j = 0; j < _sourceSegment.Count; j++)
				{
					if (IsPunctuation(translationWords[i]) != IsPunctuation(_sourceSegment[j]))
						continue;

					float confidence;
					if (IsNumber(translationWords[i]) && translationWords[i] == _sourceSegment[j])
					{
						confidence = 1;
					}
					else
					{
						confidence = _smtEngine.GetWordConfidence(_sourceSegment[j], translationWords[i]);
					}

					float distance = (float) Math.Abs(i - j) / (Math.Max(translationWords.Count, _sourceSegment.Count) - 1);
					float alignmentScore = ((translationWords[i] == _sourceSegment[j] ? 1.0f : confidence) * Alpha) + ((1.0f - distance) * (1.0f - Alpha));

					if (alignmentScore > bestAlignmentScore)
					{
						bestConfidence = confidence;
						bestIndex = j;
						bestAlignmentScore = alignmentScore;
					}
				}

				string sourceWord = _sourceSegment[bestIndex];
				bool transferred = false;
				string targetWord;
				if (bestConfidence < NullWordConfidenceThreshold && TryTransferWord(sourceWord, out targetWord))
				{
					bestConfidence = _smtEngine.GetWordConfidence(sourceWord, targetWord);
					transferred = true;
					if (_translation.Count == 1 && targetWord.StartsWith(_translation[0]))
					{
						_translation.Clear();
						_wordInfos.Clear();
					}
				}
				else
				{
					targetWord = translationWords[i];
					string word;
					if (i < _prefix.Count && TryTransferWord(sourceWord, out word) && word == targetWord)
						transferred = true;
				}
				_translation.Add(targetWord);
				_wordInfos.Add(new WordInfo(bestIndex, bestConfidence, transferred));
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

		private static bool IsPunctuation(string word)
		{
			return word.All(char.IsPunctuation);
		}

		private static bool IsNumber(string word)
		{
			return word.All(char.IsNumber);
		}

		private class WordInfo
		{
			private readonly int _sourceWordIndex;
			private readonly float _confidence;
			private readonly bool _transferred;

			public WordInfo(int sourceWordIndex, float confidence, bool transfered)
			{
				_sourceWordIndex = sourceWordIndex;
				_confidence = confidence;
				_transferred = transfered;
			}

			public int SourceWordIndex
			{
				get { return _sourceWordIndex; }
			}

			public float Confidence
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
