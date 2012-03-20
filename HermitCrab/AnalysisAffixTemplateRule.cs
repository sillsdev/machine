using System.Collections.Generic;
using System.Linq;
using SIL.Machine;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	public class AnalysisAffixTemplateRule : RuleCascade<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly AffixTemplate _template;

		public AnalysisAffixTemplateRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, AffixTemplate template)
			: base(CreateRules(spanFactory, morpher, template), RuleCascadeOrder.Permutation)
		{
			_morpher = morpher;
			_template = template;
		}

		private static IEnumerable<IRule<Word, ShapeNode>> CreateRules(SpanFactory<ShapeNode> spanFactory, Morpher morpher, AffixTemplate template)
		{
			foreach (AffixTemplateSlot slot in template.Slots.Reverse())
				yield return new RuleCascade<Word, ShapeNode>(slot.Rules.Select(mr => mr.CompileAnalysisRule(spanFactory, morpher)), RuleCascadeOrder.Permutation);
		}

		public override IEnumerable<Word> Apply(Word input)
		{
			if (_morpher.GetTraceRule(_template))
				input.CurrentTrace.Children.Add(new Trace(TraceType.TemplateAnalysisInput, _template) { Input = input.DeepClone() });
			List<Word> results = base.Apply(input).ToList();
			foreach (Word result in results)
			{
				if (_morpher.GetTraceRule(_template))
					result.CurrentTrace.Children.Add(new Trace(TraceType.TemplateAnalysisOutput, _template) { Output = result.DeepClone() });
				result.SyntacticFeatureStruct.Union(_template.RequiredSyntacticFeatureStruct);
			}
			return results;
		}

		protected override bool Continue(IRule<Word, ShapeNode> rule, int index, Word input)
		{
			IList<AffixTemplateSlot> slots = _template.Slots;
			bool cont = slots[slots.Count - index - 1].Optional;
			if (!cont && _morpher.GetTraceRule(_template))
				input.CurrentTrace.Children.Add(new Trace(TraceType.TemplateAnalysisOutput, _template));
			return cont;
		}
	}
}
