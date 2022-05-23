using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.Clusterers
{
    public class FlatUpgmaClustererTests : ClustererTestsBase
    {
        [Test]
        public void Cluster()
        {
            var matrix = new[,]
            {
                { 0.00, 0.50, 0.67, 0.80, 0.20 },
                { 0.50, 0.00, 0.40, 0.70, 0.60 },
                { 0.67, 0.40, 0.00, 0.80, 0.80 },
                { 0.80, 0.70, 0.80, 0.00, 0.30 },
                { 0.20, 0.60, 0.80, 0.30, 0.00 }
            };
            var fupgma = new FlatUpgmaClusterer<char>((o1, o2) => matrix[o1 - 'A', o2 - 'A'], 0.5);
            Cluster<char>[] clusters = fupgma.GenerateClusters(new[] { 'A', 'B', 'C', 'D', 'E' }).ToArray();

            var expected = new[] { new Cluster<char>('B', 'C'), new Cluster<char>('A', 'E', 'D') };

            Assert.That(clusters, Is.EquivalentTo(expected).Using(new ClusterEqualityComparer<char>()));
        }
    }
}
