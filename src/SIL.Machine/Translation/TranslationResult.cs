using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class TranslationResult
	{
		private readonly ReadOnlyList<double> _confidences; 
		private readonly AlignedWordPair[,] _alignment;

		public TranslationResult(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment, IEnumerable<double> confidences, AlignedWordPair[,] alignment)
		{
			SourceSegment = new ReadOnlyList<string>(sourceSegment.ToArray());
			TargetSegment = new ReadOnlyList<string>(targetSegment.ToArray());
			_confidences = new ReadOnlyList<double>(confidences.ToArray());
			_alignment = alignment;
		}

		public IReadOnlyList<string> SourceSegment { get; }

		public IReadOnlyList<string> TargetSegment { get; }

		public double GetTargetWordConfidence(int targetIndex)
		{
			return _confidences[targetIndex];
		}

		public IEnumerable<AlignedWordPair> GetSourceWordPairs(int sourceIndex)
		{
			return Enumerable.Range(0, TargetSegment.Count).Where(j => _alignment[sourceIndex, j] != null).Select(j => _alignment[sourceIndex, j]);
		}

		public IEnumerable<AlignedWordPair> GetTargetWordPairs(int targetIndex)
		{
			return Enumerable.Range(0, SourceSegment.Count).Where(i => _alignment[i, targetIndex] != null).Select(i => _alignment[i, targetIndex]);
		}

		public bool TryGetWordPair(int sourceIndex, int targetIndex, out AlignedWordPair wordPair)
		{
			if (_alignment[sourceIndex, targetIndex] != null)
			{
				wordPair = _alignment[sourceIndex, targetIndex];
				return true;
			}

			wordPair = null;
			return false;
		}
	}
}
