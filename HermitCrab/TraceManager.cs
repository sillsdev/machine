namespace SIL.HermitCrab
{
	public class TraceManager : TraceManagerBase
	{
		public override void BeginAnalyzeWord(Language lang, Word input)
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

		public override void BeginUnapplyPhonologicalRule(IPhonologicalRule rule, Word input)
		{
			if (TracePhonologicalRules)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.PhonologicalRuleAnalysis, rule) {Input = input.DeepClone()});
		}

		public override void EndUnapplyPhonologicalRule(IPhonologicalRule rule, Word output)
		{
			if (TracePhonologicalRules)
				((Trace) output.CurrentTrace).Children.Last.Output = output.DeepClone();
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

		public override void BeginSynthesizeWord(Language lang, Word input)
		{
			if (TraceLexicalLookup)
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

		public override void BeginApplyPhonologicalRule(IPhonologicalRule rule, Word input)
		{
			if (TracePhonologicalRules)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.PhonologicalRuleSynthesis, rule) {Input = input.DeepClone()});
		}

		public override void EndApplyPhonologicalRule(IPhonologicalRule rule, Word output)
		{
			if (TracePhonologicalRules)
				((Trace) output.CurrentTrace).Children.Last.Output = output.DeepClone();
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

		public override void MorphologicalRuleNotApplied(IMorphologicalRule rule, Word input)
		{
			if (TraceMorphologicalRules)
				((Trace) input.CurrentTrace).Children.Add(new Trace(TraceType.MorphologicalRuleSynthesis, rule) {Input = input});
		}

		public override void Blocking(IHCRule rule, Word output)
		{
			if (TraceBlocking)
				((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.Blocking, rule) {Output = output});
		}

		public override void ReportSuccess(Language lang, Word output)
		{
			if (TraceSuccess)
				((Trace) output.CurrentTrace).Children.Add(new Trace(TraceType.ReportSuccess, lang) {Output = output});
		}
	}
}
