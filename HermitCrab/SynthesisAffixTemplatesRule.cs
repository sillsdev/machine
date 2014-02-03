using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	internal class SynthesisAffixTemplatesRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly Stratum _stratum;
		private readonly List<AffixTemplate> _templates; 
		private readonly List<IRule<Word, ShapeNode>> _templateRules; 

		public SynthesisAffixTemplatesRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, Stratum stratum)
		{
			_morpher = morpher;
			_stratum = stratum;
			_templates = stratum.AffixTemplates.ToList();
			_templateRules = _templates.Select(temp => temp.CompileSynthesisRule(spanFactory, morpher)).ToList();
		}

		public IEnumerable<Word> Apply(Word input)
		{
			if (!input.RealizationalFeatureStruct.IsUnifiable(input.SyntacticFeatureStruct))
				return Enumerable.Empty<Word>();

			var output = new HashSet<Word>(FreezableEqualityComparer<Word>.Default);
			bool applicableTemplate = false;
			input = ChooseInflectionalStem(input);
			for (int i = 0; i < _templateRules.Count; i++)
			{
				if (input.SyntacticFeatureStruct.IsUnifiable(_templates[i].RequiredSyntacticFeatureStruct))
				{
					applicableTemplate = true;
					output.UnionWith(_templateRules[i].Apply(input));
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
						best = new Word(relative.PrimaryAllomorph, input.RealizationalFeatureStruct.DeepClone()) {CurrentTrace = input.CurrentTrace};
						best.Freeze();
					}
				}
			}

			if (_morpher.TraceBlocking && best != input)
				best.CurrentTrace.Children.Add(new Trace(TraceType.Blocking, _stratum) {Output = best});
			return best;
		}
	}
}
