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
		StemName,
		PartialParse,
		BoundRoot
	}

	public interface ITraceManager
	{
		bool IsTracing { get; set; }

		void AnalyzeWord(Language lang, Word input);

		void BeginUnapplyStratum(Stratum stratum, Word input);
		void EndUnapplyStratum(Stratum stratum, Word output);

		void PhonologicalRuleUnapplied(IPhonologicalRule rule, Word input, Word output);
		void PhonologicalRuleNotUnapplied(IPhonologicalRule rule, Word input);

		void BeginUnapplyTemplate(AffixTemplate template, Word input);
		void EndUnapplyTemplate(AffixTemplate template, Word output, bool unapplied);

		void MorphologicalRuleUnapplied(IMorphologicalRule rule, Word input, Word output, Allomorph allomorph);
		void MorphologicalRuleNotUnapplied(IMorphologicalRule rule, Word input);

		void LexicalLookup(Stratum stratum, Word input);

		void SynthesizeWord(Language lang, Word input);

		void BeginApplyStratum(Stratum stratum, Word input);
		void EndApplyStratum(Stratum stratum, Word output);

		void PhonologicalRuleApplied(IPhonologicalRule rule, Word input, Word output);
		void PhonologicalRuleNotApplied(IPhonologicalRule rule, Word input, FailureReason reason);

		void BeginApplyTemplate(AffixTemplate template, Word input);
		void EndApplyTemplate(AffixTemplate template, Word output, bool applied);

		void MorphologicalRuleApplied(IMorphologicalRule rule, Word input, Word output, Allomorph allomorph);
		void MorphologicalRuleNotApplied(IMorphologicalRule rule, Word input, FailureReason reason);

		void Blocking(IHCRule rule, Word output);

		void ParseSuccessful(Language lang, Word word);
		void ParseFailed(Language lang, Word word, FailureReason reason, Allomorph allomorph);
	}
}
