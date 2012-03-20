using System.Collections.Generic;
using System.Linq;
using SIL.Machine;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	public class SynthesisAffixTemplateRule : RuleCascade<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly AffixTemplate _template;

		public SynthesisAffixTemplateRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, AffixTemplate template)
			: base(CreateRules(spanFactory, morpher, template), RuleCascadeOrder.Permutation)
		{
			_morpher = morpher;
			_template = template;
		}

		private static IEnumerable<IRule<Word, ShapeNode>> CreateRules(SpanFactory<ShapeNode> spanFactory, Morpher morpher, AffixTemplate template)
		{
			foreach (AffixTemplateSlot slot in template.Slots)
				yield return new RuleCascade<Word, ShapeNode>(slot.Rules.Select(mr => mr.CompileSynthesisRule(spanFactory, morpher)), RuleCascadeOrder.Permutation);
		}

		public override bool IsApplicable(Word input)
		{
			return input.SyntacticFeatureStruct.IsUnifiable(_template.RequiredSyntacticFeatureStruct);
		}

		public override IEnumerable<Word> Apply(Word input)
		{
			if (_morpher.GetTraceRule(_template))
				input.CurrentTrace.Children.Add(new Trace(TraceType.TemplateSynthesisInput, _template) { Input = input.DeepClone() });
			List<Word> results = base.Apply(input).ToList();
			if (_morpher.GetTraceRule(_template))
			{
				foreach (Word result in results)
					result.CurrentTrace.Children.Add(new Trace(TraceType.TemplateSynthesisOutput, _template) { Output = result.DeepClone() });
			}
			return results;
		}

		protected override bool Continue(IRule<Word, ShapeNode> rule, int index, Word input)
		{
			IList<AffixTemplateSlot> slots = _template.Slots;
			bool cont = slots[index].Optional;
			if (!cont && _morpher.GetTraceRule(_template))
				input.CurrentTrace.Children.Add(new Trace(TraceType.TemplateSynthesisOutput, _template));
			return cont;
		}
	}
}
