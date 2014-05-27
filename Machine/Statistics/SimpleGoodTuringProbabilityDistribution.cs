using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;

namespace SIL.Machine.Statistics
{
	public class SimpleGoodTuringProbabilityDistribution<TSample> : IProbabilityDistribution<TSample>
	{
		private readonly FrequencyDistribution<TSample> _freqDist;
		private readonly double _slope;
		private readonly double _intercept;
		private readonly Dictionary<int, double> _probs;
		private readonly double _probZero;
		private readonly int _binCount;

		public SimpleGoodTuringProbabilityDistribution(FrequencyDistribution<TSample> freqDist, int binCount)
		{
			if (binCount <= freqDist.ObservedSamples.Count)
				throw new ArgumentOutOfRangeException("binCount");

			_freqDist = freqDist;
			_binCount = binCount;
			_probs = new Dictionary<int, double>();

			if (freqDist.ObservedSamples.Count == 0)
				return;

			var r = new List<int>();
			var nr = new List<int>();
			int b = 0, i = 0;
			while (b != _freqDist.ObservedSamples.Count)
			{
				int nri = _freqDist.ObservedSamples.Count(s => _freqDist[s] == i);
				if (nri > 0)
				{
					b += nri;
					r.Add(i);
					nr.Add(nri);
				}
				i++;
			}

			var zr = new double[r.Count];
			var logr = new double[r.Count];
			var logzr = new double[r.Count];
			for (int j = 0; j < r.Count; j++)
			{
				i = j > 0 ? r[j - 1] : 0;
				int k = j == r.Count - 1 ? 2 * r[j] - i : r[j + 1];
				zr[j] = 2.0 * nr[j] / (k - i);
				logr[j] = Math.Log(r[j]);
				logzr[j] = Math.Log(zr[j]);
			}

			double xycov = 0, xvar = 0, xmean = 0, ymean = 0;
			for (int j = 0; j < r.Count; j++)
			{
				xmean += logr[j];
				ymean += logzr[j];
			}
			xmean /= r.Count;
			ymean /= r.Count;
			for (int j = 0; j < logr.Length; j++)
			{
				xycov += (logr[j] - xmean) * (logzr[j] - ymean);
				xvar += Math.Pow(logr[j] - xmean, 2);
			}
			_slope = Math.Abs(xvar - 0) > double.Epsilon ? xycov / xvar : 0;
			_intercept = ymean - _slope * xmean;

			var rstar = new double[r.Count];
			for (int j = 0; j < r.Count; j++)
			{
				double smoothRstar = (r[j] + 1) * GetSmoothedSamplesCount(r[j] + 1) / GetSmoothedSamplesCount(r[j]);
				if (r.Count == j + 1 || r[j + 1] != r[j] + 1)
					rstar[j] = smoothRstar;
				else
				{
					double unsmoothRstar = (double) (r[j] + 1) * nr[j + 1] / nr[j];
					double std = Math.Sqrt(GetVariance(r[j], nr[j], nr[j + 1]));
					if (Math.Abs(unsmoothRstar - smoothRstar) <= 1.96 * std)
						rstar[j] = smoothRstar;
					else
						rstar[j] = unsmoothRstar;
				}
			}

			double samplesCountPrime = 0;
			for (int j = 0; j < r.Count; j++)
				samplesCountPrime += nr[j] * rstar[j];

			_probZero = (double) _freqDist.ObservedSamples.Count(s => _freqDist[s] == 1) / _freqDist.SampleOutcomeCount;
			for (int j = 0; j < r.Count; j++)
				_probs[r[j]] = (1.0 - _probZero) * rstar[j] / samplesCountPrime;
		}

		private double GetSmoothedSamplesCount(int r)
		{
			return Math.Exp(_intercept + _slope * Math.Log(r));
		}

		private double GetVariance(int r, int nr, int nr1)
		{
			return Math.Pow(r + 1, 2) * (nr1 / Math.Pow(nr, 2)) * (1.0 + (double) nr1 / nr);
		}

		public IReadOnlyCollection<TSample> Samples
		{
			get { return _freqDist.ObservedSamples; }
		}

		public double this[TSample sample]
		{
			get
			{
				int count = _freqDist[sample];
				double prob;
				if (_probs.TryGetValue(count, out prob))
					return prob;
				return _probZero / (_binCount - _freqDist.ObservedSamples.Count);
			}
		}

		public double Discount
		{
			get
			{
				if (_freqDist.ObservedSamples.Count == 0)
					return 0;
				return GetSmoothedSamplesCount(1) / _freqDist.SampleOutcomeCount;
			}
		}

		public FrequencyDistribution<TSample> FrequencyDistribution
		{
			get { return _freqDist; }
		}
	}
}
