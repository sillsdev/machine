namespace SIL.APRE.Transduction
{
	public interface IRule<TOffset>
	{
		bool IsApplicable(IBidirList<Annotation<TOffset>> input);

		bool Apply(IBidirList<Annotation<TOffset>> input);
	}
}
