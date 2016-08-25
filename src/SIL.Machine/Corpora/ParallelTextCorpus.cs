using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class ParallelTextCorpus
	{
		public ParallelTextCorpus(ITextCorpus sourceCorpus, ITextCorpus targetCorpus)
		{
			SourceCorpus = sourceCorpus;
			TargetCorpus = targetCorpus;
		}

		public ITextCorpus SourceCorpus { get; }

		public ITextCorpus TargetCorpus { get; }

		public IEnumerable<ParallelText> Texts
		{
			get
			{
				foreach (IText text1 in SourceCorpus.Texts)
				{
					IText text2;
					if (TargetCorpus.TryGetText(text1.Id, out text2))
						yield return new ParallelText(text1, text2);
				}
			}
		}

		public IEnumerable<ParallelTextSegment> Segments
		{
			get { return Texts.SelectMany(t => t.Segments); }
		}

		public IEnumerable<TextSegment> SourceSegments
		{
			get { return Texts.SelectMany(t => t.SourceText.Segments); }
		}

		public IEnumerable<TextSegment> TargetSegments
		{
			get { return Texts.SelectMany(t => t.TargetText.Segments); }
		}

		public ParallelTextCorpus Inverse()
		{
			return new ParallelTextCorpus(TargetCorpus, SourceCorpus);
		}
	}
}
