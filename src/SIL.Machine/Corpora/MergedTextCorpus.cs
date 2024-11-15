using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class MergedTextCorpus : TextCorpusBase
    {
        private readonly NParallelTextCorpus _corpus;

        private readonly MergeRule _mergeRule;

        private readonly Random _random;

        public MergedTextCorpus(
            IEnumerable<ITextCorpus> corpora,
            IEnumerable<bool> allRows,
            MergeRule mergeRule,
            int seed
        )
        {
            _corpus = new NParallelTextCorpus(corpora) { AllRows = allRows.ToList() };
            _mergeRule = mergeRule;
            _random = new Random(seed);
        }

        public override IEnumerable<IText> Texts => _corpus.Corpora.SelectMany(c => c.Texts);

        public override bool IsTokenized => Enumerable.Range(0, _corpus.N).All(i => _corpus.IsTokenized(i));

        public override ScrVers Versification => _corpus.N > 0 ? _corpus.Corpora[0].Versification : null;

        public override IEnumerable<TextRow> GetRows(IEnumerable<string> textIds)
        {
            int indexOfInRangeRow = -1;
            foreach (NParallelTextRow nRow in _corpus.GetRows(textIds))
            {
                IReadOnlyList<int> nonEmptyIndices = nRow
                    .NSegments.Select((s, i) => (s, i))
                    .Where(pair => pair.s.Count > 0 || nRow.IsInRange(pair.i))
                    .Select(pair => pair.i)
                    .ToList();
                IReadOnlyList<int> indices =
                    nonEmptyIndices.Count > 0 ? nonEmptyIndices : Enumerable.Range(0, nRow.N).ToList();
                if (indexOfInRangeRow == -1)
                {
                    indices = indices.Where(i => nRow.IsRangeStart(i) || !nRow.IsInRange(i)).ToList();
                }
                if (indices.Count == 0)
                    continue;
                int indexOfSelectedRow = -1;
                switch (_mergeRule)
                {
                    case MergeRule.First:
                        indexOfSelectedRow = indices.First();
                        break;
                    case MergeRule.Random:
                        indexOfSelectedRow = indices[_random.Next(0, indices.Count)];
                        break;
                }
                indexOfSelectedRow = indexOfInRangeRow != -1 ? indexOfInRangeRow : indexOfSelectedRow;
                if (!nRow.IsInRange(indexOfSelectedRow))
                {
                    indexOfInRangeRow = -1;
                }
                if (nRow.IsRangeStart(indexOfSelectedRow))
                {
                    indexOfInRangeRow = indexOfSelectedRow;
                }
                yield return new TextRow(nRow.TextId, nRow.Ref)
                {
                    Segment = nRow.NSegments[indexOfSelectedRow],
                    Flags = nRow.NFlags[indexOfSelectedRow]
                };
            }
        }
    }
}
