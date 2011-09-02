namespace SIL.APRE.Matching.Fluent
{
	public interface IQuantifierGroupSyntax<TOffset> : IAlternationGroupSyntax<TOffset>
	{
		IAlternationGroupSyntax<TOffset> ZeroOrMore { get; }

		IAlternationGroupSyntax<TOffset> OneOrMore { get; }

		IAlternationGroupSyntax<TOffset> Optional { get; }

		IAlternationGroupSyntax<TOffset> Range(int min, int max);
	}
}
