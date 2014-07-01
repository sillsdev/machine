namespace SIL.HermitCrab
{
	public class TraceManager : TraceManagerBase
	{
		public override void AnalyzeWord(Language lang, Word input)
		{
			if (IsTracing)
				input.CurrentTrace = new Trace(TraceType.WordAnalysis, lang) {Input = input};
		}

		public override void BeginUnapplyStratum(Stratum stratum, Word input)
		{
			if (TraceStrata)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.StratumAnalysisInput, stratum) {Input = input});
		}

		public override void EndUnapplyStratum(Stratum stratum, Word output)
		{
			if (TraceStrata)
				((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.StratumAnalysisOutput, stratum) {Output = output});
		}

		public override void PhonologicalRuleUnapplied(IPhonologicalRule rule, Word input, Word output)
		{
			if (TracePhonologicalRules)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.PhonologicalRuleAnalysis, rule) {Input = input.DeepClone(), Output = output.DeepClone()});
		}

		public override void PhonologicalRuleNotUnapplied(IPhonologicalRule rule, Word input)
		{
			if (TracePhonologicalRules)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.PhonologicalRuleAnalysis, rule) {Input = input.DeepClone()});
		}

		public override void BeginUnapplyTemplate(AffixTemplate template, Word input)
		{
			if (TraceTemplates)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.TemplateAnalysisInput, template) {Input = input});
		}

		public override void EndUnapplyTemplate(AffixTemplate template, Word output, bool unapplied)
		{
			if (TraceTemplates)
				((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.TemplateAnalysisOutput, template) {Output = unapplied ? output : null});
		}

		public override void MorphologicalRuleUnapplied(IMorphologicalRule rule, Word input, Word output, Allomorph allomorph)
		{
			if (TraceMorphologicalRules)
			{
				var trace = new Trace(TraceType.MorphologicalRuleAnalysis, rule) {Input = input, Output = output};
				((Trace) output.CurrentTrace).Children.Add(trace);
				output.CurrentTrace = trace;
			}
		}

		public override void MorphologicalRuleNotUnapplied(IMorphologicalRule rule, Word input)
		{
			if (TraceMorphologicalRules)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.MorphologicalRuleAnalysis, rule) {Input = input});
		}

		public override void LexicalLookup(Stratum stratum, Word input)
		{
			if (TraceLexicalLookup)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.LexicalLookup, stratum) {Input = input.DeepClone()});
		}

		public override void SynthesizeWord(Language lang, Word input)
		{
			if (IsTracing)
			{
				var trace = new Trace(TraceType.WordSynthesis, lang) {Input = input.DeepClone()};
				var curTrace = (Trace) input.CurrentTrace;
				curTrace.Children.Last.Children.Add(trace);
				input.CurrentTrace = trace;
			}
		}

		public override void BeginApplyStratum(Stratum stratum, Word input)
		{
			if (TraceStrata)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.StratumSynthesisInput, stratum) {Input = input});
		}

		public override void EndApplyStratum(Stratum stratum, Word output)
		{
			if (TraceStrata)
				((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.StratumSynthesisOutput, stratum) {Output = output});
		}

		public override void PhonologicalRuleApplied(IPhonologicalRule rule, Word input, Word output)
		{
			if (TracePhonologicalRules)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.PhonologicalRuleSynthesis, rule) {Input = input.DeepClone(), Output = output.DeepClone()});
		}

		public override void PhonologicalRuleNotApplied(IPhonologicalRule rule, Word input, FailureReason reason)
		{
			if (TracePhonologicalRules)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.PhonologicalRuleSynthesis, rule) {Input = input.DeepClone(), FailureReason = reason});
		}

		public override void BeginApplyTemplate(AffixTemplate template, Word input)
		{
			if (TraceTemplates)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.TemplateSynthesisInput, template) {Input = input});
		}

		public override void EndApplyTemplate(AffixTemplate template, Word output, bool applied)
		{
			if (TraceTemplates)
				((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.TemplateSynthesisOutput, template) {Output = applied ? output : null});
		}

		public override void MorphologicalRuleApplied(IMorphologicalRule rule, Word input, Word output, Allomorph allomorph)
		{
			if (TraceMorphologicalRules)
			{
				var trace = new Trace(TraceType.MorphologicalRuleSynthesis, rule) {Input = input, Output = output};
				((Trace) output.CurrentTrace).Children.Add(trace);
				output.CurrentTrace = trace;
			}
		}

		public override void MorphologicalRuleNotApplied(IMorphologicalRule rule, Word input, FailureReason reason)
		{
			if (TraceMorphologicalRules)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.MorphologicalRuleSynthesis, rule) {Input = input, FailureReason = reason});
		}

		public override void Blocking(IHCRule rule, Word output)
		{
			if (TraceBlocking)
				((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.Blocking, rule) {Output = output});
		}

		public override void ParseSuccessful(Language lang, Word word)
		{
			if (IsTracing)
				((Trace) word.CurrentTrace).Children.Add(new Trace(TraceType.ParseSuccessful, lang) {Output = word});
		}

		public override void ParseFailed(Language lang, Word word, FailureReason reason, Allomorph allomorph)
		{
			if (IsTracing)
				((Trace) word.CurrentTrace).Children.Add(new Trace(TraceType.ParseFailed, lang) {Output = word, FailureReason = reason});
		}
	}
}
