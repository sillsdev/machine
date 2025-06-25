using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    /// <summary>
    /// This class represents a morphological rule which combines two words in to one word.
    /// </summary>
    public class CompoundingRule : HCRuleBase, IMorphologicalRule
    {
        private readonly List<CompoundingSubrule> _subrules;
        private readonly IDBearerSet<Feature> _obligatorySyntacticFeatures;

        public CompoundingRule()
        {
            MaxApplicationCount = 1;
            Blockable = true;
            HeadRequiredSyntacticFeatureStruct = FeatureStruct.New().Value;
            NonHeadRequiredSyntacticFeatureStruct = FeatureStruct.New().Value;
            HeadProdRestrictionsMprFeatures = new MprFeatureSet();
            NonHeadProdRestrictionsMprFeatures = new MprFeatureSet();
            OutputProdRestrictionsMprFeatures = new MprFeatureSet();
            OutSyntacticFeatureStruct = FeatureStruct.New().Value;

            _subrules = new List<CompoundingSubrule>();

            _obligatorySyntacticFeatures = new IDBearerSet<Feature>();
        }

        public IList<CompoundingSubrule> Subrules
        {
            get { return _subrules; }
        }

        public int MaxApplicationCount { get; set; }

        public bool Blockable { get; set; }

        public FeatureStruct HeadRequiredSyntacticFeatureStruct { get; set; }

        public FeatureStruct NonHeadRequiredSyntacticFeatureStruct { get; set; }

        public MprFeatureSet HeadProdRestrictionsMprFeatures { get; set; }

        public MprFeatureSet NonHeadProdRestrictionsMprFeatures { get; set; }

        public MprFeatureSet OutputProdRestrictionsMprFeatures { get; set; }

        public FeatureStruct OutSyntacticFeatureStruct { get; set; }

        public ICollection<Feature> ObligatorySyntacticFeatures
        {
            get { return _obligatorySyntacticFeatures; }
        }

        public Stratum Stratum { get; set; }

        public override IRule<Word, ShapeNode> CompileAnalysisRule(Morpher morpher)
        {
            return new AnalysisCompoundingRule(morpher, this);
        }

        public override IRule<Word, ShapeNode> CompileSynthesisRule(Morpher morpher)
        {
            return new SynthesisCompoundingRule(morpher, this);
        }
    }
}
