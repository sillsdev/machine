namespace SIL.APRE.Matching.Fluent
{
	public interface IAlternationGroupSyntax<TData, TOffset> : IGroupSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		IGroupSyntax<TData, TOffset> Or { get; }
	}
}
