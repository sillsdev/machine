using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	internal class AnalysisAffixTemplateRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly AffixTemplate _template;
		private readonly List<IRule<Word, ShapeNode>> _rules; 

		public AnalysisAffixTemplateRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, AffixTemplate template)
		{
			_morpher = morpher;
			_template = template;
			_rules = new List<IRule<Word, ShapeNode>>(template.Slots
				.Select(slot => new RuleBatch<Word, ShapeNode>(slot.Rules.Select(mr => mr.CompileAnalysisRule(spanFactory, morpher)), false, ValueEqualityComparer<Word>.Instance)));
		}

		public IEnumerable<Word> Apply(Word input)
		{
			FeatureStruct fs;
			if (!input.SyntacticFeatureStruct.Unify(_template.RequiredSyntacticFeatureStruct, out fs))
				return Enumerable.Empty<Word>();

			if (_morpher.TraceRules.Contains(_template))
				input.CurrentTrace.Children.Add(new Trace(TraceType.TemplateAnalysisInput, _template) {Input = input});

			var output = new HashSet<Word>(ValueEqualityComparer<Word>.Instance);
			ApplySlots(input, _rules.Count - 1, output);
			foreach (Word outWord in output)
				outWord.SyntacticFeatureStruct = fs;
			return output;
		}

		private void ApplySlots(Word input, int index, HashSet<Word> output)
		{
			for (int i = index; i >= 0; i--)
			{
				foreach (Word outWord in _rules[i].Apply(input))
					ApplySlots(outWord, index - 1, output);

				if (!_template.Slots[i].Optional)
				{
					if (_morpher.TraceRules.Contains(_template))
						input.CurrentTrace.Children.Add(new Trace(TraceType.TemplateAnalysisOutput, _template));
					return;
				}
			}

			if (_morpher.TraceRules.Contains(_template))
				input.CurrentTrace.Children.Add(new Trace(TraceType.TemplateAnalysisOutput, _template) {Output = input});
			output.Add(input);
		}
	}
}
