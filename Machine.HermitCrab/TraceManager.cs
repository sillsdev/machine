namespace SIL.Machine.HermitCrab
{
	public class TraceManager : ITraceManager
	{
		public bool IsTracing { get; set; }

		public void AnalyzeWord(Language lang, Word input)
		{
			input.CurrentTrace = new Trace(TraceType.WordAnalysis, lang) {Input = input};
		}

		public void BeginUnapplyStratum(Stratum stratum, Word input)
		{
			((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.StratumAnalysisInput, stratum) {Input = input});
		}

		public void EndUnapplyStratum(Stratum stratum, Word output)
		{
			((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.StratumAnalysisOutput, stratum) {Output = output});
		}

		public void PhonologicalRuleUnapplied(IPhonologicalRule rule, int subruleIndex, Word input, Word output)
		{
			((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.PhonologicalRuleAnalysis, rule) {SubruleIndex = subruleIndex, Input = input, Output = output.DeepClone()});
		}

		public void PhonologicalRuleNotUnapplied(IPhonologicalRule rule, int subruleIndex, Word input)
		{
			((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.PhonologicalRuleAnalysis, rule) {SubruleIndex = subruleIndex, Input = input.DeepClone()});
		}

		public void BeginUnapplyTemplate(AffixTemplate template, Word input)
		{
			((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.TemplateAnalysisInput, template) {Input = input});
		}

		public void EndUnapplyTemplate(AffixTemplate template, Word output, bool unapplied)
		{
			((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.TemplateAnalysisOutput, template) {Output = unapplied ? output : null});
		}

		public void MorphologicalRuleUnapplied(IMorphologicalRule rule, int subruleIndex, Word input, Word output)
		{
			var trace = new Trace(TraceType.MorphologicalRuleAnalysis, rule) {SubruleIndex = subruleIndex, Input = input, Output = output};
			((Trace) output.CurrentTrace).Children.Add(trace);
			output.CurrentTrace = trace;
		}

		public void MorphologicalRuleNotUnapplied(IMorphologicalRule rule, int subruleIndex, Word input)
		{
			((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.MorphologicalRuleAnalysis, rule) {SubruleIndex = subruleIndex, Input = input});
		}

		public void LexicalLookup(Stratum stratum, Word input)
		{
			((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.LexicalLookup, stratum) {Input = input.DeepClone()});
		}

		public void SynthesizeWord(Language lang, Word input)
		{
			var trace = new Trace(TraceType.WordSynthesis, lang) {Input = input.DeepClone()};
			var curTrace = (Trace) input.CurrentTrace;
			curTrace.Children.Last.Children.Add(trace);
			input.CurrentTrace = trace;
		}

		public void BeginApplyStratum(Stratum stratum, Word input)
		{
			((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.StratumSynthesisInput, stratum) {Input = input});
		}

		public void NonFinalTemplateAppliedLast(Stratum stratum, Word word)
		{
			((Trace) word.CurrentTrace).Children.Add(new Trace(TraceType.StratumSynthesisOutput, stratum) {Output = word, FailureReason = FailureReason.PartialParse});
		}

		public void EndApplyStratum(Stratum stratum, Word output)
		{
			((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.StratumSynthesisOutput, stratum) {Output = output});
		}

		public void PhonologicalRuleApplied(IPhonologicalRule rule, int subruleIndex, Word input, Word output)
		{
			((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.PhonologicalRuleSynthesis, rule) {SubruleIndex = subruleIndex, Input = input, Output = output.DeepClone()});
		}

		public void PhonologicalRuleNotApplied(IPhonologicalRule rule, int subruleIndex, Word input, FailureReason reason, object failureObj)
		{
			((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.PhonologicalRuleSynthesis, rule) {SubruleIndex = subruleIndex, Input = input.DeepClone(), FailureReason = reason});
		}

		public void BeginApplyTemplate(AffixTemplate template, Word input)
		{
			((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.TemplateSynthesisInput, template) {Input = input});
		}

		public void EndApplyTemplate(AffixTemplate template, Word output, bool applied)
		{
			((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.TemplateSynthesisOutput, template) {Output = applied ? output : null});
		}

		public void MorphologicalRuleApplied(IMorphologicalRule rule, int subruleIndex, Word input, Word output)
		{
			var trace = new Trace(TraceType.MorphologicalRuleSynthesis, rule) {SubruleIndex = subruleIndex, Input = input, Output = output};
			((Trace) output.CurrentTrace).Children.Add(trace);
			output.CurrentTrace = trace;
		}

		public void MorphologicalRuleNotApplied(IMorphologicalRule rule, int subruleIndex, Word input, FailureReason reason, object failureObj)
		{
			((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.MorphologicalRuleSynthesis, rule) {SubruleIndex = subruleIndex, Input = input, FailureReason = reason});
		}

		public void ParseBlocked(IHCRule rule, Word output)
		{
			((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.ParseBlocked, rule) {Output = output});
		}

		public void ParseSuccessful(Language lang, Word word)
		{
			((Trace) word.CurrentTrace).Children.Add(new Trace(TraceType.ParseSuccessful, lang) {Output = word});
		}

		public void ParseFailed(Language lang, Word word, FailureReason reason, Allomorph allomorph, object failureObj)
		{
			((Trace) word.CurrentTrace).Children.Add(new Trace(TraceType.ParseFailed, lang) {Output = word, FailureReason = reason});
		}
	}
}
