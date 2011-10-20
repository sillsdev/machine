using System.Collections.Generic;
using System.Text;
using SIL.APRE.FeatureModel;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a lexical entry.
	/// </summary>
	public class LexEntry : Morpheme
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
			/// <param name="desc">The description.</param>
			/// <param name="shape">The shape.</param>
			public RootAllomorph(string id, string desc, Shape shape)
				: base(id, desc)
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

			public override string ToString()
			{
				return _shape.ToString();
			}
		}

		private readonly List<RootAllomorph> _allomorphs;
		private PartOfSpeech _partOfSpeech;
		private MprFeatureSet _mprFeatures;
		private FeatureStruct _headFeatures;
		private FeatureStruct _footFeatures;
		private LexFamily _family;

		/// <summary>
		/// Initializes a new instance of the <see cref="LexEntry"/> class.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <param name="desc">The description.</param>
		public LexEntry(string id, string desc)
			: base(id, desc)
		{
			_allomorphs = new List<RootAllomorph>();
		}

		/// <summary>
		/// Gets the primary allomorph. This is the first allomorph.
		/// </summary>
		/// <value>The primary allomorph.</value>
		public RootAllomorph PrimaryAllomorph
		{
			get
			{
				if (_allomorphs.Count == 0)
					return null;
				return _allomorphs[0];
			}
		}

		/// <summary>
		/// Gets the allomorphs.
		/// </summary>
		/// <value>The allomorphs.</value>
		public IEnumerable<RootAllomorph> Allomorphs
		{
			get
			{
				return _allomorphs;
			}
		}

		/// <summary>
		/// Gets the allomorph count.
		/// </summary>
		/// <value>The allomorph count.</value>
		public int AllomorphCount
		{
			get
			{
				return _allomorphs.Count;
			}
		}

		/// <summary>
		/// Gets or sets the part of speech.
		/// </summary>
		/// <value>The part of speech.</value>
		public PartOfSpeech PartOfSpeech
		{
			get
			{
				return _partOfSpeech;
			}

			set
			{
				_partOfSpeech = value;
			}
		}

		/// <summary>
		/// Gets the MPR features.
		/// </summary>
		/// <value>The MPR features.</value>
		public MprFeatureSet MprFeatures
		{
			get
			{
				return _mprFeatures;
			}

			set
			{
				_mprFeatures = value;
			}
		}

		/// <summary>
		/// Gets the head features.
		/// </summary>
		/// <value>The head features.</value>
		public FeatureStruct HeadFeatures
		{
			get
			{
				return _headFeatures;
			}

			set
			{
				_headFeatures = value;
			}
		}

		/// <summary>
		/// Gets the foot features.
		/// </summary>
		/// <value>The foot features.</value>
		public FeatureStruct FootFeatures
		{
			get
			{
				return _footFeatures;
			}

			set
			{
				_footFeatures = value;
			}
		}

		/// <summary>
		/// Gets or sets the lexical family.
		/// </summary>
		/// <value>The lexical family.</value>
		public LexFamily Family
		{
			get
			{
				return _family;
			}

			internal set
			{
				_family = value;
			}
		}

		/// <summary>
		/// Adds the specified allomorph.
		/// </summary>
		/// <param name="allomorph">The allomorph.</param>
		public void AddAllomorph(RootAllomorph allomorph)
		{
			allomorph.Morpheme = this;
			allomorph.Index = _allomorphs.Count;
			_allomorphs.Add(allomorph);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			bool firstItem = true;
			foreach (RootAllomorph allomorph in _allomorphs)
			{
				if (!firstItem)
					sb.Append(", ");
				sb.Append(Stratum.CharacterDefinitionTable.ToString(allomorph.Shape, Mode.Synthesis, true));
				firstItem = false;
			}

			return string.Format(HCStrings.kstidLexEntry, ID, sb, Gloss == null ? "?" : Gloss.Description);
		}
	}
}
