using System;
using System.Collections.Generic;
using System.Linq;
using QuickGraph;
using QuickGraph.Algorithms;
using SIL.Extensions;
using SIL.Machine.Clusterers;

namespace SIL.Machine.SequenceAlignment
{
    public class MultipleAlignmentAlgorithm<TSeq, TItem>
    {
        private readonly IPairwiseAlignmentScorer<TSeq, TItem> _scorer;
        private readonly TSeq[] _sequences;
        private readonly ItemsSelector<TSeq, TItem> _itemsSelector;
        private Alignment<TSeq, TItem> _result;

        public MultipleAlignmentAlgorithm(
            IPairwiseAlignmentScorer<TSeq, TItem> scorer,
            IEnumerable<TSeq> sequences,
            ItemsSelector<TSeq, TItem> itemsSelector
        )
        {
            _scorer = scorer;
            _sequences = sequences.ToArray();
            if (_sequences.Length < 3)
                throw new ArgumentException("At least three sequences must be specified.", "sequences");
            _itemsSelector = itemsSelector;
        }

        public bool UseInputOrder { get; set; }

        public void Compute()
        {
            var clusterer = new NeighborJoiningClusterer<TSeq>(
                (seq1, seq2) =>
                {
                    var pairwiseAligner = new PairwiseAlignmentAlgorithm<TSeq, TItem>(
                        _scorer,
                        seq1,
                        seq2,
                        _itemsSelector
                    );
                    pairwiseAligner.Compute();
                    return 1.0 - pairwiseAligner.GetAlignments().First().NormalizedScore;
                }
            );
            IUndirectedGraph<Cluster<TSeq>, ClusterEdge<TSeq>> unrootedTree = clusterer.GenerateClusters(_sequences);
            IBidirectionalGraph<Cluster<TSeq>, ClusterEdge<TSeq>> rootedTree = unrootedTree.ToRootedTree();

            var profiles = new Dictionary<Cluster<TSeq>, Profile<TSeq, TItem>>();
            var nodeStack = new Stack<Cluster<TSeq>>();
            Cluster<TSeq> root = rootedTree.Roots().First();
            double maxWeight = double.MinValue;
            if (root.DataObjects.Count == 1)
            {
                profiles[root] = CreateProfile(root.DataObjects.First(), 0);
                maxWeight = 0;
            }
            nodeStack.Push(root);
            foreach (ClusterEdge<TSeq> edge in rootedTree.OutEdges(root))
                maxWeight = Math.Max(maxWeight, CalcSequenceWeights(rootedTree, edge, 0, nodeStack, profiles));

            foreach (Profile<TSeq, TItem> profile in profiles.Values)
                profile.Weights[0] += 1.0 - maxWeight;

            var scorer = new ProfileScorer<TSeq, TItem>(_scorer);
            while (nodeStack.Count > 0)
            {
                Cluster<TSeq> node = nodeStack.Pop();

                var curProfiles = new Stack<Profile<TSeq, TItem>>();
                foreach (ClusterEdge<TSeq> childEdge in rootedTree.OutEdges(node))
                {
                    curProfiles.Push(profiles[childEdge.Target]);
                    profiles.Remove(childEdge.Target);
                }
                if (node.DataObjects.Count == 1)
                {
                    curProfiles.Push(profiles[node]);
                    profiles.Remove(node);
                }
                while (curProfiles.Count > 1)
                {
                    Profile<TSeq, TItem> profile1 = curProfiles.Pop();
                    Profile<TSeq, TItem> profile2 = curProfiles.Pop();
                    var profileAligner = new PairwiseAlignmentAlgorithm<Profile<TSeq, TItem>, AlignmentCell<TItem>[]>(
                        scorer,
                        profile1,
                        profile2,
                        GetProfileItems
                    );
                    profileAligner.Compute();
                    Alignment<Profile<TSeq, TItem>, AlignmentCell<TItem>[]> profileAlignment = profileAligner
                        .GetAlignments()
                        .First();
                    var sequences =
                        new List<
                            Tuple<TSeq, AlignmentCell<TItem>, IEnumerable<AlignmentCell<TItem>>, AlignmentCell<TItem>>
                        >();
                    for (int i = 0; i < profile1.Alignment.SequenceCount; i++)
                    {
                        int seq = i;
                        sequences.Add(
                            Tuple.Create(
                                profile1.Alignment.Sequences[i],
                                profile1.Alignment.Prefixes[i],
                                Enumerable
                                    .Range(0, profileAlignment.ColumnCount)
                                    .Select(
                                        col =>
                                            profileAlignment[0, col].IsNull
                                                ? new AlignmentCell<TItem>()
                                                : profileAlignment[0, col][0][seq]
                                    ),
                                profile1.Alignment.Suffixes[i]
                            )
                        );
                    }
                    for (int j = 0; j < profile2.Alignment.SequenceCount; j++)
                    {
                        int seq = j;
                        sequences.Add(
                            Tuple.Create(
                                profile2.Alignment.Sequences[j],
                                profile2.Alignment.Prefixes[j],
                                Enumerable
                                    .Range(0, profileAlignment.ColumnCount)
                                    .Select(
                                        col =>
                                            profileAlignment[1, col].IsNull
                                                ? new AlignmentCell<TItem>()
                                                : profileAlignment[1, col][0][seq]
                                    ),
                                profile2.Alignment.Suffixes[j]
                            )
                        );
                    }
                    var newAlignment = new Alignment<TSeq, TItem>(
                        profileAlignment.RawScore,
                        profileAlignment.NormalizedScore,
                        sequences
                    );
                    curProfiles.Push(new Profile<TSeq, TItem>(newAlignment, profile1.Weights.Concat(profile2.Weights)));
                }
                profiles[node] = curProfiles.Pop();
            }

            Alignment<TSeq, TItem> alignment = profiles[root].Alignment;
            if (UseInputOrder)
            {
                var reorderedSequences =
                    new List<
                        Tuple<TSeq, AlignmentCell<TItem>, IEnumerable<AlignmentCell<TItem>>, AlignmentCell<TItem>>
                    >();
                foreach (TSeq sequence in _sequences)
                {
                    for (int i = 0; i < alignment.SequenceCount; i++)
                    {
                        int seq = i;
                        if (sequence.Equals(alignment.Sequences[seq]))
                        {
                            reorderedSequences.Add(
                                Tuple.Create(
                                    sequence,
                                    alignment.Prefixes[seq],
                                    Enumerable.Range(0, alignment.ColumnCount).Select(col => alignment[seq, col]),
                                    alignment.Suffixes[seq]
                                )
                            );
                            break;
                        }
                    }
                }

                _result = new Alignment<TSeq, TItem>(alignment.RawScore, alignment.NormalizedScore, reorderedSequences);
            }
            else
            {
                _result = alignment;
            }
        }

        public Alignment<TSeq, TItem> GetAlignment()
        {
            return _result;
        }

        private IEnumerable<AlignmentCell<TItem>[]> GetProfileItems(
            Profile<TSeq, TItem> profile,
            out int startIndex,
            out int count
        )
        {
            startIndex = 0;
            count = profile.Alignment.ColumnCount;
            return Enumerable
                .Range(0, profile.Alignment.ColumnCount)
                .Select(
                    col =>
                        Enumerable
                            .Range(0, profile.Alignment.SequenceCount)
                            .Select(seq => profile.Alignment[seq, col])
                            .ToArray()
                );
        }

        private double CalcSequenceWeights(
            IBidirectionalGraph<Cluster<TSeq>, ClusterEdge<TSeq>> tree,
            ClusterEdge<TSeq> edge,
            double curWeight,
            Stack<Cluster<TSeq>> nodeStack,
            Dictionary<Cluster<TSeq>, Profile<TSeq, TItem>> profiles
        )
        {
            double length = edge.Length;
            if (tree.IsOutEdgesEmpty(edge.Target))
            {
                TSeq seq = edge.Target.DataObjects.First();
                double weight = curWeight + length;
                profiles[edge.Target] = CreateProfile(seq, weight);
                return weight;
            }

            nodeStack.Push(edge.Target);
            double lengthPart = length / tree.OutDegree(edge.Target);
            double maxWeight = double.MinValue;
            foreach (ClusterEdge<TSeq> childEdge in tree.OutEdges(edge.Target))
                maxWeight = Math.Max(
                    maxWeight,
                    CalcSequenceWeights(tree, childEdge, curWeight + lengthPart, nodeStack, profiles)
                );
            return maxWeight;
        }

        private Profile<TSeq, TItem> CreateProfile(TSeq seq, double weight)
        {
            int startIndex,
                count;
            TItem[] items = _itemsSelector(seq, out startIndex, out count).ToArray();

            return new Profile<TSeq, TItem>(
                new Alignment<TSeq, TItem>(
                    0,
                    0,
                    Tuple.Create(
                        seq,
                        new AlignmentCell<TItem>(items.Take(startIndex)),
                        items.Skip(startIndex).Take(count).Select(item => new AlignmentCell<TItem>(item)),
                        new AlignmentCell<TItem>(items.Skip(startIndex + count))
                    )
                ),
                weight.ToEnumerable()
            );
        }
    }
}
