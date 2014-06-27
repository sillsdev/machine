namespace SIL.HermitCrab
{
	public abstract class TraceManagerBase
	{
		public bool IsTracing
		{
			get
			{
				return TraceBlocking || TraceLexicalLookup || TraceSuccess || TraceMorphologicalRules
					|| TracePhonologicalRules || TraceStrata || TraceTemplates;
			}
		}

		public bool TraceAll
		{
			get
			{
				return TraceBlocking && TraceLexicalLookup && TraceSuccess && TraceMorphologicalRules
					&& TracePhonologicalRules && TraceStrata && TraceTemplates;
			}

			set
			{
				TraceBlocking = value;
				TraceLexicalLookup = value;
				TraceSuccess = value;
				TraceMorphologicalRules = value;
				TracePhonologicalRules = value;
				TraceStrata = value;
				TraceTemplates = value;
			}
		}

		public bool TraceLexicalLookup { get; set; }
		public bool TraceSuccess { get; set; }
		public bool TraceBlocking { get; set; }
		public bool TraceMorphologicalRules { get; set; }
		public bool TracePhonologicalRules { get; set; }
		public bool TraceStrata { get; set; }
		public bool TraceTemplates { get; set; }

		public abstract void BeginAnalyzeWord(Language lang, Word input);

		public abstract void BeginUnapplyStratum(Stratum stratum, Word input);
		public abstract void EndUnapplyStratum(Stratum stratum, Word output);

		public abstract void BeginUnapplyPhonologicalRule(IPhonologicalRule rule, Word input);
		public abstract void EndUnapplyPhonologicalRule(IPhonologicalRule rule, Word output);

		public abstract void BeginUnapplyTemplate(AffixTemplate template, Word input);
		public abstract void EndUnapplyTemplate(AffixTemplate template, Word output, bool unapplied);

		public abstract void MorphologicalRuleUnapplied(IMorphologicalRule rule, Word input, Word output, Allomorph allomorph);
		public abstract void MorphologicalRuleNotUnapplied(IMorphologicalRule rule, Word input);

		public abstract void LexicalLookup(Stratum stratum, Word input);

		public abstract void BeginSynthesizeWord(Language lang, Word input);

		public abstract void BeginApplyStratum(Stratum stratum, Word input);
		public abstract void EndApplyStratum(Stratum stratum, Word output);

		public abstract void BeginApplyPhonologicalRule(IPhonologicalRule rule, Word input);
		//public abstract void PhonologicalRuleNotApplicablePOS(WordSynthesis input, HCObjectSet<PartOfSpeech> requiredPOSs);
		//public abstract void PhonologicalRuleNotApplicableMPRFeatures(MPRFeaturesType type, WordSynthesis input, MPRFeatureSet mprFeatures);
		public abstract void EndApplyPhonologicalRule(IPhonologicalRule rule, Word output);

		public abstract void BeginApplyTemplate(AffixTemplate template, Word input);
		public abstract void EndApplyTemplate(AffixTemplate template, Word output, bool applied);

		public abstract void MorphologicalRuleApplied(IMorphologicalRule rule, Word input, Word output, Allomorph allomorph);
		public abstract void MorphologicalRuleNotApplied(IMorphologicalRule rule, Word input);

		//public abstract void MorphCooccurrenceRuleFailed(MorphCoOccurrence cooccurrence, string usage, WordSynthesis input);

		public abstract void Blocking(IHCRule rule, Word output);
		public abstract void ReportSuccess(Language lang, Word output);
	}
}
