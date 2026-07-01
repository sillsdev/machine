using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public class CompoundingSubrule
    {
        private readonly List<Pattern<Word, int>> _headLhs;
        private readonly List<Pattern<Word, int>> _nonHeadLhs;
        private readonly List<MorphologicalOutputAction> _rhs;

        private readonly MprFeatureSet _requiredMprFeatures;
        private readonly MprFeatureSet _excludedMprFeatures;
        private readonly MprFeatureSet _outMprFeatures;

        public CompoundingSubrule()
        {
            _headLhs = new List<Pattern<Word, int>>();
            _nonHeadLhs = new List<Pattern<Word, int>>();
            _rhs = new List<MorphologicalOutputAction>();

            _requiredMprFeatures = new MprFeatureSet();
            _excludedMprFeatures = new MprFeatureSet();
            _outMprFeatures = new MprFeatureSet();
        }

        public IList<Pattern<Word, int>> HeadLhs
        {
            get { return _headLhs; }
        }

        public IList<Pattern<Word, int>> NonHeadLhs
        {
            get { return _nonHeadLhs; }
        }

        public IList<MorphologicalOutputAction> Rhs
        {
            get { return _rhs; }
        }

        public MprFeatureSet RequiredMprFeatures
        {
            get { return _requiredMprFeatures; }
        }

        public MprFeatureSet ExcludedMprFeatures
        {
            get { return _excludedMprFeatures; }
        }

        public MprFeatureSet OutMprFeatures
        {
            get { return _outMprFeatures; }
        }
    }
}
