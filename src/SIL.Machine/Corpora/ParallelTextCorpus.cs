using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public class ParallelTextCorpus : ParallelTextCorpusBase
    {
        public ParallelTextCorpus(
            ITextCorpus sourceCorpus,
            ITextCorpus targetCorpus,
            IAlignmentCorpus alignmentCorpus = null,
            IComparer<object> rowRefComparer = null
        )
        {
            SourceCorpus = sourceCorpus;
            TargetCorpus = targetCorpus;
            AlignmentCorpus = alignmentCorpus ?? new DictionaryAlignmentCorpus();
            RowRefComparer = rowRefComparer ?? new NParallelTextCorpus.DefaultRowRefComparer();
            NParallelTextCorpus = new NParallelTextCorpus(new List<ITextCorpus> { SourceCorpus, TargetCorpus });
        }

        public override bool IsSourceTokenized => SourceCorpus.IsTokenized;
        public override bool IsTargetTokenized => TargetCorpus.IsTokenized;

        public bool AllSourceRows { get; set; }
        public bool AllTargetRows { get; set; }

        public ITextCorpus SourceCorpus { get; }
        public ITextCorpus TargetCorpus { get; }
        public IAlignmentCorpus AlignmentCorpus { get; }
        public IComparer<object> RowRefComparer { get; }

        public NParallelTextCorpus NParallelTextCorpus { get; }

        public override IEnumerable<ParallelTextRow> GetRows(IEnumerable<string> textIds)
        {
            using (IEnumerator<AlignmentRow> alignmentEnumerator = AlignmentCorpus.GetEnumerator())
            {
                NParallelTextCorpus.AllRows = new bool[] { AllSourceRows, AllTargetRows };
                bool isScripture = SourceCorpus.IsScripture() && TargetCorpus.IsScripture();
                foreach (var nRow in NParallelTextCorpus.GetRows(textIds))
                {
                    int compareAlignmentCorpus = -1;
                    if (AlignmentCorpus != null && nRow.NSegments.All(s => s.Count > 0))
                    {
                        do
                        {
                            try
                            {
                                compareAlignmentCorpus = alignmentEnumerator.MoveNext()
                                    ? RowRefComparer.Compare(nRow.Ref, alignmentEnumerator.Current.Ref)
                                    : 1;
                            }
                            catch (ArgumentException)
                            {
                                throw new CorpusAlignmentException(nRow.NRefs.Select(r => r.ToString()).ToArray());
                            }
                        } while (compareAlignmentCorpus < 0);
                    }
                    yield return new ParallelTextRow(
                        nRow.TextId,
                        nRow.NRefs[0].Count > 0 || !isScripture ? nRow.NRefs[0] : new object[] { nRow.Ref },
                        nRow.NRefs[1].Count > 0 || !isScripture ? nRow.NRefs[1] : new object[] { nRow.Ref }
                    )
                    {
                        SourceFlags = nRow.NFlags[0],
                        TargetFlags = nRow.NFlags[1],
                        SourceSegment = nRow.NSegments[0],
                        TargetSegment = nRow.NSegments[1],
                        AlignedWordPairs =
                            compareAlignmentCorpus == 0 ? alignmentEnumerator.Current.AlignedWordPairs.ToArray() : null
                    };
                }
            }
        }
    }
}
