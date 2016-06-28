using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class AnalysisMorphologicalTransform
	{
		private readonly Pattern<Word, ShapeNode> _pattern;
		private readonly Dictionary<string, FeatureStruct> _modifyFrom;
		private readonly Dictionary<string, int> _capturedParts;

		public AnalysisMorphologicalTransform(IEnumerable<Pattern<Word, ShapeNode>> lhs, IList<MorphologicalOutputAction> rhs)
		{
			Dictionary<string, Pattern<Word, ShapeNode>> partLookup = lhs.ToDictionary(p => p.Name);
			_modifyFrom = new Dictionary<string, FeatureStruct>();
			_pattern = new Pattern<Word, ShapeNode>();
			_capturedParts = new Dictionary<string, int>();
			foreach (MorphologicalOutputAction outputAction in rhs)
			{
				outputAction.GenerateAnalysisLhs(_pattern, partLookup, _capturedParts);

				var modifyFromInput = outputAction as ModifyFromInput;
				if (modifyFromInput != null)
					_modifyFrom[modifyFromInput.PartName] = modifyFromInput.FeatureStruct.AntiFeatureStruct();
			}
		}

		internal static string GetGroupName(string partName, int index)
		{
			return string.Format("{0}_{1}", partName, index);
		}

		protected IDictionary<string, int> CapturedParts
		{
			get { return _capturedParts; }
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
			int count;
			if (_capturedParts.TryGetValue(part.Name, out count))
			{
				for (int i = 0; i < count; i++)
				{
					GroupCapture<ShapeNode> inputGroup = match.GroupCaptures[GetGroupName(part.Name, i)];
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
									node.Annotation.FeatureStruct.Add(modifyFromFS, match.VariableBindings);
							}
						}
						return;
					}
				}
			}

			Untruncate(part, output, false, match.VariableBindings);
		}

		private void Untruncate(PatternNode<Word, ShapeNode> patternNode, Shape output, bool optional, VariableBindings varBindings)
		{
			foreach (PatternNode<Word, ShapeNode> node in patternNode.Children)
			{
				var constraint = node as Constraint<Word, ShapeNode>;
				if (constraint != null && constraint.Type() == HCFeatureSystem.Segment)
				{
					FeatureStruct fs = constraint.FeatureStruct.DeepClone();
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
