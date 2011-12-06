using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class SynthesisAffixPatternRule : PatternRule<Word, ShapeNode>
	{
		private readonly AffixProcessAllomorph _allomorph;

		public SynthesisAffixPatternRule(SpanFactory<ShapeNode> spanFactory, AffixProcessAllomorph allomorph)
			: base(new Pattern<Word, ShapeNode>(spanFactory) {UseDefaultsForMatching = true,
				Filter = ann => ann.Type.IsOneOf(HCFeatureSystem.SegmentType, HCFeatureSystem.BoundaryType, HCFeatureSystem.AnchorType)})
		{
			_allomorph = allomorph;

			Lhs.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.AnchorType,
				FeatureStruct.New(HCFeatureSystem.Instance).Symbol(HCFeatureSystem.LeftSide).Value));
			for (int i = 0; i < _allomorph.Lhs.Count; i++)
				Lhs.Children.Add(new Group<Word, ShapeNode>(i.ToString(), _allomorph.Lhs[i].Children.Clone()));
			Lhs.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.AnchorType,
				FeatureStruct.New(HCFeatureSystem.Instance).Symbol(HCFeatureSystem.RightSide).Value));
		}

		public AffixProcessAllomorph Allomorph
		{
			get { return _allomorph; }
		}

		public override bool IsApplicable(Word input)
		{
			return true;
		}

		public override Annotation<ShapeNode> ApplyRhs(Word input, PatternMatch<ShapeNode> match, out Word output)
		{
			output = input.Clone();
			output.Shape.Clear();
			foreach (MorphologicalOutput outputAction in _allomorph.Rhs)
				outputAction.Apply(match, input, output, _allomorph);

			return null;
		}
	}
}
