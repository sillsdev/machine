using System;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a morph. Morphs are specific phonetic realizations of morphemes in
	/// surface forms.
	/// </summary>
	public class Morph : ICloneable
	{
		private int _partition = -1;
		private readonly PhoneticShape _shape;
		private readonly Allomorph _allomorph;

		/// <summary>
		/// Initializes a new instance of the <see cref="Morph"/> class.
		/// </summary>
		/// <param name="allomorph">The allomorph.</param>
		public Morph(Allomorph allomorph)
		{
			_allomorph = allomorph;
#if WANTPORT
			_shape = new PhoneticShape();
#endif
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="morph">The morph.</param>
		public Morph(Morph morph)
		{
			_partition = morph._partition;
			_shape = morph._shape.Clone();
			_allomorph = morph._allomorph;
		}

		/// <summary>
		/// Gets or sets the partition.
		/// </summary>
		/// <value>The partition.</value>
		public int Partition
		{
			get
			{
				return _partition;
			}

			internal set
			{
				_partition = value;
			}
		}

		/// <summary>
		/// Gets the phonetic shape.
		/// </summary>
		/// <value>The phonetic shape.</value>
		public PhoneticShape Shape
		{
			get
			{
				return _shape;
			}
		}

		/// <summary>
		/// Gets the allomorph associated with this morph.
		/// </summary>
		/// <value>The allomorph.</value>
		public Allomorph Allomorph
		{
			get
			{
				return _allomorph;
			}
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Morph);
		}

		public bool Equals(Morph other)
		{
			if (other == null)
				return false;

			return _allomorph == other._allomorph;
		}

		public override int GetHashCode()
		{
			return _allomorph.GetHashCode();
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public Morph Clone()
		{
			return new Morph(this);
		}
	}
}
