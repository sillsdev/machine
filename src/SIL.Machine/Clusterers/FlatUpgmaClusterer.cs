using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;

namespace SIL.Machine.Clusterers
{
    public class FlatUpgmaClusterer<T> : IFlatClusterer<T>
    {
        private readonly Func<T, T, double> _getDistance;
        private readonly double _threshold;

        public FlatUpgmaClusterer(Func<T, T, double> getDistance, double threshold)
        {
            _getDistance = getDistance;
            _threshold = threshold;
        }

        public IEnumerable<Cluster<T>> GenerateClusters(IEnumerable<T> dataObjects)
        {
            var clusters = new List<Cluster<T>>(
                dataObjects.Select(obj => new Cluster<T>(obj.ToEnumerable()) { Description = obj.ToString() })
            );
            while (clusters.Count >= 2)
            {
                int minI = 0,
                    minJ = 0;
                double minScore = double.MaxValue;
                for (int i = 0; i < clusters.Count; i++)
                {
                    for (int j = i + 1; j < clusters.Count; j++)
                    {
                        double[] distances = clusters[i]
                            .DataObjects.SelectMany(
                                o => clusters[j].DataObjects,
                                (o1, o2) =>
                                {
                                    double distance = _getDistance(o1, o2);
                                    if (double.IsNaN(distance) || double.IsInfinity(distance) || distance < 0)
                                        throw new ArgumentException(
                                            "Invalid distance between data objects.",
                                            "dataObjects"
                                        );
                                    return distance;
                                }
                            )
                            .ToArray();

                        double mean = distances.Average();
                        double sum = distances.Select(d => (d - mean) * (d - mean)).Sum();
                        double standardDeviation = Math.Sqrt(sum / distances.Length);
                        double score = mean - 0.25 * standardDeviation;
                        if (score < minScore)
                        {
                            minScore = score;
                            minI = i;
                            minJ = j;
                        }
                    }
                }

                if (minScore > _threshold)
                    break;

                Cluster<T> iCluster = clusters[minI];
                Cluster<T> jCluster = clusters[minJ];
                var uCluster = new Cluster<T>(iCluster.DataObjects.Concat(jCluster.DataObjects));
                clusters.RemoveAt(minJ);
                clusters.RemoveAt(minI);
                clusters.Add(uCluster);
            }

            return clusters;
        }
    }
}
