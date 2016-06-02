using System;
using System.Collections.Generic;
using SIL.Machine.DataStructures;
using SIL.Machine.Statistics;

namespace SIL.Machine.NgramModeling
{
	public interface INgramModelSmoother<TSeq, TItem>
	{
		void Smooth(int ngramSize, TSeq[] sequences, Func<TSeq, IEnumerable<TItem>> itemsSelector, Direction dir, ConditionalFrequencyDistribution<Ngram<TItem>, TItem> cfd);

		double GetProbability(TItem item, Ngram<TItem> context);

		NgramModel<TSeq, TItem> LowerOrderModel { get; }
	}
}
