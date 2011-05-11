namespace SIL.APRE.Fsa
{
	public interface ITransitionCondition<TOffset, TData>
	{
		bool IsMatch(Annotation<TOffset> ann, ModeType mode, ref TData data);
	}
}
