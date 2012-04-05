using SIL.Machine;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents an allomorph in a lexical entry.
	/// </summary>
	public class RootAllomorph : Allomorph
	{
		private readonly Shape _shape;

		/// <summary>
		/// Initializes a new instance of the <see cref="RootAllomorph"/> class.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="shape">The shape.</param>
		public RootAllomorph(string id, Shape shape)
			: base(id)
		{
			_shape = shape;
		}

		/// <summary>
		/// Gets the phonetic shape.
		/// </summary>
		/// <value>The phonetic shape.</value>
		public Shape Shape
		{
			get
			{
				return _shape;
			}
		}

		public override bool ConstraintsEqual(Allomorph other)
		{
			var otherAllo = other as RootAllomorph;
			if (otherAllo == null)
				return false;

			return base.ConstraintsEqual(other) && _shape.ValueEquals(otherAllo._shape);
		}
	}
}
