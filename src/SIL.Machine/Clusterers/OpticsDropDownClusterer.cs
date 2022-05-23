using System;
using System.Collections.Generic;
using System.Linq;
using QuickGraph;

namespace SIL.Machine.Clusterers
{
    public class OpticsDropDownClusterer<T> : OpticsRootedHierarchicalClusterer<T>
    {
        public OpticsDropDownClusterer(Optics<T> optics) : base(optics) { }

        public override IBidirectionalGraph<Cluster<T>, ClusterEdge<T>> GenerateClusters(
            IList<ClusterOrderEntry<T>> clusterOrder
        )
        {
            var processed = new HashSet<int>();
            var tree = new BidirectionalGraph<Cluster<T>, ClusterEdge<T>>(false);
            GetSubclusters(processed, tree, clusterOrder, 0, clusterOrder.Count);
            return tree;
        }

        private Cluster<T> CreateCluster(
            HashSet<int> processed,
            BidirectionalGraph<Cluster<T>, ClusterEdge<T>> tree,
            IList<ClusterOrderEntry<T>> clusterOrder,
            int startIndex,
            int endIndex
        )
        {
            var subclusterDataObjects = new HashSet<T>();
            var subclusters = new List<Cluster<T>>();
            foreach (Cluster<T> subcluster in GetSubclusters(processed, tree, clusterOrder, startIndex, endIndex))
            {
                subclusterDataObjects.UnionWith(tree.GetAllDataObjects(subcluster));
                subclusters.Add(subcluster);
            }

            for (int i = startIndex; i < endIndex; i++)
                processed.Add(i);

            var cluster = new Cluster<T>(
                clusterOrder
                    .Skip(startIndex)
                    .Take(endIndex - startIndex)
                    .Select(oe => oe.DataObject)
                    .Except(subclusterDataObjects)
            );
            tree.AddVertex(cluster);
            foreach (Cluster<T> subcluster in subclusters)
                tree.AddEdge(new ClusterEdge<T>(cluster, subcluster));

            return cluster;
        }

        private IEnumerable<Cluster<T>> GetSubclusters(
            HashSet<int> processed,
            BidirectionalGraph<Cluster<T>, ClusterEdge<T>> tree,
            IList<ClusterOrderEntry<T>> clusterOrder,
            int startIndex,
            int endIndex
        )
        {
            var subclusters = new List<Cluster<T>>();
            int parentCount = endIndex - startIndex;
            Tuple<ClusterOrderEntry<T>, int>[] reachOrder = clusterOrder
                .Skip(startIndex + 1)
                .Take(endIndex - startIndex - 1)
                .Select((oe, index) => Tuple.Create(oe, startIndex + index + 1))
                .OrderByDescending(oe => oe.Item1.Reachability)
                .ThenBy(oe => oe.Item2)
                .ToArray();
            for (int i = 0; i < reachOrder.Length; i++)
            {
                Tuple<ClusterOrderEntry<T>, int> entry = reachOrder[i];
                if (processed.Contains(entry.Item2))
                    continue;

                if (entry.Item2 != clusterOrder.Count - 1 && entry.Item2 == startIndex + 1)
                {
                    // is this an inflexion point?
                    if (
                        (clusterOrder[startIndex].Reachability / entry.Item2)
                        < ((entry.Item2 / clusterOrder[entry.Item2 + 1].Reachability) * 0.75)
                    )
                    {
                        startIndex = entry.Item2;
                        int j = i + 1;
                        for (; j < reachOrder.Length && Math.Abs(reachOrder[j].Item2 - entry.Item2) < 0.00001; j++)
                        {
                            if (
                                reachOrder[j].Item2 != startIndex + 1
                                && IsValidCluster(parentCount, startIndex, reachOrder[j].Item2)
                            )
                            {
                                subclusters.Add(
                                    CreateCluster(processed, tree, clusterOrder, startIndex, reachOrder[j].Item2)
                                );
                                startIndex = reachOrder[j].Item2;
                            }
                        }
                        if (IsValidCluster(parentCount, startIndex, endIndex))
                        {
                            subclusters.Add(CreateCluster(processed, tree, clusterOrder, startIndex, endIndex));
                            break;
                        }
                    }
                    else
                    {
                        startIndex = entry.Item2;
                    }
                }
                else if (entry.Item2 == endIndex - 1)
                {
                    // is this an inflexion point?
                    if (
                        endIndex != clusterOrder.Count
                        && (clusterOrder[entry.Item2 - 1].Reachability / entry.Item2)
                            < ((entry.Item2 / clusterOrder[endIndex].Reachability) * 0.75)
                        && IsValidCluster(parentCount, startIndex, entry.Item2)
                    )
                    {
                        subclusters.Add(CreateCluster(processed, tree, clusterOrder, startIndex, entry.Item2));
                        break;
                    }
                    endIndex = entry.Item2;
                }
                else
                {
                    if (IsValidCluster(parentCount, startIndex, entry.Item2))
                        subclusters.Add(CreateCluster(processed, tree, clusterOrder, startIndex, entry.Item2));
                    startIndex = entry.Item2;
                    int j = i + 1;
                    for (; j < reachOrder.Length && Math.Abs(reachOrder[j].Item2 - entry.Item2) < 0.00001; j++)
                    {
                        if (
                            reachOrder[j].Item2 != startIndex + 1
                            && IsValidCluster(parentCount, startIndex, reachOrder[j].Item2)
                        )
                        {
                            subclusters.Add(
                                CreateCluster(processed, tree, clusterOrder, startIndex, reachOrder[j].Item2)
                            );
                            startIndex = reachOrder[j].Item2;
                        }
                    }
                    if (IsValidCluster(parentCount, startIndex, endIndex))
                    {
                        subclusters.Add(CreateCluster(processed, tree, clusterOrder, startIndex, endIndex));
                        break;
                    }
                }
            }

            return subclusters;
        }

        private bool IsValidCluster(int parentCount, int startIndex, int endIndex)
        {
            int clusterSize = endIndex - startIndex;
            return clusterSize >= Optics.MinPoints && parentCount - clusterSize >= Optics.MinPoints;
        }
    }
}
