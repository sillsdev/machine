using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.WhyNot;

public record MorphRef(string MorphemeId, int AllomorphIndex, string Gloss);

public record FailedEvent(
    int Sequence,
    FailureReason Reason,
    Allomorph Allomorph,
    object FailureObject,
    Word Word,
    IReadOnlyList<MorphRef> Morphs
);

public record SuccessfulEvent(Word Word, IReadOnlyList<MorphRef> Morphs);

/// <summary>
/// Records every validity failure and success, keeping the allomorph and failure object that the stock
/// <see cref="TraceManager"/> drops. Safe to share across the parallel stages of a <see cref="Morpher"/>.
/// </summary>
public class CapturingTraceManager : ITraceManager
{
    private readonly object _lock = new object();
    private readonly List<FailedEvent> _failures = new List<FailedEvent>();
    private readonly List<SuccessfulEvent> _successes = new List<SuccessfulEvent>();

    public bool IsTracing { get; set; } = true;

    public IReadOnlyList<FailedEvent> Failures
    {
        get
        {
            lock (_lock)
                return _failures.ToArray();
        }
    }

    public IReadOnlyList<SuccessfulEvent> Successes
    {
        get
        {
            lock (_lock)
                return _successes.ToArray();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _failures.Clear();
            _successes.Clear();
        }
    }

    public static IReadOnlyList<MorphRef> MorphsOf(Word word)
    {
        return word
            .AllomorphsInMorphOrder.Select(a => new MorphRef(MorphemeIds.Of(a.Morpheme), a.Index, a.Morpheme.Gloss))
            .ToArray();
    }

    public void Failed(Language lang, Word word, FailureReason reason, Allomorph allomorph, object failureObj)
    {
        IReadOnlyList<MorphRef> morphs = MorphsOf(word);
        lock (_lock)
            _failures.Add(new FailedEvent(_failures.Count, reason, allomorph, failureObj, word, morphs));
    }

    public void Successful(Language lang, Word word)
    {
        IReadOnlyList<MorphRef> morphs = MorphsOf(word);
        lock (_lock)
            _successes.Add(new SuccessfulEvent(word, morphs));
    }

    public object GenerateWords(Language lang) => null;

    public void AnalyzeWord(Language lang, Word input) { }

    public void BeginUnapplyStratum(Stratum stratum, Word input) { }

    public void EndUnapplyStratum(Stratum stratum, Word output) { }

    public void PhonologicalRuleUnapplied(IPhonologicalRule rule, int subruleIndex, Word input, Word output) { }

    public void PhonologicalRuleNotUnapplied(IPhonologicalRule rule, int subruleIndex, Word input) { }

    public void BeginUnapplyTemplate(AffixTemplate template, Word input) { }

    public void EndUnapplyTemplate(
        AffixTemplate template,
        Word output,
        bool unapplied,
        FailureReason reason,
        object failureObj
    ) { }

    public void MorphologicalRuleUnapplied(IMorphologicalRule rule, int subruleIndex, Word input, Word output) { }

    public void MorphologicalRuleNotUnapplied(IMorphologicalRule rule, int subruleIndex, Word input) { }

    public void CompoundingRuleNotUnapplied(
        IMorphologicalRule rule,
        int subruleIndex,
        Word input,
        FailureReason reason,
        object failureObj
    ) { }

    public void CompoundingRuleNotApplied(
        IMorphologicalRule rule,
        int subruleIndex,
        Word input,
        FailureReason reason,
        object failureObj
    ) { }

    public void LexicalLookup(Stratum stratum, Word input) { }

    public void SynthesizeWord(Language lang, Word input) { }

    public void BeginApplyStratum(Stratum stratum, Word input) { }

    public void NonFinalTemplateAppliedLast(Stratum stratum, Word word) { }

    public void ApplicableTemplatesNotApplied(Stratum stratum, Word word) { }

    public void EndApplyStratum(Stratum stratum, Word output) { }

    public void PhonologicalRuleApplied(IPhonologicalRule rule, int subruleIndex, Word input, Word output) { }

    public void PhonologicalRuleNotApplied(
        IPhonologicalRule rule,
        int subruleIndex,
        Word input,
        FailureReason reason,
        object failureObj
    ) { }

    public void BeginApplyTemplate(AffixTemplate template, Word input) { }

    public void EndApplyTemplate(AffixTemplate template, Word output, bool applied) { }

    public void MorphologicalRuleApplied(IMorphologicalRule rule, int subruleIndex, Word input, Word output) { }

    public void MorphologicalRuleNotApplied(
        IMorphologicalRule rule,
        int subruleIndex,
        Word input,
        FailureReason reason,
        object failureObj
    ) { }

    public void Blocked(IHCRule rule, Word output) { }
}

public static class MorphemeIds
{
    public static string Of(Morpheme morpheme) => morpheme.Id ?? morpheme.Gloss;
}
