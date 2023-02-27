using System;
using System.Collections.Generic;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public class InsertSimpleContext : MorphologicalOutputAction
    {
        private readonly SimpleContext _simpleCtxt;

        internal InsertSimpleContext(FeatureStruct fs, params SymbolicFeatureValue[] variables)
            : this(new SimpleContext(new NaturalClass(fs), variables)) { }

        public InsertSimpleContext(NaturalClass nc, IEnumerable<SymbolicFeatureValue> variables)
            : this(new SimpleContext(nc, variables)) { }

        public InsertSimpleContext(SimpleContext simpleCtxt)
            : base(null)
        {
            _simpleCtxt = simpleCtxt;
        }

        public SimpleContext SimpleContext
        {
            get { return _simpleCtxt; }
        }

        public override void GenerateAnalysisLhs(
            Pattern<Word, ShapeNode> analysisLhs,
            IDictionary<string, Pattern<Word, ShapeNode>> partLookup,
            IDictionary<string, int> capturedParts
        )
        {
            analysisLhs.Children.Add(new Constraint<Word, ShapeNode>(_simpleCtxt.FeatureStruct.Clone()));
        }

        public override IEnumerable<Tuple<ShapeNode, ShapeNode>> Apply(Match<Word, ShapeNode> match, Word output)
        {
            FeatureStruct fs = _simpleCtxt.FeatureStruct.Clone();
            fs.ReplaceVariables(match.VariableBindings);
            ShapeNode newNode = output.Shape.Add(fs);
            return Tuple.Create((ShapeNode)null, newNode).ToEnumerable();
        }

        public override string ToString()
        {
            return _simpleCtxt.FeatureStruct.ToString();
        }
    }
}
