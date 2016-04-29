using System.Collections.Generic;

namespace SIL.Machine.Morphology
{
	public interface IStemmer<in TSeq>
	{
		void Train(IEnumerable<TSeq> sequences);
		bool HaveSameStem(TSeq x, TSeq y);
		bool HaveSameStem(TSeq x, TSeq y, out double score);
	}
}
