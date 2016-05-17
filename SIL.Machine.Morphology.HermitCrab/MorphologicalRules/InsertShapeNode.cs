using System;
using System.Collections.Generic;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
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
			analysisLhs.Children.Add(new Constraint<Word, ShapeNode>(_fs.Clone()));
		}

		public override IEnumerable<Tuple<ShapeNode, ShapeNode>> Apply(Match<Word, ShapeNode> match, Word output)
		{
			FeatureStruct fs = _fs.Clone();
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
