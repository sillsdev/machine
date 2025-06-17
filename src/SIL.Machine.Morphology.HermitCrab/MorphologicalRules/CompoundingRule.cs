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
            HeadProdRestrictionsMprFeatureSet = new MprFeatureSet();
            NonHeadProdRestrictionsMprFeatureSet = new MprFeatureSet();
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

        public MprFeatureSet HeadProdRestrictionsMprFeatureSet { get; set; }

        public MprFeatureSet NonHeadProdRestrictionsMprFeatureSet { get; set; }

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

        public bool CompoundMprFeaturesMatch(MprFeatureSet ruleMprFeatures, MprFeatureSet stemMprFeatures)
        {
            if (ruleMprFeatures.Count > 0)
            {
                var prodRestricts = ruleMprFeatures.Clone();
                prodRestricts.IntersectWith(stemMprFeatures);
                if (prodRestricts.Count == 0)
                    return false;
            }
            return true;
        }
    }
}
