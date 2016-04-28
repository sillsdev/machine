using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.Extensions;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class TranslationSession : DisposableBase
	{
		private const float NullWordConfidenceThreshold = 0.03f;

		private readonly TranslationEngine _engine;
		private readonly ISmtSession _smtSession;
		private readonly TransferEngine _transferEngine;
		private readonly Dictionary<string, string> _transferCache;
		private readonly List<string> _sourceSegment; 
		private readonly ReadOnlyList<string> _readOnlySourceSegment;
		private readonly List<string> _translation;
		private readonly ReadOnlyList<string> _readOnlyTranslation;
		private readonly List<Tuple<List<AlignedWordInfo>, double>> _targetWordInfos;
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
			_targetWordInfos = new List<Tuple<List<AlignedWordInfo>, double>>();
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

		public IEnumerable<AlignedWordInfo> GetAlignedSourceWords(int index)
		{
			return _targetWordInfos[index].Item1;
		}

		public double GetWordConfidence(int index)
		{
			return _targetWordInfos[index].Item2;
		}

		public IEnumerable<string> Translate(IEnumerable<string> sourceSegment)
		{
			var translation = new List<string>();
			var wordInfos = new List<Tuple<List<AlignedWordInfo>, double>>();
			ProcessResult(_smtSession.Translate(sourceSegment), translation, wordInfos);
			return translation;
		}

		public void TranslateInteractively(IEnumerable<string> sourceSegment)
		{
			Reset();
			_sourceSegment.AddRange(sourceSegment);
			ProcessResult(_smtSession.TranslateInteractively(_sourceSegment), _translation, _targetWordInfos);
		}

		public void Reset()
		{
			_sourceSegment.Clear();
			_translation.Clear();
			_targetWordInfos.Clear();
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
				ProcessResult(_smtSession.SetPrefix(_prefix, _isLastWordPartial), _translation, _targetWordInfos);
			}
		}

		public void AddToPrefix(string addition, bool isWordPartial)
		{
			_prefix.Add(addition);
			_isLastWordPartial = isWordPartial;
			ProcessResult(_smtSession.AddToPrefix(addition.ToEnumerable(), _isLastWordPartial), _translation, _targetWordInfos);
		}

		public void AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial)
		{
			string[] additionArray = addition.ToArray();
			_prefix.AddRange(additionArray);
			_isLastWordPartial = isLastWordPartial;
			ProcessResult(_smtSession.AddToPrefix(additionArray, _isLastWordPartial), _translation, _targetWordInfos);
		}

		public void Approve()
		{
			_smtSession.Train(_readOnlySourceSegment, _prefix);
		}

		private void ProcessResult(IEnumerable<string> result, List<string> translation, List<Tuple<List<AlignedWordInfo>, double>> targetWordInfos)
		{
			translation.Clear();
			targetWordInfos.Clear();
			List<string> targetSegment = result.ToList();
			WordAlignmentMatrix waMatrix;
			_segmentAligner.GetBestAlignment(_readOnlySourceSegment, targetSegment, out waMatrix);
			for (int j = 0; j < targetSegment.Count; j++)
			{
				int[] sourceIndices = Enumerable.Range(0, waMatrix.I).Where(i => waMatrix[i, j]).ToArray();
				string targetWord = null;
				var alignedSourceWords = new List<AlignedWordInfo>();
				double bestConfidence = 0;
				if (sourceIndices.Length == 0)
				{
					targetWord = targetSegment[j];
					bestConfidence = _segmentAligner.GetTranslationProbability(null, targetWord);
				}
				else
				{
					bool transferred = false;
					foreach (int sourceIndex in sourceIndices)
					{
						AlignedWordType type = AlignedWordType.Normal;
						string sourceWord = _readOnlySourceSegment[sourceIndex];
						double confidence = _segmentAligner.GetTranslationProbability(sourceWord, targetSegment[j]);
						if (confidence < NullWordConfidenceThreshold && sourceWord == targetSegment[j])
						{
							if (!transferred && TryTransferWord(sourceWord, out targetWord))
							{
								confidence = _segmentAligner.GetTranslationProbability(sourceWord, targetWord);
								transferred = true;
								type = AlignedWordType.Transferred;
								if (_translation.Count == 1 && targetWord.StartsWith(_translation[0]))
								{
									_translation.Clear();
									_targetWordInfos.Clear();
								}
							}
							else
							{
								targetWord = targetSegment[j];
								type = AlignedWordType.NotTranslated;
							}
						}
						else
						{
							targetWord = targetSegment[j];
							string word;
							if (!transferred && j < _prefix.Count && TryTransferWord(sourceWord, out word) && word == targetWord)
							{
								transferred = true;
								type = AlignedWordType.Transferred;
							}
						}
						if (confidence > bestConfidence)
							bestConfidence = confidence;
						alignedSourceWords.Add(new AlignedWordInfo(sourceIndex, confidence, type));
					}
				}
				Debug.Assert(targetWord != null);
				translation.Add(targetWord);
				targetWordInfos.Add(Tuple.Create(alignedSourceWords, bestConfidence));
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
	}
}
