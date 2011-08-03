namespace SIL.APRE.Fsa
{
	public interface IArcCondition<TOffset>
	{
		bool IsMatch(Annotation<TOffset> ann, ModeType mode);

		IArcCondition<TOffset> Negation();

		IArcCondition<TOffset> Conjunction(IArcCondition<TOffset> cond);

		bool IsSatisfiable { get; }
	}
}
