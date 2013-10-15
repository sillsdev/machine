using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
				.Select(slot => new RuleBatch<Word, ShapeNode>(slot.Rules.Select(mr => mr.CompileAnalysisRule(spanFactory, morpher)), false, FreezableEqualityComparer<Word>.Default)));
		}

		public IEnumerable<Word> Apply(Word input)
		{
			FeatureStruct fs;
			if (!input.SyntacticFeatureStruct.Unify(_template.RequiredSyntacticFeatureStruct, out fs))
				return Enumerable.Empty<Word>();

			if (_morpher.TraceRules.Contains(_template))
				input.CurrentTrace.Children.Add(new Trace(TraceType.TemplateAnalysisInput, _template) {Input = input});

			Word inWord = input.DeepClone();
			inWord.Freeze();
			//var output = new HashSet<Word>(ValueEqualityComparer<Word>.Default);
			//ApplySlots(temp, _rules.Count - 1, output);

			var outStack = new ConcurrentStack<Word>();
			var from = new ConcurrentStack<Tuple<Word, int>>();
			from.Push(Tuple.Create(inWord, _rules.Count - 1));
			var to = new ConcurrentStack<Tuple<Word, int>>();
			while (!from.IsEmpty)
			{
				to.Clear();
			    Parallel.ForEach(from, work =>
				    {
					    bool add = true;
			            for (int i = work.Item2; i >= 0; i--)
			            {
				            Tuple<Word, int>[] workItems = _rules[i].Apply(work.Item1).Select(res => Tuple.Create(res, i - 1)).ToArray();
							if (workItems.Length > 0)
								to.PushRange(workItems);

			                if (!_template.Slots[i].Optional)
			                {
			                    if (_morpher.TraceRules.Contains(_template))
			                        work.Item1.CurrentTrace.Children.Add(new Trace(TraceType.TemplateAnalysisOutput, _template));
				                add = false;
			                    break;
			                }
			            }

					    if (add)
					    {
						    if (_morpher.TraceRules.Contains(_template))
							    work.Item1.CurrentTrace.Children.Add(new Trace(TraceType.TemplateAnalysisOutput, _template) {Output = work.Item1});
						    outStack.Push(work.Item1);
					    }
				    });
				ConcurrentStack<Tuple<Word, int>> temp = from;
			    from = to;
				to = temp;
			}

			Word[] output = outStack.Distinct(FreezableEqualityComparer<Word>.Default).ToArray();
			foreach (Word outWord in output)
				outWord.SyntacticFeatureStruct = fs;
			return output;
		}

		//private void ApplySlots(Word input, int index, HashSet<Word> output)
		//{
		//    for (int i = index; i >= 0; i--)
		//    {
		//        foreach (Word outWord in _rules[i].Apply(input))
		//            ApplySlots(outWord, i - 1, output);

		//        if (!_template.Slots[i].Optional)
		//        {
		//            if (_morpher.TraceRules.Contains(_template))
		//                input.CurrentTrace.Children.Add(new Trace(TraceType.TemplateAnalysisOutput, _template));
		//            return;
		//        }
		//    }

		//    if (_morpher.TraceRules.Contains(_template))
		//        input.CurrentTrace.Children.Add(new Trace(TraceType.TemplateAnalysisOutput, _template) {Output = input});
		//    output.Add(input);
		//}
	}
}
