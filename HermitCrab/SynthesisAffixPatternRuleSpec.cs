using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class SynthesisAffixPatternRuleSpec : IPatternRuleSpec<Word, ShapeNode>
	{
		private readonly AffixProcessAllomorph _allomorph;
		private readonly Pattern<Word, ShapeNode> _pattern; 

		public SynthesisAffixPatternRuleSpec(AffixProcessAllomorph allomorph)
		{
			_allomorph = allomorph;

			_pattern = new Pattern<Word, ShapeNode>();
			_pattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.AnchorType,
				FeatureStruct.New().Symbol(HCFeatureSystem.LeftSide).Value));
			for (int i = 0; i < _allomorph.Lhs.Count; i++)
				_pattern.Children.Add(new Group<Word, ShapeNode>(i.ToString(), _allomorph.Lhs[i].Children.Clone()));
			_pattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.AnchorType,
				FeatureStruct.New().Symbol(HCFeatureSystem.RightSide).Value));
		}

		public AffixProcessAllomorph Allomorph
		{
			get { return _allomorph; }
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
			foreach (MorphologicalOutput outputAction in _allomorph.Rhs)
				outputAction.Apply(match, output, _allomorph);

			return null;
		}
	}
}
