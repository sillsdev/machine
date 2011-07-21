namespace SIL.APRE.Fsa
{
	public interface ITransitionCondition<TOffset, TData>
	{
		bool IsMatch(Annotation<TOffset> ann, ModeType mode, ref TData data);

		ITransitionCondition<TOffset, TData> Negation();

		ITransitionCondition<TOffset, TData> Conjunction(ITransitionCondition<TOffset, TData> cond);

		bool IsSatisfiable { get; }
	}
}
