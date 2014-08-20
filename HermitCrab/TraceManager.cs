namespace SIL.HermitCrab
{
	public class TraceManager : ITraceManager
	{
		public bool IsTracing { get; set; }

		public void AnalyzeWord(Language lang, Word input)
		{
			if (IsTracing)
				input.CurrentTrace = new Trace(TraceType.WordAnalysis, lang) {Input = input};
		}

		public void BeginUnapplyStratum(Stratum stratum, Word input)
		{
			if (IsTracing)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.StratumAnalysisInput, stratum) {Input = input});
		}

		public void EndUnapplyStratum(Stratum stratum, Word output)
		{
			if (IsTracing)
				((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.StratumAnalysisOutput, stratum) {Output = output});
		}

		public void PhonologicalRuleUnapplied(IPhonologicalRule rule, int subruleIndex, Word output)
		{
			if (IsTracing)
				((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.PhonologicalRuleAnalysis, rule) {SubruleIndex = subruleIndex, Output = output.DeepClone()});
		}

		public void PhonologicalRuleNotUnapplied(IPhonologicalRule rule, int subruleIndex, Word input)
		{
			if (IsTracing)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.PhonologicalRuleAnalysis, rule) {SubruleIndex = subruleIndex, Input = input.DeepClone()});
		}

		public void BeginUnapplyTemplate(AffixTemplate template, Word input)
		{
			if (IsTracing)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.TemplateAnalysisInput, template) {Input = input});
		}

		public void EndUnapplyTemplate(AffixTemplate template, Word output, bool unapplied)
		{
			if (IsTracing)
				((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.TemplateAnalysisOutput, template) {Output = unapplied ? output : null});
		}

		public void MorphologicalRuleUnapplied(IMorphologicalRule rule, int subruleIndex, Word input, Word output)
		{
			if (IsTracing)
			{
				var trace = new Trace(TraceType.MorphologicalRuleAnalysis, rule) {SubruleIndex = subruleIndex, Input = input, Output = output};
				((Trace) output.CurrentTrace).Children.Add(trace);
				output.CurrentTrace = trace;
			}
		}

		public void MorphologicalRuleNotUnapplied(IMorphologicalRule rule, int subruleIndex, Word input)
		{
			if (IsTracing)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.MorphologicalRuleAnalysis, rule) {SubruleIndex = subruleIndex, Input = input});
		}

		public void LexicalLookup(Stratum stratum, Word input)
		{
			if (IsTracing)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.LexicalLookup, stratum) {Input = input.DeepClone()});
		}

		public void SynthesizeWord(Language lang, Word input)
		{
			if (IsTracing)
			{
				var trace = new Trace(TraceType.WordSynthesis, lang) {Input = input.DeepClone()};
				var curTrace = (Trace) input.CurrentTrace;
				curTrace.Children.Last.Children.Add(trace);
				input.CurrentTrace = trace;
			}
		}

		public void BeginApplyStratum(Stratum stratum, Word input)
		{
			if (IsTracing)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.StratumSynthesisInput, stratum) {Input = input});
		}

		public void EndApplyStratum(Stratum stratum, Word output)
		{
			if (IsTracing)
				((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.StratumSynthesisOutput, stratum) {Output = output});
		}

		public void PhonologicalRuleApplied(IPhonologicalRule rule, int subruleIndex, Word output)
		{
			if (IsTracing)
				((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.PhonologicalRuleSynthesis, rule) {SubruleIndex = subruleIndex, Output = output.DeepClone()});
		}

		public void PhonologicalRuleNotApplied(IPhonologicalRule rule, int subruleIndex, Word input, FailureReason reason)
		{
			if (IsTracing)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.PhonologicalRuleSynthesis, rule) {SubruleIndex = subruleIndex, Input = input.DeepClone(), FailureReason = reason});
		}

		public void BeginApplyTemplate(AffixTemplate template, Word input)
		{
			if (IsTracing)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.TemplateSynthesisInput, template) {Input = input});
		}

		public void EndApplyTemplate(AffixTemplate template, Word output, bool applied)
		{
			if (IsTracing)
				((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.TemplateSynthesisOutput, template) {Output = applied ? output : null});
		}

		public void MorphologicalRuleApplied(IMorphologicalRule rule, int subruleIndex, Word input, Word output)
		{
			if (IsTracing)
			{
				var trace = new Trace(TraceType.MorphologicalRuleSynthesis, rule) {SubruleIndex = subruleIndex, Input = input, Output = output};
				((Trace) output.CurrentTrace).Children.Add(trace);
				output.CurrentTrace = trace;
			}
		}

		public void MorphologicalRuleNotApplied(IMorphologicalRule rule, int subruleIndex, Word input, FailureReason reason)
		{
			if (IsTracing)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.MorphologicalRuleSynthesis, rule) {SubruleIndex = subruleIndex, Input = input, FailureReason = reason});
		}

		public void Blocking(IHCRule rule, Word output)
		{
			if (IsTracing)
				((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.Blocking, rule) {Output = output});
		}

		public void ParseSuccessful(Language lang, Word word)
		{
			if (IsTracing)
				((Trace) word.CurrentTrace).Children.Add(new Trace(TraceType.ParseSuccessful, lang) {Output = word});
		}

		public void ParseFailed(Language lang, Word word, FailureReason reason, Allomorph allomorph)
		{
			if (IsTracing)
				((Trace) word.CurrentTrace).Children.Add(new Trace(TraceType.ParseFailed, lang) {Output = word, FailureReason = reason});
		}
	}
}
