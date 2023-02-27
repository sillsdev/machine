using System;
using System.Collections.Generic;
using System.Linq;
using QuickGraph;

namespace SIL.Machine.Clusterers
{
    public class OpticsXiClusterer<T> : OpticsRootedHierarchicalClusterer<T>
    {
        private readonly double _ixi;

        public OpticsXiClusterer(Optics<T> optics, double xi)
            : base(optics)
        {
            _ixi = 1.0 - xi;
        }

        public override IBidirectionalGraph<Cluster<T>, ClusterEdge<T>> GenerateClusters(
            IList<ClusterOrderEntry<T>> clusterOrder
        )
        {
            double mib = 0.0;
            var clusterIndices = new Dictionary<Cluster<T>, Tuple<int, int>>();
            var curClusters = new HashSet<Cluster<T>>();
            var unclassifed = new HashSet<T>(clusterOrder.Select(oe => oe.DataObject));
            var sdaSet = new List<SteepArea>();
            var tree = new BidirectionalGraph<Cluster<T>, ClusterEdge<T>>(false);
            var scan = new SteepScanPosition(clusterOrder);
            while (scan.HasNext)
            {
                int curPos = scan.Index;
                mib = Math.Max(mib, scan.Current.Reachability);
                if (scan.Successor != null)
                {
                    if (scan.SteepDown(_ixi))
                    {
                        UpdateFilterSdaSet(mib, sdaSet);
                        double startVal = scan.Current.Reachability;
                        int startSteep = scan.Index;
                        int endSteep = Math.Min(scan.Index + 1, clusterOrder.Count);
                        while (scan.HasNext)
                        {
                            scan.Next();
                            if (!scan.SteepDown(1.0))
                                break;
                            if (scan.SteepDown(_ixi))
                                endSteep = Math.Min(scan.Index + 1, clusterOrder.Count);
                            else if (scan.Index - endSteep > Optics.MinPoints)
                                break;
                        }
                        mib = clusterOrder[endSteep].Reachability;
                        var sda = new SteepArea(startSteep, endSteep, startVal);
                        sdaSet.Add(sda);
                        continue;
                    }
                    if (scan.SteepUp(_ixi))
                    {
                        UpdateFilterSdaSet(mib, sdaSet);
                        int startSteep = scan.Index;
                        int endSteep = scan.Index + 1;
                        mib = scan.Current.Reachability;
                        double succ = scan.Successor.Reachability;
                        if (!double.IsPositiveInfinity(succ))
                        {
                            while (scan.HasNext)
                            {
                                scan.Next();
                                if (!scan.SteepUp(1.0))
                                    break;

                                if (scan.SteepUp(_ixi))
                                {
                                    endSteep = Math.Min(scan.Index + 1, clusterOrder.Count - 1);
                                    mib = scan.Current.Reachability;
                                    succ = scan.Successor.Reachability;
                                }
                                else if (scan.Index - endSteep > Optics.MinPoints)
                                {
                                    break;
                                }
                            }
                        }
                        var sua = new SteepArea(startSteep, endSteep, succ);
                        foreach (SteepArea sda in ((IEnumerable<SteepArea>)sdaSet).Reverse())
                        {
                            if (mib * _ixi < sda.Mib)
                                continue;

                            int cstart = sda.StartIndex;
                            int cend = sua.EndIndex;
                            if (sda.Maximum * _ixi >= sua.Maximum)
                            {
                                while (cstart < sda.EndIndex)
                                {
                                    if (clusterOrder[cstart + 1].Reachability > sua.Maximum)
                                        cstart++;
                                    else
                                        break;
                                }
                            }
                            else if (sua.Maximum * _ixi >= sda.Maximum)
                            {
                                while (cend > sua.StartIndex)
                                {
                                    if (clusterOrder[cend - 1].Reachability > sda.Maximum)
                                        cend--;
                                    else
                                        break;
                                }
                            }

                            if (cend - cstart + 1 < Optics.MinPoints)
                                continue;

                            var cluster = new Cluster<T>(
                                clusterOrder
                                    .Skip(cstart)
                                    .Take(cend - cstart + 1)
                                    .Select(oe => oe.DataObject)
                                    .Intersect(unclassifed)
                            );
                            tree.AddVertex(cluster);
                            unclassifed.ExceptWith(cluster.DataObjects);

                            var toRemove = new HashSet<Cluster<T>>();
                            foreach (Cluster<T> curCluster in curClusters)
                            {
                                Tuple<int, int> indices = clusterIndices[curCluster];
                                if (cstart <= indices.Item1 && indices.Item2 <= cend)
                                {
                                    tree.AddEdge(new ClusterEdge<T>(cluster, curCluster));
                                    toRemove.Add(curCluster);
                                }
                            }
                            curClusters.ExceptWith(toRemove);
                            curClusters.Add(cluster);
                            clusterIndices[cluster] = Tuple.Create(cstart, cend);
                        }
                    }
                }

                if (curPos == scan.Index)
                    scan.Next();
            }

            if (unclassifed.Count > 0)
            {
                Cluster<T> allCluster = double.IsPositiveInfinity(clusterOrder.Last().Reachability)
                    ? new Cluster<T>(unclassifed, true)
                    : new Cluster<T>(unclassifed);
                tree.AddVertex(allCluster);
                foreach (Cluster<T> curCluster in curClusters)
                    tree.AddEdge(new ClusterEdge<T>(allCluster, curCluster));
            }

            return tree;
        }

        private void UpdateFilterSdaSet(double mib, List<SteepArea> sdaSet)
        {
            sdaSet.RemoveAll(sda => sda.Maximum * _ixi <= mib);
            foreach (SteepArea sda in sdaSet)
                sda.Mib = Math.Max(sda.Mib, mib);
        }

        private class SteepScanPosition
        {
            private readonly IList<ClusterOrderEntry<T>> _clusterOrder;
            private int _index;
            private ClusterOrderEntry<T> _current;
            private ClusterOrderEntry<T> _successor;

            public SteepScanPosition(IList<ClusterOrderEntry<T>> clusterOrder)
            {
                _clusterOrder = clusterOrder;
                _current = (clusterOrder.Count >= 1) ? clusterOrder[0] : null;
                _successor = (clusterOrder.Count >= 2) ? clusterOrder[1] : null;
            }

            public ClusterOrderEntry<T> Current
            {
                get { return _current; }
            }

            public ClusterOrderEntry<T> Successor
            {
                get { return _successor; }
            }

            public int Index
            {
                get { return _index; }
            }

            public void Next()
            {
                _index++;
                _current = _successor;
                if (_index + 1 < _clusterOrder.Count)
                    _successor = _clusterOrder[_index + 1];
            }

            public bool HasNext
            {
                get { return _index < _clusterOrder.Count; }
            }

            public bool SteepUp(double ixi)
            {
                if (double.IsPositiveInfinity(_current.Reachability))
                    return false;
                if (_successor == null)
                    return true;
                return _current.Reachability <= _successor.Reachability * ixi;
            }

            public bool SteepDown(double ixi)
            {
                if (_successor == null)
                    return false;
                if (double.IsPositiveInfinity(_successor.Reachability))
                    return false;
                return _current.Reachability * ixi >= _successor.Reachability;
            }
        }

        private class SteepArea
        {
            private readonly int _startIndex;
            private readonly int _endIndex;
            private readonly double _maximum;

            public SteepArea(int startIndex, int endIndex, double maximum, double mib = 0.0)
            {
                _startIndex = startIndex;
                _endIndex = endIndex;
                _maximum = maximum;
                Mib = mib;
            }

            public int StartIndex
            {
                get { return _startIndex; }
            }

            public int EndIndex
            {
                get { return _endIndex; }
            }

            public double Maximum
            {
                get { return _maximum; }
            }

            public double Mib { get; set; }
        }
    }
}
