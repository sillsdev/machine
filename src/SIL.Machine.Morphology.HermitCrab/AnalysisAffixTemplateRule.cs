#if !SINGLE_THREADED
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
#endif
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    internal class AnalysisAffixTemplateRule : IRule<Word, ShapeNode>
    {
        private readonly Morpher _morpher;
        private readonly AffixTemplate _template;
        private readonly List<IRule<Word, ShapeNode>> _rules;

        public AnalysisAffixTemplateRule(Morpher morpher, AffixTemplate template)
        {
            _morpher = morpher;
            _template = template;
            _rules = new List<IRule<Word, ShapeNode>>(
                template.Slots.Select(
                    slot =>
                        new RuleBatch<Word, ShapeNode>(
                            slot.Rules.Select(mr => mr.CompileAnalysisRule(morpher)),
                            false,
                            FreezableEqualityComparer<Word>.Default
                        )
                )
            );
        }

        public IEnumerable<Word> Apply(Word input)
        {
            if (!_morpher.RuleSelector(_template))
                return Enumerable.Empty<Word>();

            FeatureStruct fs;
            if (!input.SyntacticFeatureStruct.Unify(_template.RequiredSyntacticFeatureStruct, out fs))
                return Enumerable.Empty<Word>();

            if (_morpher.TraceManager.IsTracing)
                _morpher.TraceManager.BeginUnapplyTemplate(_template, input);

            Word inWord = input.Clone();
            inWord.Freeze();

            var output = new HashSet<Word>(FreezableEqualityComparer<Word>.Default);
#if SINGLE_THREADED
            ApplySlots(inWord, _rules.Count - 1, output);
#else
            ParallelApplySlots(inWord, output);
#endif

            foreach (Word outWord in output)
                outWord.SyntacticFeatureStruct.Add(fs);
            return output;
        }

#if SINGLE_THREADED
        private void ApplySlots(Word inWord, int index, HashSet<Word> output)
        {
            for (int i = index; i >= 0; i--)
            {
                foreach (Word outWord in _rules[i].Apply(inWord))
                    ApplySlots(outWord, i - 1, output);

                if (!_template.Slots[i].Optional)
                {
                    if (_morpher.TraceManager.IsTracing)
                        _morpher.TraceManager.EndUnapplyTemplate(_template, inWord, false);
                    return;
                }
            }

            if (_morpher.TraceManager.IsTracing)
                _morpher.TraceManager.EndUnapplyTemplate(_template, inWord, true);
            output.Add(inWord);
        }
#else
        private void ParallelApplySlots(Word inWord, HashSet<Word> output)
        {
            var outStack = new ConcurrentStack<Word>();
            var from = new ConcurrentStack<Tuple<Word, int>>();
            from.Push(Tuple.Create(inWord, _rules.Count - 1));
            var to = new ConcurrentStack<Tuple<Word, int>>();
            while (!from.IsEmpty)
            {
                to.Clear();
                Parallel.ForEach(
                    from,
                    work =>
                    {
                        bool add = true;
                        for (int i = work.Item2; i >= 0; i--)
                        {
                            Tuple<Word, int>[] workItems = _rules[i]
                                .Apply(work.Item1)
                                .Select(res => Tuple.Create(res, i - 1))
                                .ToArray();
                            if (workItems.Length > 0)
                                to.PushRange(workItems);

                            if (!_template.Slots[i].Optional)
                            {
                                if (_morpher.TraceManager.IsTracing)
                                    _morpher.TraceManager.EndUnapplyTemplate(_template, work.Item1, false);
                                add = false;
                                break;
                            }
                        }

                        if (add)
                        {
                            if (_morpher.TraceManager.IsTracing)
                                _morpher.TraceManager.EndUnapplyTemplate(_template, work.Item1, true);
                            outStack.Push(work.Item1);
                        }
                    }
                );
                ConcurrentStack<Tuple<Word, int>> temp = from;
                from = to;
                to = temp;
            }

            output.UnionWith(outStack);
        }
#endif
    }
}
