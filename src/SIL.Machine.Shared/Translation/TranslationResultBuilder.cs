using SIL.Machine.Annotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.Machine.Translation
{
	public class TranslationResultBuilder
	{
		private readonly List<string> _words;
		private readonly List<double> _confidences;
		private readonly HashSet<int> _unknownWords;
		private readonly HashSet<int> _uncorrectedPrefixWords;
		private readonly List<PhraseInfo> _phrases;

		public TranslationResultBuilder()
		{
			_words = new List<string>();
			_confidences = new List<double>();
			_unknownWords = new HashSet<int>();
			_uncorrectedPrefixWords = new HashSet<int>();
			_phrases = new List<PhraseInfo>();
		}

		public IReadOnlyList<string> Words => _words;
		public IReadOnlyList<double> Confidences => _confidences;
		public IReadOnlyList<PhraseInfo> Phrases => _phrases;

		public void AppendWord(string word, double confidence = -1, bool isUnknown = false)
		{
			_words.Add(word);
			_confidences.Add(confidence);
			if (isUnknown)
				_unknownWords.Add(_words.Count - 1);
		}

		public void MarkPhrase(Range<int> sourceSegmentRange, WordAlignmentMatrix alignment)
		{
			_phrases.Add(new PhraseInfo(sourceSegmentRange, _words.Count, alignment));
		}

		public void SetConfidence(int index, double confidence)
		{
			_confidences[index] = confidence;
		}

		public int CorrectPrefix(IEnumerable<EditOperation> wordOps, IEnumerable<EditOperation> charOps,
			string[] prefix, bool isLastWordComplete)
		{
			var alignmentColsToCopy = new List<int>();

			int i = 0, j = 0, k = 0;
			foreach (EditOperation wordOp in wordOps)
			{
				switch (wordOp)
				{
					case EditOperation.Insert:
						_words.Insert(j, prefix[j]);
						_confidences.Insert(j, -1);
						alignmentColsToCopy.Add(-1);
						for (int l = k; l < _phrases.Count; l++)
							_phrases[l].TargetCut++;
						j++;
						break;

					case EditOperation.Delete:
						_words.RemoveAt(j);
						_confidences.RemoveAt(j);
						i++;
						if (k < _phrases.Count)
						{
							for (int l = k; l < _phrases.Count; l++)
								_phrases[l].TargetCut--;

							if (_phrases[k].TargetCut <= 0
								|| (k > 0 && _phrases[k].TargetCut == _phrases[k - 1].TargetCut))
							{
								_phrases.RemoveAt(k);
								alignmentColsToCopy.Clear();
								i = 0;
							}
							else if (j >= _phrases[k].TargetCut)
							{
								ResizeAlignment(k, alignmentColsToCopy);
								alignmentColsToCopy.Clear();
								i = 0;
								k++;
							}
						}
						break;

					case EditOperation.Hit:
					case EditOperation.Substitute:
						if (wordOp == EditOperation.Substitute || j < prefix.Length - 1 || isLastWordComplete)
							_words[j] = prefix[j];
						else
							_words[j] = CorrectWord(charOps, _words[j], prefix[j]);

						if (wordOp == EditOperation.Substitute)
							_confidences[j] = -1;
						else if (wordOp == EditOperation.Hit)
							_uncorrectedPrefixWords.Add(j);

						alignmentColsToCopy.Add(i);

						i++;
						j++;
						if (k < _phrases.Count && j >= _phrases[k].TargetCut)
						{
							ResizeAlignment(k, alignmentColsToCopy);
							alignmentColsToCopy.Clear();
							i = 0;
							k++;
						}
						break;
				}
			}

			while (j < Words.Count)
			{
				alignmentColsToCopy.Add(i);

				i++;
				j++;
				if (k < _phrases.Count && j >= _phrases[k].TargetCut)
				{
					ResizeAlignment(k, alignmentColsToCopy);
					alignmentColsToCopy.Clear();
					break;
				}
			}

			return alignmentColsToCopy.Count;
		}

		private void ResizeAlignment(int phraseIndex, List<int> colsToCopy)
		{
			WordAlignmentMatrix curAlignment = _phrases[phraseIndex].Alignment;
			if (colsToCopy.Count == curAlignment.ColumnCount)
				return;

			var newAlignment = new WordAlignmentMatrix(curAlignment.RowCount, colsToCopy.Count);
			for (int j = 0; j < newAlignment.ColumnCount; j++)
			{
				if (colsToCopy[j] != -1)
				{
					for (int i = 0; i < newAlignment.RowCount; i++)
						newAlignment[i, j] = curAlignment[i, colsToCopy[j]];
				}
			}

			_phrases[phraseIndex].Alignment = newAlignment;
		}

		private string CorrectWord(IEnumerable<EditOperation> charOps, string word, string prefix)
		{
			var sb = new StringBuilder();
			int i = 0, j = 0;
			foreach (EditOperation charOp in charOps)
			{
				switch (charOp)
				{
					case EditOperation.Hit:
						sb.Append(word[i]);
						i++;
						j++;
						break;

					case EditOperation.Insert:
						sb.Append(prefix[j]);
						j++;
						break;

					case EditOperation.Delete:
						i++;
						break;

					case EditOperation.Substitute:
						sb.Append(prefix[j]);
						i++;
						j++;
						break;
				}
			}

			sb.Append(word.Substring(i));
			return sb.ToString();
		}

		public TranslationResult ToResult(IReadOnlyList<string> sourceSegment, int prefixCount = 0)
		{
			double[] confidences = _confidences.ToArray();
			var sources = new TranslationSources[Words.Count];
			var alignment = new WordAlignmentMatrix(sourceSegment.Count, Words.Count);
			var phrases = new List<Phrase>();
			int trgPhraseStartIndex = 0;
			foreach (PhraseInfo phraseInfo in _phrases)
			{
				double confidence = double.MaxValue;
				for (int j = trgPhraseStartIndex; j < phraseInfo.TargetCut; j++)
				{
					for (int i = phraseInfo.SourceSegmentRange.Start; i < phraseInfo.SourceSegmentRange.End; i++)
					{
						bool aligned = phraseInfo.Alignment[i - phraseInfo.SourceSegmentRange.Start,
							j - trgPhraseStartIndex];
						if (aligned)
							alignment[i, j] = true;
					}

					if (j < prefixCount)
					{
						sources[j] = TranslationSources.Prefix;
						if (_uncorrectedPrefixWords.Contains(j))
							sources[j] |= TranslationSources.Smt;
					}
					else if (_unknownWords.Contains(j))
					{
						sources[j] = TranslationSources.None;
					}
					else
					{
						sources[j] = TranslationSources.Smt;
					}

					confidence = Math.Min(confidence, Confidences[j]);
				}

				phrases.Add(new Phrase(phraseInfo.SourceSegmentRange, phraseInfo.TargetCut, confidence));
				trgPhraseStartIndex = phraseInfo.TargetCut;
			}

			return new TranslationResult(sourceSegment, Words, confidences, sources, alignment, phrases);
		}

		public class PhraseInfo
		{
			public PhraseInfo(Range<int> sourceSegmentRange, int targetCut, WordAlignmentMatrix alignment)
			{
				SourceSegmentRange = sourceSegmentRange;
				TargetCut = targetCut;
				Alignment = alignment;
			}

			public Range<int> SourceSegmentRange { get; }
			public int TargetCut { get; internal set; }
			public WordAlignmentMatrix Alignment { get; internal set; }
		}
	}
}
