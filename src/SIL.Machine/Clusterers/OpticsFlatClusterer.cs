using System.Collections.Generic;

namespace SIL.Machine.Clusterers
{
    public abstract class OpticsFlatClusterer<T> : IFlatClusterer<T>
    {
        private readonly Optics<T> _optics;

        protected OpticsFlatClusterer(Optics<T> optics)
        {
            _optics = optics;
        }

        public Optics<T> Optics
        {
            get { return _optics; }
        }

        public IEnumerable<Cluster<T>> GenerateClusters(IEnumerable<T> dataObjects)
        {
            return GenerateClusters(_optics.ClusterOrder(dataObjects));
        }

        public abstract IEnumerable<Cluster<T>> GenerateClusters(IList<ClusterOrderEntry<T>> clusterOrder);
    }
}
