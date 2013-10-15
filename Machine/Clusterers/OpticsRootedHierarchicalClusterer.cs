using System.Collections.Generic;
using QuickGraph;

namespace SIL.Machine.Clusterers
{
	public abstract class OpticsRootedHierarchicalClusterer<T> : IRootedHierarchicalClusterer<T>
	{
		private readonly Optics<T> _optics; 

		protected OpticsRootedHierarchicalClusterer(Optics<T> optics)
		{
			_optics = optics;
		}

		public Optics<T> Optics
		{
			get { return _optics; }
		}

		public IBidirectionalGraph<Cluster<T>, ClusterEdge<T>> GenerateClusters(IEnumerable<T> dataObjects)
		{
			return GenerateClusters(_optics.ClusterOrder(dataObjects));
		}

		public abstract IBidirectionalGraph<Cluster<T>, ClusterEdge<T>> GenerateClusters(IList<ClusterOrderEntry<T>> clusterOrder);
	}
}
