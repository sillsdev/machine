using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	public class SynthesisAffixProcessRule : PatternRuleBase<Word, ShapeNode>
	{
		private readonly AffixProcessAllomorph _allomorph;

		public SynthesisAffixProcessRule(SpanFactory<ShapeNode> spanFactory, AffixProcessAllomorph allomorph)
			: base(new Pattern<Word, ShapeNode>(spanFactory, Direction.LeftToRight,
				ann => ann.Type.IsOneOf(HCFeatureSystem.SegmentType, HCFeatureSystem.BoundaryType, HCFeatureSystem.AnchorType)),
			ApplicationMode.Single)
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
			output = new Word(input.Stratum, input.Mode);
			foreach (MorphologicalOutput outputAction in _allomorph.Rhs)
				outputAction.Apply(match, input, output, _allomorph);

			return null;
		}
	}
}
