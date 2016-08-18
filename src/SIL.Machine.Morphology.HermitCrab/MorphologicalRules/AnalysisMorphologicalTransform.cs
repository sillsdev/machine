using System;
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
		private readonly Dictionary<string, Tuple<int, FeatureStruct>> _modifyFromInfos;
		private readonly Dictionary<string, int> _capturedParts;

		public AnalysisMorphologicalTransform(IEnumerable<Pattern<Word, ShapeNode>> lhs, IList<MorphologicalOutputAction> rhs)
		{
			Dictionary<string, Pattern<Word, ShapeNode>> partLookup = lhs.ToDictionary(p => p.Name);
			_modifyFromInfos = new Dictionary<string, Tuple<int, FeatureStruct>>();
			_pattern = new Pattern<Word, ShapeNode>();
			_capturedParts = new Dictionary<string, int>();
			foreach (MorphologicalOutputAction outputAction in rhs)
			{
				outputAction.GenerateAnalysisLhs(_pattern, partLookup, _capturedParts);

				var modifyFromInput = outputAction as ModifyFromInput;
				if (modifyFromInput != null)
					_modifyFromInfos[modifyFromInput.PartName] = Tuple.Create(_capturedParts[modifyFromInput.PartName] - 1, modifyFromInput.SimpleContext.FeatureStruct.AntiFeatureStruct());
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
				Tuple<int, FeatureStruct> modifyFromInfo;
				if (_modifyFromInfos.TryGetValue(part.Name, out modifyFromInfo))
				{
					if (AddCapturedPartNodes(part.Name, modifyFromInfo.Item1, match, modifyFromInfo.Item2, output))
						return;
				}

				for (int i = 0; i < count; i++)
				{
					if (AddCapturedPartNodes(part.Name, i, match, null, output))
						return;
				}
			}

			Untruncate(part, output, false, match.VariableBindings);
		}

		private bool AddCapturedPartNodes(string partName, int index, Match<Word, ShapeNode> match, FeatureStruct modifyFromFS, Shape output)
		{
			GroupCapture<ShapeNode> inputGroup = match.GroupCaptures[GetGroupName(partName, index)];
			if (inputGroup.Success)
			{
				Span<ShapeNode> outputSpan = match.Input.Shape.CopyTo(inputGroup.Span, output);
				if (modifyFromFS != null)
				{
					foreach (ShapeNode node in output.GetNodes(outputSpan))
					{
						if ((FeatureSymbol) modifyFromFS.GetValue(HCFeatureSystem.Type) == node.Annotation.Type())
							node.Annotation.FeatureStruct.Add(modifyFromFS, match.VariableBindings);
					}
				}
				return true;
			}
			return false;
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
