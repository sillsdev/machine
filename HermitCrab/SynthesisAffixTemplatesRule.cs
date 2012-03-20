using System.Collections.Generic;
using System.Linq;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	public class SynthesisAffixTemplatesRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly Stratum _stratum;
		private readonly List<IRule<Word, ShapeNode>> _templateRules; 

		public SynthesisAffixTemplatesRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, Stratum stratum)
		{
			_morpher = morpher;
			_stratum = stratum;
			_templateRules = _stratum.AffixTemplates.Select(temp => temp.CompileSynthesisRule(spanFactory, morpher)).ToList();
		}

		public bool IsApplicable(Word input)
		{
			return input.RealizationalFeatureStruct.IsUnifiable(input.SyntacticFeatureStruct);
		}

		public IEnumerable<Word> Apply(Word input)
		{
			var output = new List<Word>();
			input = ChooseInflectionalStem(input);
			bool applicableTemplate = false;
			foreach (IRule<Word, ShapeNode> rule in _templateRules)
			{
				if (rule.IsApplicable(input))
				{
					applicableTemplate = true;
					output.AddRange(rule.Apply(input));
				}
			}

			if (!applicableTemplate)
				output.Add(input);

			return output;
		}

		private Word ChooseInflectionalStem(Word input)
		{
			LexFamily family = ((LexEntry) input.RootAllomorph.Morpheme).Family;
			if (family == null || input.RealizationalFeatureStruct.IsEmpty)
				return input;

			Word best = input;
			foreach (LexEntry relative in family.Entries)
			{
				if (relative != input.RootAllomorph.Morpheme && relative.Stratum == input.Stratum
					&& input.RealizationalFeatureStruct.IsUnifiable(relative.SyntacticFeatureStruct)
					&& best.SyntacticFeatureStruct.Subsumes(relative.SyntacticFeatureStruct))
				{
					FeatureStruct remainder = relative.SyntacticFeatureStruct.DeepClone();
					remainder.Subtract(best.SyntacticFeatureStruct);
					if (!remainder.IsEmpty && input.RealizationalFeatureStruct.IsUnifiable(remainder))
					{
						best = new Word(relative.PrimaryAllomorph, input.RealizationalFeatureStruct.DeepClone()) { CurrentTrace = input.CurrentTrace };
					}
				}
			}

			if (_morpher.TraceBlocking && best != input)
				best.CurrentTrace.Children.Add(new Trace(TraceType.Blocking, _stratum) { Output = best.DeepClone() });
			return best;
		}
	}
}
