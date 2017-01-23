using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class ParallelTextCorpus
	{
		public ParallelTextCorpus(ITextCorpus sourceCorpus, ITextCorpus targetCorpus, ITextAlignmentCorpus textAlignmentCorpus = null)
		{
			SourceCorpus = sourceCorpus;
			TargetCorpus = targetCorpus;
			TextAlignmentCorpus = textAlignmentCorpus;
		}

		public ITextCorpus SourceCorpus { get; }

		public ITextCorpus TargetCorpus { get; }

		public ITextAlignmentCorpus TextAlignmentCorpus { get; }

		public IEnumerable<ParallelText> Texts
		{
			get
			{
				foreach (IText text1 in SourceCorpus.Texts)
				{
					IText text2;
					if (TargetCorpus.TryGetText(text1.Id, out text2))
					{
						ITextAlignmentCollection textAlignmentCollection = null;
						if (TextAlignmentCorpus != null)
						{
							if (!TextAlignmentCorpus.TryGetTextAlignmentCollection(text1.Id, out textAlignmentCollection))
								textAlignmentCollection = null;
						}
						yield return new ParallelText(text1, text2, textAlignmentCollection);
					}
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

		public ParallelTextCorpus Invert()
		{
			return new ParallelTextCorpus(TargetCorpus, SourceCorpus, TextAlignmentCorpus?.Invert());
		}
	}
}
