namespace SIL.Machine.Morphology.HermitCrab
{
    public class TraceManager : ITraceManager
    {
        public bool IsTracing { get; set; }

        /// <summary>
        /// Gets or sets whether trace nodes keep the words they describe. When <c>true</c> (the default),
        /// <see cref="Trace.Input"/> and <see cref="Trace.Output"/> hold the words, and some nodes hold clones.
        /// When <c>false</c>, both are always <c>null</c> and each node keeps only <see cref="Trace.InputText"/> and
        /// <see cref="Trace.OutputText"/>, so a trace no longer keeps every intermediate shape alive. The tree and
        /// the text are the same in both modes.
        /// </summary>
        public bool RetainWords { get; set; } = true;

        private Trace Node(
            TraceType type,
            IHCRule source,
            int subruleIndex = -1,
            Word input = null,
            Word output = null,
            FailureReason reason = FailureReason.None,
            bool cloneInput = false,
            bool cloneOutput = false
        )
        {
            var trace = new Trace(type, source) { SubruleIndex = subruleIndex, FailureReason = reason };
            if (RetainWords)
            {
                trace.Input = cloneInput ? input?.Clone() : input;
                trace.Output = cloneOutput ? output?.Clone() : output;
            }
            else
            {
                bool analysis = Trace.IsAnalysis(type);
                trace.SetTexts(input?.GetTraceText(analysis), output?.GetTraceText(analysis));
            }
            return trace;
        }

        public object GenerateWords(Language lang)
        {
            return Node(TraceType.GenerateWords, lang);
        }

        public void AnalyzeWord(Language lang, Word input)
        {
            input.CurrentTrace = Node(TraceType.WordAnalysis, lang, input: input);
        }

        public void BeginUnapplyStratum(Stratum stratum, Word input)
        {
            ((Trace)input.CurrentTrace).Children.Add(Node(TraceType.StratumAnalysisInput, stratum, input: input));
        }

        public void EndUnapplyStratum(Stratum stratum, Word output)
        {
            ((Trace)output.CurrentTrace).Children.Add(Node(TraceType.StratumAnalysisOutput, stratum, output: output));
        }

        public void PhonologicalRuleUnapplied(IPhonologicalRule rule, int subruleIndex, Word input, Word output)
        {
            ((Trace)output.CurrentTrace).Children.Add(
                Node(
                    TraceType.PhonologicalRuleAnalysis,
                    rule,
                    subruleIndex: subruleIndex,
                    input: input,
                    output: output,
                    cloneOutput: true
                )
            );
        }

        public void PhonologicalRuleNotUnapplied(IPhonologicalRule rule, int subruleIndex, Word input)
        {
            ((Trace)input.CurrentTrace).Children.Add(
                Node(
                    TraceType.PhonologicalRuleAnalysis,
                    rule,
                    subruleIndex: subruleIndex,
                    input: input,
                    cloneInput: true
                )
            );
        }

        public void BeginUnapplyTemplate(AffixTemplate template, Word input)
        {
            ((Trace)input.CurrentTrace).Children.Add(Node(TraceType.TemplateAnalysisInput, template, input: input));
        }

        public void EndUnapplyTemplate(AffixTemplate template, Word output, bool unapplied, FailureReason reason)
        {
            ((Trace)output.CurrentTrace).Children.Add(
                Node(TraceType.TemplateAnalysisOutput, template, output: unapplied ? output : null, reason: reason)
            );
        }

        public void MorphologicalRuleUnapplied(IMorphologicalRule rule, int subruleIndex, Word input, Word output)
        {
            var trace = Node(
                TraceType.MorphologicalRuleAnalysis,
                rule,
                subruleIndex: subruleIndex,
                input: input,
                output: output
            );
            ((Trace)output.CurrentTrace).Children.Add(trace);
            output.CurrentTrace = trace;
        }

        public void MorphologicalRuleNotUnapplied(IMorphologicalRule rule, int subruleIndex, Word input)
        {
            ((Trace)input.CurrentTrace).Children.Add(
                Node(TraceType.MorphologicalRuleAnalysis, rule, subruleIndex: subruleIndex, input: input)
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
                Node(TraceType.CompoundingRuleAnalysis, rule, subruleIndex: subruleIndex, input: input, reason: reason)
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
                Node(TraceType.CompoundingRuleSynthesis, rule, subruleIndex: subruleIndex, input: input, reason: reason)
            );
        }

        public void LexicalLookup(Stratum stratum, Word input)
        {
            ((Trace)input.CurrentTrace).Children.Add(
                Node(TraceType.LexicalLookup, stratum, input: input, cloneInput: true)
            );
        }

        public void SynthesizeWord(Language lang, Word input)
        {
            var trace = Node(TraceType.WordSynthesis, lang, input: input, cloneInput: true);
            var curTrace = (Trace)input.CurrentTrace;
            curTrace.Children.Last.Children.Add(trace);
            input.CurrentTrace = trace;
        }

        public void BeginApplyStratum(Stratum stratum, Word input)
        {
            ((Trace)input.CurrentTrace).Children.Add(Node(TraceType.StratumSynthesisInput, stratum, input: input));
        }

        public void NonFinalTemplateAppliedLast(Stratum stratum, Word word)
        {
            ((Trace)word.CurrentTrace).Children.Add(
                Node(TraceType.StratumSynthesisOutput, stratum, output: word, reason: FailureReason.PartialParse)
            );
        }

        public void ApplicableTemplatesNotApplied(Stratum stratum, Word word)
        {
            ((Trace)word.CurrentTrace).Children.Add(
                Node(TraceType.StratumSynthesisOutput, stratum, output: word, reason: FailureReason.PartialParse)
            );
        }

        public void EndApplyStratum(Stratum stratum, Word output)
        {
            ((Trace)output.CurrentTrace).Children.Add(Node(TraceType.StratumSynthesisOutput, stratum, output: output));
        }

        public void PhonologicalRuleApplied(IPhonologicalRule rule, int subruleIndex, Word input, Word output)
        {
            ((Trace)output.CurrentTrace).Children.Add(
                Node(
                    TraceType.PhonologicalRuleSynthesis,
                    rule,
                    subruleIndex: subruleIndex,
                    input: input,
                    output: output,
                    cloneOutput: true
                )
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
                Node(
                    TraceType.PhonologicalRuleSynthesis,
                    rule,
                    subruleIndex: subruleIndex,
                    input: input,
                    cloneInput: true,
                    reason: reason
                )
            );
        }

        public void BeginApplyTemplate(AffixTemplate template, Word input)
        {
            ((Trace)input.CurrentTrace).Children.Add(Node(TraceType.TemplateSynthesisInput, template, input: input));
        }

        public void EndApplyTemplate(AffixTemplate template, Word output, bool applied)
        {
            ((Trace)output.CurrentTrace).Children.Add(
                Node(TraceType.TemplateSynthesisOutput, template, output: applied ? output : null)
            );
        }

        public void MorphologicalRuleApplied(IMorphologicalRule rule, int subruleIndex, Word input, Word output)
        {
            var trace = Node(
                TraceType.MorphologicalRuleSynthesis,
                rule,
                subruleIndex: subruleIndex,
                input: input,
                output: output
            );
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
                Node(
                    TraceType.MorphologicalRuleSynthesis,
                    rule,
                    subruleIndex: subruleIndex,
                    input: input,
                    reason: reason
                )
            );
        }

        public void Blocked(IHCRule rule, Word output)
        {
            ((Trace)output.CurrentTrace).Children.Add(Node(TraceType.Blocked, rule, output: output));
        }

        public void Successful(Language lang, Word word)
        {
            ((Trace)word.CurrentTrace).Children.Add(Node(TraceType.Successful, lang, output: word));
        }

        public void Failed(Language lang, Word word, FailureReason reason, Allomorph allomorph, object failureObj)
        {
            ((Trace)word.CurrentTrace).Children.Add(Node(TraceType.Failed, lang, output: word, reason: reason));
        }
    }
}
