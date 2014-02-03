using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class RealizationalAffixProcessRule : Morpheme, IMorphologicalRule
	{
		private readonly ObservableCollection<AffixProcessAllomorph> _allomorphs;

		public RealizationalAffixProcessRule(string id)
			: base(id)
		{
			Blockable = true;
			_allomorphs = new ObservableCollection<AffixProcessAllomorph>();
			_allomorphs.CollectionChanged += AllomorphsChanged;
			RequiredSyntacticFeatureStruct = FeatureStruct.New().Value;
			RealizationalFeatureStruct = FeatureStruct.New().Value;
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

		public bool Blockable { get; set; }

		public FeatureStruct RequiredSyntacticFeatureStruct { get; set; }

		public FeatureStruct RealizationalFeatureStruct { get; set; }

		public IList<AffixProcessAllomorph> Allomorphs
		{
			get { return _allomorphs; }
		}

		public IRule<Word, ShapeNode> CompileAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
		{
			return new AnalysisRealizationalAffixProcessRule(spanFactory, morpher, this);
		}

		public IRule<Word, ShapeNode> CompileSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
		{
			return new SynthesisRealizationalAffixProcessRule(spanFactory, morpher, this);
		}

		public void Traverse(Action<IHCRule> action)
		{
			action(this);
		}
	}
}
