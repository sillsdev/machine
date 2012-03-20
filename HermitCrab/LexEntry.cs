using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a lexical entry.
	/// </summary>
	public class LexEntry : Morpheme
	{
		private readonly ObservableCollection<RootAllomorph> _allomorphs;

		/// <summary>
		/// Initializes a new instance of the <see cref="LexEntry"/> class.
		/// </summary>
		/// <param name="id">The ID.</param>
		public LexEntry(string id)
			: base(id)
		{
			MprFeatures = new MprFeatureSet();
			SyntacticFeatureStruct = FeatureStruct.New().Value;
			_allomorphs = new ObservableCollection<RootAllomorph>();
			_allomorphs.CollectionChanged += AllomorphsChanged;
		}

		private void AllomorphsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (RootAllomorph allo in e.OldItems)
				{
					allo.Morpheme = null;
					allo.Index = -1;
				}
			}

			if (e.NewItems != null)
			{
				foreach (RootAllomorph allo in e.NewItems)
					allo.Morpheme = this;
			}

			int index = Math.Min(e.NewStartingIndex == -1 ? int.MaxValue : e.NewStartingIndex, e.OldStartingIndex == -1 ? int.MaxValue : e.OldStartingIndex);
			for (int i = index; i < _allomorphs.Count; i++)
				_allomorphs[i].Index = i;
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
		public IList<RootAllomorph> Allomorphs
		{
			get { return _allomorphs; }
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
		public FeatureStruct SyntacticFeatureStruct { get; set; }

		/// <summary>
		/// Gets or sets the lexical family.
		/// </summary>
		/// <value>The lexical family.</value>
		public LexFamily Family { get; internal set; }

		public override string ToString()
		{
			var sb = new StringBuilder();
			bool firstItem = true;
			foreach (RootAllomorph allomorph in _allomorphs)
			{
				if (!firstItem)
					sb.Append(", ");
				sb.Append(Stratum.SymbolTable.ToString(allomorph.Shape, true));
				firstItem = false;
			}

			return string.Format(HCStrings.kstidLexEntry, ID, sb, string.IsNullOrEmpty(Gloss) ? "?" : Gloss);
		}
	}
}
