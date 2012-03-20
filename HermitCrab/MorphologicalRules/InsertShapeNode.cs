using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class InsertShapeNode : MorphologicalOutputAction
	{
		private readonly FeatureStruct _fs; 

		public InsertShapeNode(FeatureStruct fs)
			: base(null)
		{
			_fs = fs;
		}

		public override void GenerateAnalysisLhs(Pattern<Word, ShapeNode> analysisLhs, IDictionary<string, Pattern<Word, ShapeNode>> partLookup)
		{
			analysisLhs.Children.Add(new Constraint<Word, ShapeNode>(_fs.DeepClone()));
		}

		public override IEnumerable<Tuple<ShapeNode, ShapeNode>> Apply(Match<Word, ShapeNode> match, Word output)
		{
			if (match.VariableBindings.Values.OfType<SymbolicFeatureValue>().Any(value => value.Feature.DefaultValue.Equals(value)))
				throw new MorphException(MorphErrorCode.UninstantiatedFeature);
			FeatureStruct fs = _fs.DeepClone();
			fs.ReplaceVariables(match.VariableBindings);
			ShapeNode newNode = output.Shape.Add(fs);
			return Tuple.Create((ShapeNode) null, newNode).ToEnumerable();
		}
	}
}
