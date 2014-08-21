using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	/// <summary>
	/// This class represents a morphological rule which combines two words in to one word.
	/// </summary>
	public class CompoundingRule : IMorphologicalRule
	{
		private readonly List<CompoundingSubrule> _subrules;
		private readonly IDBearerSet<Feature> _obligatorySyntacticFeatures; 

		public CompoundingRule()
		{
			MaxApplicationCount = 1;
			Blockable = true;
			HeadRequiredSyntacticFeatureStruct = FeatureStruct.New().Value;
			NonHeadRequiredSyntacticFeatureStruct = FeatureStruct.New().Value;
			OutSyntacticFeatureStruct = FeatureStruct.New().Value;

			_subrules = new List<CompoundingSubrule>();

			_obligatorySyntacticFeatures = new IDBearerSet<Feature>();
		}

		public string Name { get; set; }

		public IList<CompoundingSubrule> Subrules
		{
			get { return _subrules; }
		}

		public int MaxApplicationCount { get; set; }

		public bool Blockable { get; set; }

		public FeatureStruct HeadRequiredSyntacticFeatureStruct { get; set; }

		public FeatureStruct NonHeadRequiredSyntacticFeatureStruct { get; set; }

		public FeatureStruct OutSyntacticFeatureStruct { get; set; }

		public ICollection<Feature> ObligatorySyntacticFeatures
		{
			get { return _obligatorySyntacticFeatures; }
		}

		public Stratum Stratum { get; set; }

		public IRule<Word, ShapeNode> CompileAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
		{
			return new AnalysisCompoundingRule(spanFactory, morpher, this);
		}

		public IRule<Word, ShapeNode> CompileSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
		{
			return new SynthesisCompoundingRule(spanFactory, morpher, this);
		}

		public override string ToString()
		{
			return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
		}
	}
}