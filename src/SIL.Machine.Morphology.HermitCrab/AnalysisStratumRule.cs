using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    internal class AnalysisStratumRule : InstrumentedRule<Word, int>
    {
        private readonly IRule<Word, int> _mrulesRule;
        private readonly IRule<Word, int> _prulesRule;
        private readonly IRule<Word, int> _templatesRule;
        private readonly Stratum _stratum;
        private readonly Morpher _morpher;

        public AnalysisStratumRule(Morpher morpher, Stratum stratum)
        {
            Name = stratum.Name;
            _stratum = stratum;
            _morpher = morpher;
            _prulesRule = new LinearRuleCascade<Word, int>(
                stratum.PhonologicalRules.Select(prule => CompilePhonologicalRule(prule, morpher)).Reverse()
            );
            _templatesRule = new RuleBatch<Word, int>(
                stratum.AffixTemplates.Select(template => CompileAffixTemplate(template, morpher)),
                false,
                FreezableEqualityComparer<Word>.Default
            );
            _mrulesRule = null;
            IEnumerable<IRule<Word, int>> mrules = stratum
                .MorphologicalRules.Select(mrule => CompileMorphologicalRule(mrule, morpher))
                .Reverse();
            switch (stratum.MorphologicalRuleOrder)
            {
                case MorphologicalRuleOrder.Linear:
                    // Use PermutationRuleCascade instead of LinearRuleCascade
                    // because morphological rules should be considered optional
                    // during unapplication (they are obligatory during application,
                    // but we don't know they have been applied during unapplication).
                    _mrulesRule = new PermutationRuleCascade<Word, int>(
                        mrules,
                        true,
                        FreezableEqualityComparer<Word>.Default
                    );
                    break;
                case MorphologicalRuleOrder.Unordered:
                    // Single-threaded when the caller caps within-word parallelism (e.g. it
                    // parallelizes across words itself); parallel cascade otherwise. The sequential
                    // cascade additionally memoizes analysis states (parse-optimization.md Phases 2-3) --
                    // the parallel one does not yet, see MemoizedCombinationRuleCascade's doc comment.
                    _mrulesRule =
                        morpher.MaxDegreeOfParallelism == 1
                            ? (IRule<Word, int>)
                                new MemoizedCombinationRuleCascade(
                                    mrules,
                                    FreezableEqualityComparer<Word>.Default,
                                    morpher
                                )
                            : new ParallelCombinationRuleCascade<Word, int>(
                                mrules,
                                true,
                                FreezableEqualityComparer<Word>.Default
                            )
                            {
                                // Honor the within-word parallelism cap rather than running at
                                // the default (effectively unbounded) scheduler degree.
                                MaxDegreeOfParallelism = morpher.MaxDegreeOfParallelism,
                            };
                    break;
            }
            AddSubRule(_prulesRule);
            AddSubRule(_templatesRule);
            AddSubRule(_mrulesRule);
        }

        private IRule<Word, int> CompileAffixTemplate(AffixTemplate template, Morpher morpher)
        {
            try
            {
                return template.CompileAnalysisRule(morpher);
            }
            catch (Exception e)
            {
                throw new CompileException("Could not compile affix template named " + template.Name, e);
            }
        }

        private IRule<Word, int> CompileMorphologicalRule(IMorphologicalRule mrule, Morpher morpher)
        {
            try
            {
                return mrule.CompileAnalysisRule(morpher);
            }
            catch (Exception e)
            {
                throw new CompileException("Could not compile morphological rule named " + mrule.Name, e);
            }
        }

        private IRule<Word, int> CompilePhonologicalRule(IPhonologicalRule prule, Morpher morpher)
        {
            try
            {
                return prule.CompileAnalysisRule(morpher);
            }
            catch (Exception e)
            {
                throw new CompileException("Could not compile phonological rule named " + prule.Name, e);
            }
        }

        public override IEnumerable<Word> Apply(Word input)
        {
            long startTime = Stopwatch.GetTimestamp();
            if (_morpher.TraceManager.IsTracing)
                _morpher.TraceManager.BeginUnapplyStratum(_stratum, input);

            Word origInput = input;
            input = input.Clone();
            input.Stratum = _stratum;

            _prulesRule.Apply(input);
            input.Freeze();

            // parse-optimization.md Phase 4 Gate B: once phonological unapplication has grown this
            // candidate past the longest form the grammar's own lexicon+rules could ever produce (root +
            // every rule's own max insertion, see GrammarAnalyzer), no morphological unapplication from
            // here can ever recover a valid analysis -- affix rules only ever shrink during analysis (they
            // reverse synthesis's insertions), so the length can only go down from here. Skip the
            // (potentially exponential) morphological cascade entirely rather than let it search a subtree
            // that is already provably dead. Null MaxAnalysisLength means the grammar couldn't be measured
            // this exactly (a compounding rule, or a phonological pattern with quantifiers/groups) -- see
            // GrammarAnalyzer's remarks -- so the gate is off, matching today's unbounded behavior exactly.
            // Bypassed while tracing (ground rule 1): the early return below would also skip this word's
            // EndUnapplyStratum trace event further down.
            if (
                !_morpher.TraceManager.IsTracing
                && _morpher.MaxAnalysisLength is int maxLength
                && input.Shape.SegmentCount() > maxLength
            )
            {
                ElapsedTime += Stopwatch.GetTimestamp() - startTime;
                AddRuleStats(0);
                return Enumerable.Empty<Word>();
            }

            IDictionary<Shape, Word> shapeWord = null;
            // Don't merge if tracing because it messes up the tracing.
            bool mergeEquivalentAnalyses = _morpher.MergeEquivalentAnalyses && !_morpher.TraceManager.IsTracing;
            if (mergeEquivalentAnalyses)
                shapeWord = new Dictionary<Shape, Word>(FreezableEqualityComparer<Shape>.Default);

            // AnalysisStratumRule.Apply should cover the inverse of SynthesisStratumRule.Apply.
            IEnumerable<Word> mruleOutWords = ApplyTemplates(input).Concat(ApplyMorphologicalRules(input));
            Debug.Assert(mruleOutWords != null);

            var output = new HashSet<Word>(FreezableEqualityComparer<Word>.Default) { input };
            if (_morpher.TraceManager.IsTracing)
                _morpher.TraceManager.EndUnapplyStratum(_stratum, input);
            foreach (Word mruleOutWord in mruleOutWords)
            {
                // Skip intermediate sources from phonological rules, templates, and morphological rules.
                mruleOutWord.Source = origInput;
                if (mergeEquivalentAnalyses)
                {
                    Shape shape = mruleOutWord.Shape;
                    Word canonicalWord;
                    if (shapeWord.TryGetValue(shape, out canonicalWord))
                    {
                        canonicalWord.Alternatives.Add(mruleOutWord);
                        continue;
                    }
                    shapeWord[shape] = mruleOutWord;
                }
                output.Add(mruleOutWord);
                if (_morpher.TraceManager.IsTracing)
                    _morpher.TraceManager.EndUnapplyStratum(_stratum, mruleOutWord);
                if (_morpher.MaxUnapplications > 0 && output.Count >= _morpher.MaxUnapplications)
                    break;
            }
            ElapsedTime += Stopwatch.GetTimestamp() - startTime;
            AddRuleStats(output.Count);
            return output;
        }

        // Test hook: incremented on every template-memo replay (see ApplyTemplateBattery). The
        // equivalence test that covers the replay path asserts this is nonzero so it can never go
        // vacuous -- a memo that silently stops firing would otherwise look exactly like a passing test.
        internal static long DiagTemplateMemoHits;

        private IEnumerable<Word> ApplyMorphologicalRules(Word input)
        {
            foreach (Word mruleOutWord in _mrulesRule.Apply(input))
            {
                switch (_stratum.MorphologicalRuleOrder)
                {
                    case MorphologicalRuleOrder.Linear:
                        yield return mruleOutWord;
                        break;

                    case MorphologicalRuleOrder.Unordered:
                        foreach (Word tempOutWord in ApplyTemplates(mruleOutWord))
                            yield return tempOutWord;
                        yield return mruleOutWord;
                        break;
                }
            }
        }

        // Runs the template battery for `input`, memoized by AnalysisStateKey (parse-optimization.md
        // Phase 3, extended 2026-07-03). Template unapplication reads only what the key captures --
        // shape, syntactic FS, stratum, non-heads -- never the trail's ORDER, so an equal-keyed arrival
        // gets the stored outputs replayed (Word.ReplayOnto grafts the new arrival's own trail/non-head
        // prefix, identical to the mrule-cascade memo). Measured motivation: on Sena's `atawirambo`, the
        // template battery ran 38,840 times against ~2,581 unique keys and accounted for 93% of parse
        // wall time -- the mrule cascade Phases 2/3 memoized had already shrunk to ~1.4s. Sequential
        // only and skipped while tracing (scope is null then), matching the mrule memo's scoping; no
        // in-flight re-entry guard is needed here because _templatesRule.Apply is eager and
        // self-contained (the template<->mrule mutual recursion lives in this class's enumerators,
        // outside the memoized call).
        private IEnumerable<Word> ApplyTemplateBattery(Word input)
        {
            AnalysisScope scope = input.AnalysisScope;
            if (scope == null || _morpher.MaxDegreeOfParallelism != 1)
                return _templatesRule.Apply(input);

            var key = new AnalysisStateKey(input);
            if (scope.TemplateMemo.TryGetValue(key, out MemoEntry entry))
            {
                var replayed = new List<Word>(entry.Results.Count);
                DiagTemplateMemoHits++;
                foreach (Word stored in entry.Results)
                    replayed.Add(stored.ReplayOnto(input, entry.MruleTrailPrefixLength, entry.NonHeadPrefixLength));
                return replayed;
            }

            var results = new List<Word>(_templatesRule.Apply(input));
            if (scope.HasMemoCapacity)
            {
                scope.TemplateMemo.TryAdd(
                    key,
                    new MemoEntry(results, input.MorphologicalRuleTrailLength, input.NonHeadCount)
                );
            }
            return results;
        }

        private IEnumerable<Word> ApplyTemplates(Word input)
        {
            foreach (Word tempOutWord in ApplyTemplateBattery(input))
            {
                switch (_stratum.MorphologicalRuleOrder)
                {
                    case MorphologicalRuleOrder.Linear:
                        foreach (Word outWord in ApplyMorphologicalRules(tempOutWord))
                            yield return outWord;
                        if (!FreezableEqualityComparer<Word>.Default.Equals(input, tempOutWord))
                            yield return tempOutWord;
                        break;

                    case MorphologicalRuleOrder.Unordered:
                        if (!FreezableEqualityComparer<Word>.Default.Equals(input, tempOutWord))
                        {
                            foreach (Word outWord in ApplyMorphologicalRules(tempOutWord))
                                yield return outWord;
                            yield return tempOutWord;
                        }
                        break;
                }
            }
        }
    }
}
