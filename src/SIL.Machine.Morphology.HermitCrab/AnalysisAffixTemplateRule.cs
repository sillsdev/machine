using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
                template.Slots.Select(slot => new RuleBatch<Word, ShapeNode>(
                    slot.Rules.Select(mr => mr.CompileAnalysisRule(morpher)),
                    false,
                    FreezableEqualityComparer<Word>.Default
                ))
            );
        }

        public IEnumerable<Word> Apply(Word input)
        {
            if (!_morpher.RuleSelector(_template))
                return Enumerable.Empty<Word>();

            FeatureStruct fs;
            if (!input.SyntacticFeatureStruct.Unify(_template.RequiredSyntacticFeatureStruct, out fs))
                return Enumerable.Empty<Word>();

            Debug.Assert(input.IsFrozen, "AnalysisAffixTemplateRule.Apply requires a frozen input");

            bool tracing = _morpher.TraceManager.IsTracing;
            Word inWord;
            if (tracing)
            {
                // Tracing must stay byte-identical to the unmemoized engine, so keep the eager clone: the
                // trace manager mutates CurrentTrace on whatever word it is handed, and every recursive call
                // below must see a word that is safe to stamp that way, never the caller's shared input.
                _morpher.TraceManager.BeginUnapplyTemplate(_template, input);
                inWord = input.CloneForEngine();
                inWord.Freeze();
            }
            else
            {
                inWord = input;
            }

            var output = new HashSet<Word>(FreezableEqualityComparer<Word>.Default);
            if (_morpher.MaxDegreeOfParallelism == 1)
                ApplySlots(inWord, input, _rules.Count - 1, output);
            else
                ParallelApplySlots(inWord, input, output);

            foreach (Word outWord in output)
            {
                outWord.EnsureOwnSyntacticFeatureStruct();
                outWord.SyntacticFeatureStruct.Add(fs);
            }
            return output;
        }

        // originalInput is the word Apply was called with. Every word the DFS produces along the way is a
        // fresh clone (from an underlying rule's ApplyRhs, or -- in the traced case -- the eager clone above),
        // except the word reaching the "every remaining slot was skipped" terminal, which is only ever the
        // untraced inWord passed in from the top: it may still be the same object as originalInput. Apply then
        // mutates every emitted word's SyntacticFeatureStruct, so that one case must be cloned on the way out.
        private void ApplySlots(Word inWord, Word originalInput, int index, HashSet<Word> output)
        {
            for (int i = index; i >= 0; i--)
            {
                foreach (Word outWord in _rules[i].Apply(inWord))
                    ApplySlots(outWord, originalInput, i - 1, output);

                if (!_template.Slots[i].Optional)
                {
                    if (_morpher.TraceManager.IsTracing)
                        _morpher.TraceManager.EndUnapplyTemplate(_template, inWord, false);
                    return;
                }
            }

            if (_morpher.TraceManager.IsTracing)
                _morpher.TraceManager.EndUnapplyTemplate(_template, inWord, true);
            output.Add(ReferenceEquals(inWord, originalInput) ? CloneFrozen(inWord) : inWord);
        }

        private void ParallelApplySlots(Word inWord, Word originalInput, HashSet<Word> output)
        {
            ParallelOptions parallelOptions = _morpher.CreateParallelOptions();
            var outStack = new ConcurrentStack<Word>();
            var from = new ConcurrentStack<Tuple<Word, int>>();
            from.Push(Tuple.Create(inWord, _rules.Count - 1));
            var to = new ConcurrentStack<Tuple<Word, int>>();
            while (!from.IsEmpty)
            {
                to.Clear();
                Parallel.ForEach(
                    from,
                    parallelOptions,
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
                            outStack.Push(
                                ReferenceEquals(work.Item1, originalInput) ? CloneFrozen(work.Item1) : work.Item1
                            );
                        }
                    }
                );
                ConcurrentStack<Tuple<Word, int>> temp = from;
                from = to;
                to = temp;
            }

            output.UnionWith(outStack);
        }

        private static Word CloneFrozen(Word word)
        {
            Word clone = word.CloneForEngine();
            clone.Freeze();
            return clone;
        }
    }
}
