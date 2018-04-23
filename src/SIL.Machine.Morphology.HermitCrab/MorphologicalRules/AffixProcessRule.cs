using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
	/// <summary>
	/// This class represents an affixal morphological rule. It supports many different types of affixation,
	/// such as prefixation, suffixation, infixation, circumfixation, simulfixation, reduplication,
	/// and truncation.
	/// </summary>
	public class AffixProcessRule : MorphemicMorphologicalRule
	{ 
		private readonly ObservableCollection<AffixProcessAllomorph> _allomorphs;
		private readonly IDBearerSet<Feature> _obligatorySyntacticFeatures; 

		public AffixProcessRule()
		{
			_allomorphs = new ObservableCollection<AffixProcessAllomorph>();
			_allomorphs.CollectionChanged += AllomorphsChanged;

			MaxApplicationCount = 1;
			Blockable = true;
			RequiredSyntacticFeatureStruct = FeatureStruct.New().Value;
			OutSyntacticFeatureStruct = FeatureStruct.New().Value;
			_obligatorySyntacticFeatures = new IDBearerSet<Feature>();
		}

		private void AllomorphsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (AffixProcessAllomorph allo in e.OldItems)
				{
					allo.Morpheme = null;
					allo.Index = -1;
				}
			}

			if (e.NewItems != null)
			{
				foreach (AffixProcessAllomorph allo in e.NewItems)
					allo.Morpheme = this;
			}

			int index = Math.Min(e.NewStartingIndex == -1 ? int.MaxValue : e.NewStartingIndex,
				e.OldStartingIndex == -1 ? int.MaxValue : e.OldStartingIndex);
			for (int i = index; i < _allomorphs.Count; i++)
				_allomorphs[i].Index = i;
		}

		public int MaxApplicationCount { get; set; }

		public FeatureStruct RequiredSyntacticFeatureStruct { get; set; }

		public FeatureStruct OutSyntacticFeatureStruct { get; set; }

		public StemName RequiredStemName { get; set; }

		public ICollection<Feature> ObligatorySyntacticFeatures
		{
			get { return _obligatorySyntacticFeatures; }
		}

		public bool Blockable { get; set; }

		public IList<AffixProcessAllomorph> Allomorphs
		{
			get { return _allomorphs; }
		}

		public override IRule<Word, ShapeNode> CompileAnalysisRule(Morpher morpher)
		{
			return new AnalysisAffixProcessRule(morpher, this);
		}

		public override IRule<Word, ShapeNode> CompileSynthesisRule(Morpher morpher)
		{
			return new SynthesisAffixProcessRule(morpher, this);
		}

		public override string Category
		{
			get
			{
				FeatureSymbol pos = RequiredSyntacticFeatureStruct.PartsOfSpeech().FirstOrDefault();
				return pos?.ID;
			}
		}

		public override Allomorph GetAllomorph(int index)
		{
			return _allomorphs[index];
		}

		public override string ToString()
		{
			return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
		}
	}
}