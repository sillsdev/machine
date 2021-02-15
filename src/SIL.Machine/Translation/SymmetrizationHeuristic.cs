namespace SIL.Machine.Translation
{
	public enum SymmetrizationHeuristic
	{
		None,

		/// <summary>
		/// matrix union
		/// </summary>
		Union,

		/// <summary>
		/// matrix intersection
		/// </summary>
		Intersection,

		/// <summary>
		/// method in "Improved Alignment Models for Statistical Machine Translation" (Och et al., 1999).
		/// </summary>
		Och,

		/// <summary>
		/// "base" method in "Statistical Phrase-Based Translation" (Koehn et al., 2003) without final step.
		/// </summary>
		Grow,

		/// <summary>
		/// "diag" method in "Statistical Phrase-Based Translation" (Koehn et al., 2003) without final step.
		/// </summary>
		GrowDiag,

		/// <summary>
		/// "diag" method in "Statistical Phrase-Based Translation" (Koehn et al., 2003).
		/// </summary>
		GrowDiagFinal,

		/// <summary>
		/// "diag-and" method in "Statistical Phrase-Based Translation" (Koehn et al., 2003).
		/// </summary>
		GrowDiagFinalAnd
	}
}
