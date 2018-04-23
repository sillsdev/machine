using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;

namespace SIL.Machine.Statistics
{
	public static class StatisticalMethods
	{
		public static double CosineSimilarity(IEnumerable<double> dist1, IEnumerable<double> dist2)
		{
			double dot = 0, obsTotal = 0, expTotal = 0;
			foreach (Tuple<double, double> t in dist1.Zip(dist2))
			{
				dot += t.Item1 * t.Item2;
				obsTotal += t.Item1 * t.Item1;
				expTotal += t.Item2 * t.Item2;
			}
			if (obsTotal == 0 || expTotal == 0)
				return 0;
			return dot / (Math.Sqrt(obsTotal) * Math.Sqrt(expTotal));
		}

		public static double EuclideanDistance(IEnumerable<double> dist1, IEnumerable<double> dist2)
		{
			return Math.Sqrt(dist1.Zip(dist2, (p1, p2) => Math.Pow(p2 - p1, 2)).Sum());
		}

		public static double JensenShannonDivergence(IEnumerable<double> dist1, IEnumerable<double> dist2)
		{
			double[] d1 = dist1.ToArray();
			double[] d2 = dist2.ToArray();
			double[] avg = d1.Zip(d2, (o, e) => (o + e) / 2).ToArray();

			double kl1 = KullbackLeiblerDivergence(d1, avg);
			double kl2 = KullbackLeiblerDivergence(d2, avg);
			return (kl1 + kl2) / 2;
		}

		public static double KullbackLeiblerDivergence(IEnumerable<double> dist1, IEnumerable<double> dist2)
		{
			return dist1.Zip(dist2, (p1, p2) => p1 == 0 || p2 == 0 ? 0 : Math.Log(p1 / p2, 2) * p1).Sum();
		}
	}
}
