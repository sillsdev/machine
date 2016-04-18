using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class SegmentTranslator
	{
		public const float WordConfidenceThreshold = 0.03f;

		private readonly ISmtEngine _smtEngine;
		private readonly ISmtSession _smtSession;
		private readonly TransferEngine _transferEngine;
		private readonly ReadOnlyList<string> _sourceSegment;
		private readonly List<string> _translation;
		private readonly ReadOnlyList<string> _readOnlyTranslation;
		private readonly List<WordInfo> _wordInfos;
		private readonly List<string> _prefix;
		private readonly ReadOnlyList<string> _readOnlyPrefix; 
		private bool _isLastWordPartial;
		private readonly Dictionary<string, string> _transferedWords; 

		internal SegmentTranslator(ISmtEngine smtEngine, ISmtSession smtSession, TransferEngine transferEngine, IEnumerable<string> segment)
		{
			_smtEngine = smtEngine;
			_smtSession = smtSession;
			_transferEngine = transferEngine;
			_sourceSegment = new ReadOnlyList<string>(segment.ToArray());
			_prefix = new List<string>();
			_readOnlyPrefix = new ReadOnlyList<string>(_prefix);
			_translation = new List<string>();
			_readOnlyTranslation = new ReadOnlyList<string>(_translation);
			_wordInfos = new List<WordInfo>();
			_isLastWordPartial = true;
			_transferedWords = new Dictionary<string, string>();
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
				bool exactMatch = false;
				float bestConfidence = 0;
				int bestIndex = 0;
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

					if (confidence > bestConfidence)
					{
						bestConfidence = confidence;
						bestIndex = j;
						exactMatch = translationWords[i] == _sourceSegment[j];
					}
					else if (Math.Abs(confidence - bestConfidence) < float.Epsilon)
					{
						bool indexCloser = Math.Abs(i - j) < Math.Abs(i - bestIndex);
						if (translationWords[i] == _sourceSegment[j])
						{
							if (!exactMatch || indexCloser)
								bestIndex = j;
							exactMatch = true;
						}
						else if (!exactMatch && indexCloser)
						{
							bestIndex = j;
						}
					}
				}

				string sourceWord = _sourceSegment[bestIndex];
				bool transferred = false;
				string targetWord;
				if (_transferEngine != null && bestConfidence < WordConfidenceThreshold && _transferEngine.TryTranslateWord(sourceWord, out targetWord))
				{
					bestConfidence = _smtEngine.GetWordConfidence(sourceWord, targetWord);
					_transferedWords[sourceWord] = targetWord;
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
					if (i < _prefix.Count && _transferedWords.TryGetValue(sourceWord, out word) && word == targetWord)
						transferred = true;
				}
				_translation.Add(targetWord);
				_wordInfos.Add(new WordInfo(bestIndex, bestConfidence, transferred));
			}
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
