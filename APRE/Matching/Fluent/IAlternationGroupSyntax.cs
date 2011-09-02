namespace SIL.APRE.Matching.Fluent
{
	public interface IAlternationGroupSyntax<TOffset> : IGroupSyntax<TOffset>
	{
		IGroupSyntax<TOffset> Or { get; }
	}
}
