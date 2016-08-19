namespace SIL.Machine.Morphology
{
	public enum MorphemeType
	{
		Stem,
		Affix
	}

	/// <summary>
	/// This interface represents a morpheme.
	/// </summary>
	public interface IMorpheme
	{
		/// <summary>
		/// Gets the unique identifier.
		/// </summary>
		string Id { get; }

		/// <summary>
		/// Gets the category or part of speech.
		/// </summary>
		string Category { get; }

		/// <summary>
		/// Gets the gloss.
		/// </summary>
		string Gloss { get; }

		/// <summary>
		/// Gets the morpheme type.
		/// </summary>
		MorphemeType MorphemeType { get; }
	}
}
