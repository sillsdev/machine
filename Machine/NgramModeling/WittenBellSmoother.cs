using System;
using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.Statistics;

namespace SIL.Machine.NgramModeling
{
	public class WittenBellSmoother<TSeq, TItem> : INgramModelSmoother<TSeq, TItem>
	{
		private ConditionalFrequencyDistribution<Ngram<TItem>, TItem> _cfd; 
		private NgramModel<TSeq, TItem> _lowerOrderModel;
		private Direction _dir;

		public void Smooth(int ngramSize, TSeq[] sequences, Func<TSeq, IEnumerable<TItem>> itemsSelector, Direction dir, ConditionalFrequencyDistribution<Ngram<TItem>, TItem> cfd)
		{
			_cfd = cfd;
			_dir = dir;
			if (ngramSize > 1)
				_lowerOrderModel = new NgramModel<TSeq, TItem>(ngramSize - 1, sequences, itemsSelector, dir, new WittenBellSmoother<TSeq, TItem>());
		}

		public double GetProbability(TItem item, Ngram<TItem> context)
		{
			FrequencyDistribution<TItem> freqDist = _cfd[context];
			double numer = freqDist[item] + (freqDist.ObservedSamples.Count * (_lowerOrderModel == null ? 1.0 / freqDist.ObservedSamples.Count
				: _lowerOrderModel.GetProbability(item, context.SkipFirst(_dir))));
			double denom = freqDist.SampleOutcomeCount + freqDist.ObservedSamples.Count;
			return numer / denom;
		}

		public NgramModel<TSeq, TItem> LowerOrderModel
		{
			get { return _lowerOrderModel; }
		}
	}
}
