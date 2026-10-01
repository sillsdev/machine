namespace SIL.Machine.Morphology.HermitCrab
{
    public class TraceManager : IDetailedTraceManager
    {
        private readonly System.Runtime.CompilerServices.ConditionalWeakTable<Word, Trace> _lexicalLookups =
            new System.Runtime.CompilerServices.ConditionalWeakTable<Word, Trace>();
        public bool IsTracing { get; set; }

        public object GenerateWords(Language lang)
        {
            return new Trace(TraceType.GenerateWords, lang);
        }

        public void AnalyzeWord(Language lang, Word input)
        {
            input.CurrentTrace = new Trace(TraceType.WordAnalysis, lang) { Input = input };
        }

        public void BeginUnapplyStratum(Stratum stratum, Word input)
        {
            ((Trace)input.CurrentTrace).Children.Add(
                new Trace(TraceType.StratumAnalysisInput, stratum) { Input = input }
            );
        }

        public void EndUnapplyStratum(Stratum stratum, Word output)
        {
            ((Trace)output.CurrentTrace).Children.Add(
                new Trace(TraceType.StratumAnalysisOutput, stratum) { Output = output }
            );
        }

        public void PhonologicalRuleUnapplied(IPhonologicalRule rule, int subruleIndex, Word input, Word output)
        {
            ((Trace)output.CurrentTrace).Children.Add(
                new Trace(TraceType.PhonologicalRuleAnalysis, rule)
                {
                    SubruleIndex = subruleIndex,
                    Input = input,
                    Output = output.Clone(),
                }
            );
        }

        public void PhonologicalRuleNotUnapplied(IPhonologicalRule rule, int subruleIndex, Word input)
        {
            ((Trace)input.CurrentTrace).Children.Add(
                new Trace(TraceType.PhonologicalRuleAnalysis, rule)
                {
                    SubruleIndex = subruleIndex,
                    Input = input.Clone(),
                }
            );
        }

        public void BeginUnapplyTemplate(AffixTemplate template, Word input)
        {
            ((Trace)input.CurrentTrace).Children.Add(
                new Trace(TraceType.TemplateAnalysisInput, template) { Input = input }
            );
        }

        public void EndUnapplyTemplate(AffixTemplate template, Word output, bool unapplied, FailureReason reason)
        {
            ((Trace)output.CurrentTrace).Children.Add(
                new Trace(TraceType.TemplateAnalysisOutput, template)
                {
                    Output = unapplied ? output : null,
                    FailureReason = reason,
                }
            );
        }

        public void MorphologicalRuleUnapplied(IMorphologicalRule rule, int subruleIndex, Word input, Word output)
        {
            var trace = new Trace(TraceType.MorphologicalRuleAnalysis, rule)
            {
                SubruleIndex = subruleIndex,
                Input = input,
                Output = output,
            };
            ((Trace)output.CurrentTrace).Children.Add(trace);
            output.CurrentTrace = trace;
        }

        public void MorphologicalRuleNotUnapplied(IMorphologicalRule rule, int subruleIndex, Word input)
        {
            MorphologicalRuleNotUnapplied(rule, subruleIndex, input, FailureReason.None, null);
        }

        public void MorphologicalRuleNotUnapplied(
            IMorphologicalRule rule,
            int subruleIndex,
            Word input,
            FailureReason reason,
            object failureObj
        )
        {
            ((Trace)input.CurrentTrace).Children.Add(
                new Trace(TraceType.MorphologicalRuleAnalysis, rule)
                {
                    SubruleIndex = subruleIndex,
                    Input = input,
                    FailureReason = reason,
                    FailureObject = failureObj,
                }
            );
        }

        public void CompoundingRuleNotUnapplied(
            IMorphologicalRule rule,
            int subruleIndex,
            Word input,
            FailureReason reason,
            object failureObj
        )
        {
            ((Trace)input.CurrentTrace).Children.Add(
                new Trace(TraceType.CompoundingRuleAnalysis, rule)
                {
                    SubruleIndex = subruleIndex,
                    Input = input,
                    FailureReason = reason,
                    FailureObject = failureObj,
                }
            );
        }

        public void CompoundingRuleNotApplied(
            IMorphologicalRule rule,
            int subruleIndex,
            Word input,
            FailureReason reason,
            object failureObj
        )
        {
            ((Trace)input.CurrentTrace).Children.Add(
                new Trace(TraceType.CompoundingRuleSynthesis, rule)
                {
                    SubruleIndex = subruleIndex,
                    Input = input,
                    FailureReason = reason,
                    FailureObject = failureObj,
                }
            );
        }

        public void LexicalLookup(Stratum stratum, Word input)
        {
            var trace = new Trace(TraceType.LexicalLookup, stratum) { Input = input.Clone() };
            ((Trace)input.CurrentTrace).Children.Add(trace);
            _lexicalLookups.Remove(input);
            _lexicalLookups.Add(input, trace);
        }

        public void LexicalLookupCompleted(Stratum stratum, Word input, int candidateCount, bool guessed)
        {
            if (_lexicalLookups.TryGetValue(input, out Trace trace))
            {
                trace.LexicalCandidateCount = candidateCount;
                trace.IsLexicalGuess = guessed;
                _lexicalLookups.Remove(input);
            }
        }

        public void SynthesizeWord(Language lang, Word input)
        {
            var trace = new Trace(TraceType.WordSynthesis, lang) { Input = input.Clone() };
            var curTrace = (Trace)input.CurrentTrace;
            if (input.Source != null && _lexicalLookups.TryGetValue(input.Source, out Trace lookup))
                lookup.Children.Add(trace);
            else
                curTrace.Children.Last.Children.Add(trace);
            input.CurrentTrace = trace;
        }

        public void BeginApplyStratum(Stratum stratum, Word input)
        {
            ((Trace)input.CurrentTrace).Children.Add(
                new Trace(TraceType.StratumSynthesisInput, stratum) { Input = input }
            );
        }

        public void NonFinalTemplateAppliedLast(Stratum stratum, Word word)
        {
            ((Trace)word.CurrentTrace).Children.Add(
                new Trace(TraceType.StratumSynthesisOutput, stratum)
                {
                    Output = word,
                    FailureReason = FailureReason.PartialParse,
                    PartialParseCause = PartialParseCause.NonFinalTemplateAppliedLast,
                }
            );
        }

        public void ApplicableTemplatesNotApplied(Stratum stratum, Word word)
        {
            ((Trace)word.CurrentTrace).Children.Add(
                new Trace(TraceType.StratumSynthesisOutput, stratum)
                {
                    Output = word,
                    FailureReason = FailureReason.PartialParse,
                    PartialParseCause = PartialParseCause.ApplicableTemplatesNotApplied,
                }
            );
        }

        public void EndApplyStratum(Stratum stratum, Word output)
        {
            ((Trace)output.CurrentTrace).Children.Add(
                new Trace(TraceType.StratumSynthesisOutput, stratum) { Output = output }
            );
        }

        public void PhonologicalRuleApplied(IPhonologicalRule rule, int subruleIndex, Word input, Word output)
        {
            ((Trace)output.CurrentTrace).Children.Add(
                new Trace(TraceType.PhonologicalRuleSynthesis, rule)
                {
                    SubruleIndex = subruleIndex,
                    Input = input,
                    Output = output.Clone(),
                }
            );
        }

        public void PhonologicalRuleNotApplied(
            IPhonologicalRule rule,
            int subruleIndex,
            Word input,
            FailureReason reason,
            object failureObj
        )
        {
            ((Trace)input.CurrentTrace).Children.Add(
                new Trace(TraceType.PhonologicalRuleSynthesis, rule)
                {
                    SubruleIndex = subruleIndex,
                    Input = input.Clone(),
                    FailureReason = reason,
                    FailureObject = failureObj,
                }
            );
        }

        public void BeginApplyTemplate(AffixTemplate template, Word input)
        {
            ((Trace)input.CurrentTrace).Children.Add(
                new Trace(TraceType.TemplateSynthesisInput, template) { Input = input }
            );
        }

        public void EndApplyTemplate(AffixTemplate template, Word output, bool applied)
        {
            ((Trace)output.CurrentTrace).Children.Add(
                new Trace(TraceType.TemplateSynthesisOutput, template) { Output = applied ? output : null }
            );
        }

        public void MorphologicalRuleApplied(IMorphologicalRule rule, int subruleIndex, Word input, Word output)
        {
            var trace = new Trace(TraceType.MorphologicalRuleSynthesis, rule)
            {
                SubruleIndex = subruleIndex,
                Input = input,
                Output = output,
            };
            ((Trace)output.CurrentTrace).Children.Add(trace);
            output.CurrentTrace = trace;
        }

        public void MorphologicalRuleNotApplied(
            IMorphologicalRule rule,
            int subruleIndex,
            Word input,
            FailureReason reason,
            object failureObj
        )
        {
            ((Trace)input.CurrentTrace).Children.Add(
                new Trace(TraceType.MorphologicalRuleSynthesis, rule)
                {
                    SubruleIndex = subruleIndex,
                    Input = input,
                    FailureReason = reason,
                    FailureObject = failureObj,
                }
            );
        }

        public void Blocked(IHCRule rule, Word output)
        {
            ((Trace)output.CurrentTrace).Children.Add(
                new Trace(TraceType.Blocked, rule)
                {
                    Output = output,
                    BlockingEntry = output.RootAllomorph?.Morpheme as LexEntry,
                }
            );
        }

        public void Successful(Language lang, Word word)
        {
            ((Trace)word.CurrentTrace).Children.Add(new Trace(TraceType.Successful, lang) { Output = word });
        }

        public void Failed(Language lang, Word word, FailureReason reason, Allomorph allomorph, object failureObj)
        {
            ((Trace)word.CurrentTrace).Children.Add(
                new Trace(TraceType.Failed, lang)
                {
                    Output = word,
                    FailureReason = reason,
                    FailureObject = failureObj,
                    Allomorph = allomorph,
                    PartialParseCause = (failureObj as PartialParseFailure)?.Cause,
                }
            );
        }

        public void TemplateSlotProcessed(
            AffixTemplate template,
            int slotIndex,
            Word input,
            Word output,
            bool analysis,
            TemplateSlotOutcome outcome
        )
        {
            ((Trace)input.CurrentTrace).Children.Add(
                new Trace(analysis ? TraceType.TemplateSlotAnalysis : TraceType.TemplateSlotSynthesis, template)
                {
                    Input = input,
                    Output = output,
                    SlotIndex = slotIndex,
                    SlotOutcome = outcome,
                    SlotRule = output == null ? null : ((Trace)output.CurrentTrace).Source as IMorphologicalRule,
                }
            );
        }
    }
}
