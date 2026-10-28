using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    internal class AnalysisAffixTemplateRule : IRule<Word, ShapeNode>
    {
        private readonly Morpher _morpher;
        private readonly AffixTemplate _template;
        private readonly List<IRule<Word, ShapeNode>> _rules;
        private readonly RequiredSlotMaterial _lookahead;

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
            _lookahead = new RequiredSlotMaterial(template);
        }

        public IEnumerable<Word> Apply(Word input)
        {
            if (!_morpher.RuleSelector(_template))
                return Enumerable.Empty<Word>();

            // A check only: adding the template's required features here would make SyntacticFeatureStruct
            // depend on whether a template was un-applied, which Word.ValueEquals assumes cannot happen.
            if (!input.SyntacticFeatureStruct.IsUnifiable(_template.RequiredSyntacticFeatureStruct))
                return Enumerable.Empty<Word>();

            if (_morpher.TraceManager.IsTracing)
                _morpher.TraceManager.BeginUnapplyTemplate(_template, input);

            // Do not allow a final template to unapply if we don't need to worry about partials
            // and a non-template was last unapplied.
            if (
                (!_morpher.IsPartial || _morpher.AlwaysEnforceFinalTemplates)
                && input.FinalTemplateState == FinalTemplateState.NonTemplate
                && _template.IsFinal
            )
            {
                if (_morpher.TraceManager.IsTracing)
                {
                    _morpher.TraceManager.EndUnapplyTemplate(
                        _template,
                        input,
                        false,
                        FailureReason.NonPartialRuleProhibitedAfterFinalTemplate
                    );
                }
                return Enumerable.Empty<Word>();
            }

            int[] fired = null;
            if (RequiredSlotCannotUnapply(input, _rules.Count - 1, ref fired))
            {
                if (_morpher.TraceManager.IsTracing)
                    _morpher.TraceManager.EndUnapplyTemplate(_template, input, false, FailureReason.None);
                return Enumerable.Empty<Word>();
            }

            Word inWord = input.Clone();
            inWord.Freeze();

            var output = new HashSet<Word>(FreezableEqualityComparer<Word>.Default);
            if (_morpher.MaxDegreeOfParallelism == 1)
                ApplySlots(inWord, _rules.Count - 1, output, fired);
            else
                ParallelApplySlots(inWord, output, fired);

            return output;
        }

        // Testing runs from the next slot to unapply and stops after an impure slot, which makes the test
        // for any slot behind it unsound. Shadow mode prunes nothing and carries the fired slots in fired.
        private bool RequiredSlotCannotUnapply(Word word, int lastSlot, ref int[] fired)
        {
            RequiredSlotLookaheadMode mode = _morpher.RequiredSlotLookahead;
            if (mode == RequiredSlotLookaheadMode.Off)
                return false;

            long unifications = 0;
            bool cannot = false;
            SlotWalk walk = SlotWalk.Start();
            for (int j = lastSlot; j >= 0; j--)
            {
                if (
                    !_template.Slots[j].Optional
                    && !IsFired(fired, j)
                    && _lookahead.CannotUnapply(j, word.Shape, ref walk, ref unifications)
                )
                {
                    if (mode == RequiredSlotLookaheadMode.On)
                    {
                        cannot = true;
                        break;
                    }
                    fired = fired == null ? new[] { j } : fired.Append(j).ToArray();
                    _morpher.RequiredSlotShadow?.Invoke("fired", _template, j);
                }
                _lookahead.Advance(j, ref walk);
                if (!_lookahead.IsPure(j))
                    break;
            }
            if (unifications != 0)
                _morpher.RequiredSlotCheckObserver?.Invoke(unifications);
            return cannot;
        }

        private static bool IsFired(int[] fired, int slot)
        {
            return fired != null && Array.IndexOf(fired, slot) >= 0;
        }

        private void ReportShadow(string kind, int[] fired, int slot)
        {
            if (fired == null || _morpher.RequiredSlotShadow == null)
                return;
            if (slot < 0)
            {
                foreach (int j in fired)
                    _morpher.RequiredSlotShadow(kind, _template, j);
            }
            else if (IsFired(fired, slot))
            {
                _morpher.RequiredSlotShadow(kind, _template, slot);
            }
        }

        private void ApplySlots(Word inWord, int index, HashSet<Word> output, int[] fired)
        {
            for (int i = index; i >= 0; i--)
            {
                foreach (Word outWord in _rules[i].Apply(inWord))
                {
                    ReportShadow("violated", fired, i);
                    int[] outFired = fired;
                    if (RequiredSlotCannotUnapply(outWord, i - 1, ref outFired))
                    {
                        if (_morpher.TraceManager.IsTracing)
                            _morpher.TraceManager.EndUnapplyTemplate(_template, outWord, false, FailureReason.None);
                        continue;
                    }
                    ApplySlots(outWord, i - 1, output, outFired);
                }

                if (!_template.Slots[i].Optional)
                {
                    if (_morpher.TraceManager.IsTracing)
                        _morpher.TraceManager.EndUnapplyTemplate(_template, inWord, false, FailureReason.None);
                    return;
                }
            }

            if (_morpher.TraceManager.IsTracing)
                _morpher.TraceManager.EndUnapplyTemplate(_template, inWord, true, FailureReason.None);
            ReportShadow("completed-under-fired", fired, -1);
            output.Add(inWord);
        }

        private void ParallelApplySlots(Word inWord, HashSet<Word> output, int[] fired)
        {
            ParallelOptions parallelOptions = _morpher.CreateParallelOptions();
            var outStack = new ConcurrentStack<Word>();
            var from = new ConcurrentStack<Tuple<Word, int, int[]>>();
            from.Push(Tuple.Create(inWord, _rules.Count - 1, fired));
            var to = new ConcurrentStack<Tuple<Word, int, int[]>>();
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
                            int next = i - 1;
                            Tuple<Word, int, int[]>[] workItems = _rules[i]
                                .Apply(work.Item1)
                                .Select(res =>
                                {
                                    ReportShadow("violated", work.Item3, i);
                                    int[] resFired = work.Item3;
                                    if (!RequiredSlotCannotUnapply(res, next, ref resFired))
                                        return Tuple.Create(res, next, resFired);
                                    if (_morpher.TraceManager.IsTracing)
                                        _morpher.TraceManager.EndUnapplyTemplate(
                                            _template,
                                            res,
                                            false,
                                            FailureReason.None
                                        );
                                    return null;
                                })
                                .Where(item => item != null)
                                .ToArray();
                            if (workItems.Length > 0)
                                to.PushRange(workItems);

                            if (!_template.Slots[i].Optional)
                            {
                                if (_morpher.TraceManager.IsTracing)
                                    _morpher.TraceManager.EndUnapplyTemplate(
                                        _template,
                                        work.Item1,
                                        false,
                                        FailureReason.None
                                    );
                                add = false;
                                break;
                            }
                        }

                        if (add)
                        {
                            if (_morpher.TraceManager.IsTracing)
                                _morpher.TraceManager.EndUnapplyTemplate(
                                    _template,
                                    work.Item1,
                                    true,
                                    FailureReason.None
                                );
                            ReportShadow("completed-under-fired", work.Item3, -1);
                            outStack.Push(work.Item1);
                        }
                    }
                );
                ConcurrentStack<Tuple<Word, int, int[]>> temp = from;
                from = to;
                to = temp;
            }

            output.UnionWith(outStack);
        }
    }
}
