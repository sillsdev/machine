using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	/// <summary>
	/// This class represents an affixal morphological rule. It supports many different types of affixation,
	/// such as prefixation, suffixation, infixation, circumfixation, simulfixation, reduplication,
	/// and truncation.
	/// </summary>
	public class AffixProcessRule : Morpheme, IMorphologicalRule
	{ 
		private readonly ObservableCollection<AffixProcessAllomorph> _allomorphs;
		private readonly IDBearerSet<Feature> _obligatorySyntacticFeatures; 

		public AffixProcessRule(string id)
			: base(id)
		{
			_allomorphs = new ObservableCollection<AffixProcessAllomorph>();
			_allomorphs.CollectionChanged += AllomorphsChanged;

			MaxApplicationCount = 1;
			Blockable = true;
			RequiredSyntacticFeatureStruct = new FeatureStruct();
			OutSyntacticFeatureStruct = new FeatureStruct();
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

			int index = Math.Min(e.NewStartingIndex == -1 ? int.MaxValue : e.NewStartingIndex, e.OldStartingIndex == -1 ? int.MaxValue : e.OldStartingIndex);
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

		public IRule<Word, ShapeNode> CompileAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
		{
			return new AnalysisAffixProcessRule(spanFactory, morpher, this);
		}

		public IRule<Word, ShapeNode> CompileSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
		{
			return new SynthesisAffixProcessRule(spanFactory, morpher, this);
		}

		public void Traverse(Action<IHCRule> action)
		{
			action(this);
		}

		public override Allomorph GetAllomorph(int index)
		{
			return _allomorphs[index];
		}
	}
}