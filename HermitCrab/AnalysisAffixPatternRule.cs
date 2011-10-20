using System.Collections.Generic;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	public class AnalysisAffixPatternRule : PatternRuleBase<Word, ShapeNode>
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly AffixProcessAllomorph _allomorph;
		private readonly Dictionary<int, Constraint<Word, ShapeNode>> _modifyFromConstraints;

		public AnalysisAffixPatternRule(SpanFactory<ShapeNode> spanFactory, AffixProcessAllomorph allomorph)
			: base(new Pattern<Word, ShapeNode>(spanFactory,
				Direction.LeftToRight, ann => ann.Type.IsOneOf(HCFeatureSystem.SegmentType, HCFeatureSystem.AnchorType)),
			ApplicationMode.Multiple)
		{
			_spanFactory = spanFactory;
			_allomorph = allomorph;
			_modifyFromConstraints = new Dictionary<int, Constraint<Word, ShapeNode>>();

			Lhs.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.AnchorType,
				FeatureStruct.New(HCFeatureSystem.Instance).Symbol(HCFeatureSystem.LeftSide).Value));
			foreach (MorphologicalOutput outputAction in _allomorph.Rhs)
			{
				outputAction.GenerateAnalysisLhs(Lhs, _allomorph.Lhs);

				var modifyFrom = outputAction as ModifyFromInput;
				if (modifyFrom != null)
				{
					_modifyFromConstraints[modifyFrom.Index] = new Constraint<Word, ShapeNode>(modifyFrom.Constraint.Type,
						GetAntiFeatureStruct(modifyFrom.Constraint.FeatureStruct));
				}
			}
			Lhs.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.AnchorType,
				FeatureStruct.New(HCFeatureSystem.Instance).Symbol(HCFeatureSystem.RightSide).Value));
		}

		private static FeatureStruct GetAntiFeatureStruct(FeatureStruct fs)
		{
			var result = new FeatureStruct();
			foreach (Feature feature in fs.Features)
			{
				FeatureValue value = fs.GetValue(feature);
				var childFS = value as FeatureStruct;
				FeatureValue newValue;
				if (childFS != null)
				{
					newValue = GetAntiFeatureStruct(childFS);
				}
				else
				{
					value.Negation(out newValue);
				}
				result.AddValue(feature, newValue);
			}
			return result;
		}

		public override bool IsApplicable(Word input)
		{
			return true;
		}

		public override Annotation<ShapeNode> ApplyRhs(Word input, PatternMatch<ShapeNode> match, out Word output)
		{
			output = new Word(input.Stratum, input.Mode);
			for (int i = 0; i < _allomorph.Lhs.Count; i++)
			{
				Span<ShapeNode> inputSpan;
				if (match.TryGetGroup(i.ToString(), out inputSpan))
				{
					Constraint<Word, ShapeNode> constraint;
					if (!_modifyFromConstraints.TryGetValue(i, out constraint))
						constraint = null;
					Span<ShapeNode> outputSpan = input.Shape.CopyTo(inputSpan, output.Shape);
					if (constraint != null)
					{
						foreach (ShapeNode node in output.Shape.GetNodes(outputSpan))
						{
							if (constraint.Type == node.Annotation.Type)
								node.Annotation.FeatureStruct.Merge(constraint.FeatureStruct, match.VariableBindings);
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
				if (constraint != null && constraint.Type == HCFeatureSystem.SegmentType)
				{
					var newNode = new ShapeNode(constraint.Type, _spanFactory, (FeatureStruct) constraint.FeatureStruct.Clone());
					newNode.Annotation.FeatureStruct.ReplaceVariables(varBindings);
					newNode.Annotation.Optional = optional;
					output.Shape.Add(newNode);
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
