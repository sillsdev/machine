using System.Collections.Generic;
using System.Globalization;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class AnalysisAffixPatternRuleSpec : IPatternRuleSpec<Word, ShapeNode>
	{
		private readonly Pattern<Word, ShapeNode> _pattern; 
		private readonly AffixProcessAllomorph _allomorph;
		private readonly Dictionary<int, Constraint<Word, ShapeNode>> _modifyFromConstraints;

		public AnalysisAffixPatternRuleSpec(AffixProcessAllomorph allomorph)
		{
			_allomorph = allomorph;
			_modifyFromConstraints = new Dictionary<int, Constraint<Word, ShapeNode>>();
			_pattern = new Pattern<Word, ShapeNode>();
			_pattern.Children.Add(new Constraint<Word, ShapeNode>(FeatureStruct.New().Symbol(HCFeatureSystem.AnchorType).Symbol(HCFeatureSystem.LeftSide).Value));
			foreach (MorphologicalOutput outputAction in _allomorph.Rhs)
			{
				outputAction.GenerateAnalysisLhs(_pattern, _allomorph.Lhs);

				var modifyFrom = outputAction as ModifyFromInput;
				if (modifyFrom != null)
				{
					FeatureStruct fs = modifyFrom.Constraint.FeatureStruct.AntiFeatureStruct();
					fs.AddValue(HCFeatureSystem.Type, modifyFrom.Constraint.Type());
					_modifyFromConstraints[modifyFrom.Index] = new Constraint<Word, ShapeNode>(fs);
				}
			}
			_pattern.Children.Add(new Constraint<Word, ShapeNode>(FeatureStruct.New().Symbol(HCFeatureSystem.AnchorType).Symbol(HCFeatureSystem.RightSide).Value));
		}

		public Pattern<Word, ShapeNode> Pattern
		{
			get { return _pattern; }
		}

		public bool IsApplicable(Word input)
		{
			return true;
		}

		public ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
		{
			output = match.Input.Clone();
			output.Shape.Clear();
			for (int i = 0; i < _allomorph.Lhs.Count; i++)
			{
				GroupCapture<ShapeNode> inputGroup = match[i.ToString(CultureInfo.InvariantCulture)];
				if (inputGroup.Success)
				{
					Constraint<Word, ShapeNode> constraint;
					if (!_modifyFromConstraints.TryGetValue(i, out constraint))
						constraint = null;
					Span<ShapeNode> outputSpan = match.Input.Shape.CopyTo(inputGroup.Span, output.Shape);
					if (constraint != null)
					{
						foreach (ShapeNode node in output.Shape.GetNodes(outputSpan))
						{
							if (constraint.Type() == node.Annotation.Type())
								node.Annotation.FeatureStruct.Union(constraint.FeatureStruct, match.VariableBindings);
						}
					}
				}
				else
				{
					Untruncate(_allomorph.Lhs[i], output, false, match.VariableBindings);
				}
			}

			return null;
		}

		private void Untruncate(PatternNode<Word, ShapeNode> patternNode, Word output, bool optional, VariableBindings varBindings)
		{
			foreach (PatternNode<Word, ShapeNode> node in patternNode.Children)
			{
				var constraint = node as Constraint<Word, ShapeNode>;
				if (constraint != null && constraint.Type() == HCFeatureSystem.SegmentType)
				{
					FeatureStruct fs = constraint.FeatureStruct.Clone();
					fs.ReplaceVariables(varBindings);
					output.Shape.Add(fs, optional);
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
