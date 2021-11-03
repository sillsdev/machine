using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class ParallelTextCorpus
	{
		private readonly IComparer<object> _segmentRefComparer;

		public ParallelTextCorpus(ITextCorpus sourceCorpus, ITextCorpus targetCorpus,
			ITextAlignmentCorpus textAlignmentCorpus = null, IComparer<object> segmentRefComparer = null)
		{
			SourceCorpus = sourceCorpus;
			TargetCorpus = targetCorpus;
			TextAlignmentCorpus = textAlignmentCorpus ?? new DictionaryTextAlignmentCorpus();
			_segmentRefComparer = segmentRefComparer;
		}

		public ITextCorpus SourceCorpus { get; }

		public ITextCorpus TargetCorpus { get; }

		public ITextAlignmentCorpus TextAlignmentCorpus { get; }

		public IEnumerable<ParallelText> Texts => GetTexts();

		public IEnumerable<ParallelTextSegment> Segments => GetSegments();

		public IEnumerable<TextSegment> SourceSegments => Texts.SelectMany(t => t.SourceText.GetSegments());

		public IEnumerable<TextSegment> TargetSegments => Texts.SelectMany(t => t.TargetText.GetSegments());

		public ParallelTextCorpus Invert()
		{
			return new ParallelTextCorpus(TargetCorpus, SourceCorpus, TextAlignmentCorpus.Invert());
		}

		public IEnumerable<ParallelText> GetTexts(bool allSourceSegments = false, bool allTargetSegments = false)
		{
			IEnumerable<string> sourceTextIds = SourceCorpus.Texts.Select(t => t.Id);
			IEnumerable<string> targetTextIds = TargetCorpus.Texts.Select(t => t.Id);

			IEnumerable<string> textIds;
			if (allSourceSegments && allTargetSegments)
				textIds = sourceTextIds.Union(targetTextIds);
			else if (!allSourceSegments && !allTargetSegments)
				textIds = sourceTextIds.Intersect(targetTextIds);
			else if (allSourceSegments)
				textIds = sourceTextIds;
			else
				textIds = targetTextIds;

			return textIds.Select(id => CreateParallelText(id)).OrderBy(t => t.SortKey);
		}

		public IEnumerable<ParallelTextSegment> GetSegments(bool allSourceSegments = false,
			bool allTargetSegments = false, bool includeText = true)
		{
			return GetTexts(allSourceSegments, allTargetSegments)
				.SelectMany(t => t.GetSegments(allSourceSegments, allTargetSegments, includeText));
		}

		public int GetCount(bool allSourceSegments = false, bool allTargetSegments = false, bool nonemptyOnly = false)
		{
			return GetTexts(allSourceSegments, allTargetSegments)
				.Sum(t => t.GetCount(allSourceSegments, allTargetSegments, nonemptyOnly));
		}

		private ParallelText CreateParallelText(string id)
		{
			IText sourceText = SourceCorpus[id];
			IText targetText = TargetCorpus[id];
			ITextAlignmentCollection textAlignmentCollection = TextAlignmentCorpus[id];
			return new ParallelText(sourceText, targetText, textAlignmentCollection, _segmentRefComparer);
		}
	}
}
