using System.Collections.Generic;
using System.Linq;
using SIL.APRE;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	public class AffixProcessSynthesisRule : IRule<Word, ShapeNode>
	{
		private readonly SpanFactory<ShapeNode> _spanFactory; 
		private readonly List<SynthesisAffixProcessRule> _rules;

		public AffixProcessSynthesisRule(SpanFactory<ShapeNode> spanFactory)
		{
			_spanFactory = spanFactory;
			_rules = new List<SynthesisAffixProcessRule>();
		}

		public void AddAllomorph(AffixProcessAllomorph allomorph)
		{
			_rules.Add(new SynthesisAffixProcessRule(_spanFactory, allomorph));
		}

		public bool IsApplicable(Word input)
		{
			throw new System.NotImplementedException();
		}

		public bool Apply(Word input, out IEnumerable<Word> output)
		{
			List<Word> outputList = null;
			foreach (SynthesisAffixProcessRule sr in _rules)
			{
				IEnumerable<Word> result;
				if (sr.Apply(input, out result))
				{
					if (outputList == null)
						outputList = new List<Word>();
					outputList.Add(result.Single());

					if (sr.Allomorph.RequiredEnvironments == null && sr.Allomorph.ExcludedEnvironments == null)
						break;
				}
			}

			output = outputList;
			return output != null;
		}
	}
}
