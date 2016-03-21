using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	internal class SynthesisAffixTemplateRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly AffixTemplate _template;
		private readonly List<IRule<Word, ShapeNode>> _rules; 

		public SynthesisAffixTemplateRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, AffixTemplate template)
		{
			_morpher = morpher;
			_template = template;
			_rules = new List<IRule<Word, ShapeNode>>(template.Slots
				.Select(slot => new RuleBatch<Word, ShapeNode>(slot.Rules.Select(mr => mr.CompileSynthesisRule(spanFactory, morpher)), false, FreezableEqualityComparer<Word>.Default)));
		}

		public IEnumerable<Word> Apply(Word input)
		{
			if (_morpher.TraceManager.IsTracing)
				_morpher.TraceManager.BeginApplyTemplate(_template, input);
			var output = new HashSet<Word>(FreezableEqualityComparer<Word>.Default);
			ApplySlots(input, 0, output);
			return output;
		}

		private void ApplySlots(Word input, int index, HashSet<Word> output)
		{
			for (int i = index; i < _rules.Count; i++)
			{
				foreach (Word outWord in _rules[i].Apply(input))
					ApplySlots(outWord, i + 1, output);

				if (!_template.Slots[i].Optional)
				{
					if (_morpher.TraceManager.IsTracing)
						_morpher.TraceManager.EndApplyTemplate(_template, input, false);
					return;
				}
			}

			if (_morpher.TraceManager.IsTracing)
				_morpher.TraceManager.EndApplyTemplate(_template, input, true);
			output.Add(input);
		}
	}
}
