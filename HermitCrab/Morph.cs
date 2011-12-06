using SIL.Machine;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a morph. Morphs are specific phonetic realizations of morphemes in
	/// surface forms.
	/// </summary>
	public class Morph
	{
		private readonly Span<ShapeNode> _span;
		private readonly Allomorph _allomorph;

		/// <summary>
		/// Initializes a new instance of the <see cref="Morph"/> class.
		/// </summary>
		/// <param name="span"></param>
		/// <param name="allomorph">The allomorph.</param>
		public Morph(Span<ShapeNode> span, Allomorph allomorph)
		{
			_span = span;
			_allomorph = allomorph;
		}

		/// <summary>
		/// Gets the span.
		/// </summary>
		/// <value>The span.</value>
		public Span<ShapeNode> Span
		{
			get { return _span; }
		}

		/// <summary>
		/// Gets the allomorph associated with this morph.
		/// </summary>
		/// <value>The allomorph.</value>
		public Allomorph Allomorph
		{
			get { return _allomorph; }
		}
	}
}
