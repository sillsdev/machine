using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Corpora;
using SIL.Machine.Utils;

namespace SIL.Machine.Translation
{
	public class TransferTruecaser : ITruecaser
	{
		public ITrainer CreateTrainer(ITextCorpus corpus)
		{
			return new NullTrainer();
		}

		public void Save()
		{
		}

		public Task SaveAsync()
		{
			return Task.CompletedTask;
		}

		public void TrainSegment(IReadOnlyList<string> segment, bool sentenceStart = true)
		{
		}

		public TranslationResult Truecase(IReadOnlyList<string> sourceSegment, TranslationResult result)
		{
			IReadOnlyList<string> targetSegment = Truecase(sourceSegment, 0, result.TargetSegment, result.Alignment);
			return new TranslationResult(sourceSegment, targetSegment, result.WordConfidences,
				result.WordSources, result.Alignment, result.Phrases);
		}

		public WordGraph Truecase(IReadOnlyList<string> sourceSegment, WordGraph wordGraph)
		{
			var newArcs = new List<WordGraphArc>();
			foreach (WordGraphArc arc in wordGraph.Arcs)
			{
				IReadOnlyList<string> words = Truecase(sourceSegment, arc.SourceSegmentRange.Start, arc.Words,
					arc.Alignment);
				newArcs.Add(new WordGraphArc(arc.PrevState, arc.NextState, arc.Score, words, arc.Alignment,
					arc.SourceSegmentRange, arc.WordSources, arc.WordConfidences));
			}
			return new WordGraph(newArcs, wordGraph.FinalStates, wordGraph.InitialStateScore);
		}

		public IReadOnlyList<string> Truecase(IReadOnlyList<string> sourceSegment, int sourceStartIndex,
			IReadOnlyList<string> targetSegment, WordAlignmentMatrix alignment)
		{
			var newSegment = new string[targetSegment.Count];
			for (int j = 0; j < newSegment.Length; j++)
			{
				newSegment[j] = targetSegment[j];
				if (alignment.GetColumnAlignedIndices(j).Any(i => sourceSegment[sourceStartIndex + i].IsTitleCase()))
					newSegment[j] = newSegment[j].ToTitleCase();
			}
			return newSegment;
		}
	}
}
