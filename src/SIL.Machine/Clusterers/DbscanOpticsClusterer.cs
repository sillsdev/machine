using System.Collections.Generic;

namespace SIL.Machine.Clusterers
{
    public class DbscanOpticsClusterer<T> : OpticsFlatClusterer<T>
    {
        private readonly double _epsilon;

        public DbscanOpticsClusterer(Optics<T> optics, double epsilon)
            : base(optics)
        {
            _epsilon = epsilon;
        }

        public override IEnumerable<Cluster<T>> GenerateClusters(IList<ClusterOrderEntry<T>> clusterOrder)
        {
            var clusters = new List<Cluster<T>>();
            HashSet<T> curCluster = null;
            var noise = new HashSet<T>();
            foreach (ClusterOrderEntry<T> oe in clusterOrder)
            {
                if (oe.Reachability > _epsilon)
                {
                    if (oe.CoreDistance <= _epsilon)
                    {
                        if (curCluster != null)
                            clusters.Add(new Cluster<T>(curCluster));
                        curCluster = new HashSet<T> { oe.DataObject };
                    }
                    else
                        noise.Add(oe.DataObject);
                }
                else if (curCluster != null)
                    curCluster.Add(oe.DataObject);
                else
                    noise.Add(oe.DataObject);
            }
            if (curCluster != null)
                clusters.Add(new Cluster<T>(curCluster));
            if (noise.Count > 0)
                clusters.Add(new Cluster<T>(noise, true));
            return clusters;
        }
    }
}
