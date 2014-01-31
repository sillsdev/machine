using System.Collections.Generic;

namespace SIL.Machine.Morphology
{
	public interface IAffixIdentifier<in TSeq, TItem>
	{
		IEnumerable<Affix<TItem>> IdentifyAffixes(IEnumerable<TSeq> sequences, AffixType affixType);
	}
}
