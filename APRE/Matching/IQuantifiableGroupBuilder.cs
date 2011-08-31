namespace SIL.APRE.Matching
{
	public interface IQuantifiableGroupBuilder<TOffset> : IGroupBuilder<TOffset>
	{
		IGroupBuilder<TOffset> ZeroOrMore { get; }

		IGroupBuilder<TOffset> OneOrMore { get; }

		IGroupBuilder<TOffset> Optional { get; }

		IGroupBuilder<TOffset> Range(int min, int max);
	}
}
