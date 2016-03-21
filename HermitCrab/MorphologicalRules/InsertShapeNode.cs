using System;
using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.Annotations;
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
			FeatureStruct fs = _fs.DeepClone();
			fs.ReplaceVariables(match.VariableBindings);
			ShapeNode newNode = output.Shape.Add(fs);
			return Tuple.Create((ShapeNode) null, newNode).ToEnumerable();
		}

		public override string ToString()
		{
			return _fs.ToString();
		}
	}
}
