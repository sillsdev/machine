using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public class RewriteSubrule
    {
        private readonly MprFeatureSet _requiredMprFeatures;
        private readonly MprFeatureSet _excludedMprFeatures;

        public RewriteSubrule()
        {
            Rhs = Pattern<Word, int>.New().Value;
            LeftEnvironment = Pattern<Word, int>.New().Value;
            RightEnvironment = Pattern<Word, int>.New().Value;
            RequiredSyntacticFeatureStruct = FeatureStruct.New().Value;
            _requiredMprFeatures = new MprFeatureSet();
            _excludedMprFeatures = new MprFeatureSet();
        }

        public Pattern<Word, int> Rhs { get; set; }
        public Pattern<Word, int> LeftEnvironment { get; set; }
        public Pattern<Word, int> RightEnvironment { get; set; }
        public FeatureStruct RequiredSyntacticFeatureStruct { get; set; }
        public MprFeatureSet RequiredMprFeatures
        {
            get { return _requiredMprFeatures; }
        }
        public MprFeatureSet ExcludedMprFeatures
        {
            get { return _excludedMprFeatures; }
        }
    }
}
