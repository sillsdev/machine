using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public class CompoundingSubrule
    {
        private readonly List<Pattern<Word, ShapeNode>> _headLhs;
        private readonly List<Pattern<Word, ShapeNode>> _nonHeadLhs;
        private readonly List<MorphologicalOutputAction> _rhs;

        private readonly MprFeatureSet _requiredMprFeatures;
        private readonly MprFeatureSet _excludedMprFeatures;
        private readonly MprFeatureSet _outMprFeatures;

        public CompoundingSubrule()
        {
            _headLhs = new List<Pattern<Word, ShapeNode>>();
            _nonHeadLhs = new List<Pattern<Word, ShapeNode>>();
            _rhs = new List<MorphologicalOutputAction>();

            _requiredMprFeatures = new MprFeatureSet();
            _excludedMprFeatures = new MprFeatureSet();
            _outMprFeatures = new MprFeatureSet();
        }

        public IList<Pattern<Word, ShapeNode>> HeadLhs
        {
            get { return _headLhs; }
        }

        public IList<Pattern<Word, ShapeNode>> NonHeadLhs
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
