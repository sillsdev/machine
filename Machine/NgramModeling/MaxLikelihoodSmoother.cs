using System;
using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.Statistics;

namespace SIL.Machine.NgramModeling
{
	public class MaxLikelihoodSmoother<TSeq, TItem> : INgramModelSmoother<TSeq, TItem>
	{
		private ConditionalFrequencyDistribution<Ngram<TItem>, TItem> _cfd;

		public void Smooth(int ngramSize, TSeq[] sequences, Func<TSeq, IEnumerable<TItem>> itemsSelector, Direction dir, ConditionalFrequencyDistribution<Ngram<TItem>, TItem> cfd)
		{
			_cfd = cfd;
		}

		public double GetProbability(TItem item, Ngram<TItem> context)
		{
			FrequencyDistribution<TItem> fd = _cfd[context];
			if (fd.SampleOutcomeCount == 0)
				return 0;
			return (double) fd[item] / fd.SampleOutcomeCount;
		}

		public NgramModel<TSeq, TItem> LowerOrderModel
		{
			get { return null; }
		}
	}
}
