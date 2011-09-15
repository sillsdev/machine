namespace SIL.APRE.Matching.Fluent
{
	public interface IQuantifierGroupSyntax<TOffset> : IAlternationGroupSyntax<TOffset>
	{
		IAlternationGroupSyntax<TOffset> ZeroOrMore { get; }
		IAlternationGroupSyntax<TOffset> LazyZeroOrMore { get; }

		IAlternationGroupSyntax<TOffset> OneOrMore { get; }
		IAlternationGroupSyntax<TOffset> LazyOneOrMore { get; }

		IAlternationGroupSyntax<TOffset> Optional { get; }
		IAlternationGroupSyntax<TOffset> LazyOptional { get; }

		IAlternationGroupSyntax<TOffset> Range(int min, int max);
		IAlternationGroupSyntax<TOffset> LazyRange(int min, int max);
	}
}
