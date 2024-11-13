using System.Collections.Generic;

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
            NParallelTextCorpus = new NParallelTextCorpus(new List<ITextCorpus> { SourceCorpus, TargetCorpus })
            {
                AlignmentCorpus = AlignmentCorpus
            };
        }

        public override bool IsSourceTokenized => SourceCorpus.IsTokenized;
        public override bool IsTargetTokenized => TargetCorpus.IsTokenized;

        public bool AllSourceRows { get; set; }
        public bool AllTargetRows { get; set; }

        public ITextCorpus SourceCorpus { get; }
        public ITextCorpus TargetCorpus { get; }
        public IAlignmentCorpus AlignmentCorpus { get; }
        public IComparer<object> RowRefComparer { get; }

        private NParallelTextCorpus NParallelTextCorpus { get; set; }

        public override IEnumerable<ParallelTextRow> GetRows(IEnumerable<string> textIds)
        {
            NParallelTextCorpus.AllRows = new bool[] { AllSourceRows, AllTargetRows };
            bool isScripture = SourceCorpus.IsScripture() && TargetCorpus.IsScripture();
            foreach (var nRow in NParallelTextCorpus.GetRows(textIds))
            {
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
                    AlignedWordPairs = nRow.AlignedWordPairs
                };
            }
        }
    }
}
