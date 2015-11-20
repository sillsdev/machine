using System.Linq;
using NUnit.Framework;
using SIL.Machine.Clusterers;

namespace SIL.Machine.Tests.Clusterers
{
	[TestFixture]
	public class DbscanClustererTests : ClustererTestsBase
	{
		[Test]
		public void Cluster_AllObjectsInSameCluster()
		{
			var matrix = new[,]
				{
					{0.00, 0.09, 0.12},
					{0.09, 0.00, 0.21},
					{0.12, 0.21, 0.00}
				};
			char[] objects = {'A', 'B', 'C'};
			var clusterer = new DbscanClusterer<char>(o1 => objects.Where(o2 => matrix[o1 - 'A', o2 - 'A'] <= 0.2), 2);
			Cluster<char>[] clusters = clusterer.GenerateClusters(objects).ToArray();
			Assert.That(clusters, Is.EquivalentTo(new[] {new Cluster<char>('A', 'B', 'C'), new Cluster<char>(Enumerable.Empty<char>(), true)}).Using(new ClusterEqualityComparer<char>()));
		}

		[Test]
		public void Cluster_NoiseNotEmpty()
		{
			var matrix = new[,]
				{
					{0.00, 0.09, 0.22},
					{0.09, 0.00, 0.30},
					{0.22, 0.30, 0.00}
				};
			char[] objects = {'A', 'B', 'C'};
			var clusterer = new DbscanClusterer<char>(o1 => objects.Where(o2 => matrix[o1 - 'A', o2 - 'A'] <= 0.2), 2);
			Cluster<char>[] clusters = clusterer.GenerateClusters(objects).ToArray();
			Assert.That(clusters, Is.EquivalentTo(new[] {new Cluster<char>('A', 'B'), new Cluster<char>(new[] {'C'}, true)}).Using(new ClusterEqualityComparer<char>()));
		}
	}
}
