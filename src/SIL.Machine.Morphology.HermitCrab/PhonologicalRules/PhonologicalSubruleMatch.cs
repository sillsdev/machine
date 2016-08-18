using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
	public class PhonologicalSubruleMatch
	{
		private readonly IPhonologicalPatternSubruleSpec _subruleSpec;
		private readonly Span<ShapeNode> _span;
		private readonly VariableBindings _varBindings;

		public PhonologicalSubruleMatch(IPhonologicalPatternSubruleSpec subruleSpec, Span<ShapeNode> span, VariableBindings varBindings)
		{
			_subruleSpec = subruleSpec;
			_span = span;
			_varBindings = varBindings;
		}

		public IPhonologicalPatternSubruleSpec SubruleSpec
		{
			get { return _subruleSpec; }
		}

		public Span<ShapeNode> Span
		{
			get { return _span; }
		}

		public VariableBindings VariableBindings
		{
			get { return _varBindings; }
		}
	}
}
