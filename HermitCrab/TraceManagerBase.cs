namespace SIL.HermitCrab
{
	public enum FailureReason
	{
		None,
		ObligatorySyntacticFeatures,
		RequiredAllomorphCoOccurrences,
		ExcludedAllomorphCoOccurrences,
		RequiredEnvironments,
		ExcludedEnvironments,
		RequiredMorphemeCoOccurrences,
		ExcludedMorphemeCoOccurrences,
		DisjunctiveAllomorph,
		SurfaceFormMismatch,
		SubruleMismatch,
		PatternMismatch,
		RequiredSyntacticFeatureStruct,
		HeadRequiredSyntacticFeatureStruct,
		NonHeadRequiredSyntacticFeatureStruct,
		StemName
	}

	public abstract class TraceManagerBase
	{
		public bool IsTracing
		{
			get
			{
				return TraceBlocking || TraceLexicalLookup || TraceMorphologicalRules
					|| TracePhonologicalRules || TraceStrata || TraceTemplates;
			}
		}

		public bool TraceAll
		{
			get
			{
				return TraceBlocking && TraceLexicalLookup && TraceMorphologicalRules
					&& TracePhonologicalRules && TraceStrata && TraceTemplates;
			}

			set
			{
				TraceBlocking = value;
				TraceLexicalLookup = value;
				TraceMorphologicalRules = value;
				TracePhonologicalRules = value;
				TraceStrata = value;
				TraceTemplates = value;
			}
		}

		public bool TraceLexicalLookup { get; set; }
		public bool TraceBlocking { get; set; }
		public bool TraceMorphologicalRules { get; set; }
		public bool TracePhonologicalRules { get; set; }
		public bool TraceStrata { get; set; }
		public bool TraceTemplates { get; set; }

		public abstract void AnalyzeWord(Language lang, Word input);

		public abstract void BeginUnapplyStratum(Stratum stratum, Word input);
		public abstract void EndUnapplyStratum(Stratum stratum, Word output);

		public abstract void PhonologicalRuleUnapplied(IPhonologicalRule rule, Word input, Word output);
		public abstract void PhonologicalRuleNotUnapplied(IPhonologicalRule rule, Word input);

		public abstract void BeginUnapplyTemplate(AffixTemplate template, Word input);
		public abstract void EndUnapplyTemplate(AffixTemplate template, Word output, bool unapplied);

		public abstract void MorphologicalRuleUnapplied(IMorphologicalRule rule, Word input, Word output, Allomorph allomorph);
		public abstract void MorphologicalRuleNotUnapplied(IMorphologicalRule rule, Word input);

		public abstract void LexicalLookup(Stratum stratum, Word input);

		public abstract void SynthesizeWord(Language lang, Word input);

		public abstract void BeginApplyStratum(Stratum stratum, Word input);
		public abstract void EndApplyStratum(Stratum stratum, Word output);

		public abstract void PhonologicalRuleApplied(IPhonologicalRule rule, Word input, Word output);
		public abstract void PhonologicalRuleNotApplied(IPhonologicalRule rule, Word input, FailureReason reason);

		public abstract void BeginApplyTemplate(AffixTemplate template, Word input);
		public abstract void EndApplyTemplate(AffixTemplate template, Word output, bool applied);

		public abstract void MorphologicalRuleApplied(IMorphologicalRule rule, Word input, Word output, Allomorph allomorph);
		public abstract void MorphologicalRuleNotApplied(IMorphologicalRule rule, Word input, FailureReason reason);

		public abstract void Blocking(IHCRule rule, Word output);

		public abstract void ParseSuccessful(Language lang, Word word);
		public abstract void ParseFailed(Language lang, Word word, FailureReason reason, Allomorph allomorph);
	}
}
