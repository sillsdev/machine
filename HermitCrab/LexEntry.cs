using System.Collections.Generic;
using System.Text;
using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a lexical entry.
	/// </summary>
	public class LexEntry : Morpheme
	{
		private readonly List<RootAllomorph> _allomorphs;
		private readonly FeatureStruct _syntacticFS;

		/// <summary>
		/// Initializes a new instance of the <see cref="LexEntry"/> class.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <param name="syntacticFS"></param>
		public LexEntry(string id, FeatureStruct syntacticFS)
			: base(id)
		{
			_syntacticFS = syntacticFS;
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
		/// Gets the MPR features.
		/// </summary>
		/// <value>The MPR features.</value>
		public MprFeatureSet MprFeatures { get; set; }

		/// <summary>
		/// Gets the head features.
		/// </summary>
		/// <value>The head features.</value>
		public FeatureStruct SyntacticFeatureStruct
		{
			get { return _syntacticFS; }
		}

		/// <summary>
		/// Gets or sets the lexical family.
		/// </summary>
		/// <value>The lexical family.</value>
		public LexFamily Family { get; internal set; }

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
				sb.Append(Stratum.SymbolDefinitionTable.ToString(allomorph.Shape, true));
				firstItem = false;
			}

			return string.Format(HCStrings.kstidLexEntry, ID, sb, Gloss == null ? "?" : Gloss.Description);
		}
	}
}
