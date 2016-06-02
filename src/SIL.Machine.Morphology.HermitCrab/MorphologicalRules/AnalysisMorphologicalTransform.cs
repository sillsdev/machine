using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
	public class AnalysisMorphologicalTransform
	{
		private readonly Pattern<Word, ShapeNode> _pattern;
		private readonly Dictionary<string, FeatureStruct> _modifyFrom;

		public AnalysisMorphologicalTransform(IEnumerable<Pattern<Word, ShapeNode>> lhs, IList<MorphologicalOutputAction> rhs)
		{
			Dictionary<string, Pattern<Word, ShapeNode>> partLookup = lhs.ToDictionary(p => p.Name);
			_modifyFrom = new Dictionary<string, FeatureStruct>();
			_pattern = new Pattern<Word, ShapeNode>();
			foreach (MorphologicalOutputAction outputAction in rhs)
			{
				outputAction.GenerateAnalysisLhs(_pattern, partLookup);

				var modifyFromInput = outputAction as ModifyFromInput;
				if (modifyFromInput != null)
					_modifyFrom[modifyFromInput.PartName] = modifyFromInput.FeatureStruct.AntiFeatureStruct();
			}
		}

		public Pattern<Word, ShapeNode> Pattern
		{
			get { return _pattern; }
		}

		public void GenerateShape(IList<Pattern<Word, ShapeNode>> lhs, Shape shape, Match<Word, ShapeNode> match)
		{
			shape.Clear();
			foreach (Pattern<Word, ShapeNode> part in lhs)
				AddPartNodes(part, match, shape);
		}

		private void AddPartNodes(Pattern<Word, ShapeNode> part, Match<Word, ShapeNode> match, Shape output)
		{
			GroupCapture<ShapeNode> inputGroup = match.GroupCaptures[part.Name];
			if (inputGroup.Success)
			{
				FeatureStruct modifyFromFS;
				if (!_modifyFrom.TryGetValue(part.Name, out modifyFromFS))
					modifyFromFS = null;
				Span<ShapeNode> outputSpan = match.Input.Shape.CopyTo(inputGroup.Span, output);
				if (modifyFromFS != null)
				{
					foreach (ShapeNode node in output.GetNodes(outputSpan))
					{
						if ((FeatureSymbol) modifyFromFS.GetValue(HCFeatureSystem.Type) == node.Annotation.Type())
						{
							FeatureStruct fs = node.Annotation.FeatureStruct.Clone();
							fs.PriorityUnion(modifyFromFS);
							node.Annotation.FeatureStruct.Union(fs, match.VariableBindings);
						}
					}
				}
			}
			else
			{
				Untruncate(part, output, false, match.VariableBindings);
			}
		}

		private void Untruncate(PatternNode<Word, ShapeNode> patternNode, Shape output, bool optional, VariableBindings varBindings)
		{
			foreach (PatternNode<Word, ShapeNode> node in patternNode.Children)
			{
				var constraint = node as Constraint<Word, ShapeNode>;
				if (constraint != null && constraint.Type() == HCFeatureSystem.Segment)
				{
					FeatureStruct fs = constraint.FeatureStruct.Clone();
					fs.ReplaceVariables(varBindings);
					output.Add(fs, optional);
				}
				else
				{
					var quantifier = node as Quantifier<Word, ShapeNode>;
					if (quantifier != null)
					{
						for (int i = 0; i < quantifier.MaxOccur; i++)
							Untruncate(quantifier, output, i >= quantifier.MinOccur, varBindings);
					}
					else
					{
						Untruncate(node, output, optional, varBindings);
					}
				}
			}
		}
	}
}
