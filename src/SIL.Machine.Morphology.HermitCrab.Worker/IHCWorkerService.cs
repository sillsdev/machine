using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace SIL.Machine.Morphology.HermitCrab.Worker
{
    // Namespace/Name are pinned explicitly so FieldWorks' client-side copy of this contract
    // (necessarily a separate CLR type in a separate repo/assembly - FieldWorks consumes
    // SIL.Machine.Morphology.HermitCrab.Worker as a sibling exe, not a referenced library) is
    // wire-compatible as long as its attributes match these values. See
    // RUSTIFY-fieldworks-worker-design.md §3.
    [ServiceContract(Namespace = "http://sil.org/machine/hermitcrab/worker", Name = "IHCWorkerService")]
    public interface IHCWorkerService
    {
        /// <summary>
        /// Rebuilds the worker's Morpher from a grammar produced by
        /// <see cref="XmlLanguageWriter.Save"/> on the FieldWorks side (the same HC.NET XML
        /// input format <see cref="XmlLanguageLoader"/> already knows how to read - no new
        /// serialization format, no changes to SIL.Machine.Morphology.HermitCrab itself).
        /// Idempotent and cheap enough to call defensively before every bulk run.
        /// </summary>
        [OperationContract]
        void UpdateGrammar(HCGrammarDto grammar);

        /// <summary>Single-word interactive path (was: m_morpher.ParseWord in HCParser.cs).</summary>
        [OperationContract]
        WordAnalysisDto[] ParseWord(string word, bool guessRoots);

        /// <summary>
        /// Bulk path (was: m_parser.ParseWord(word) per-word inside
        /// ParserWorker.ParseAndUpdateWordform, invoked under a Parallel.ForEach the caller no
        /// longer needs). One round trip for the whole batch; the worker parses it with its own
        /// internal parallelism (Server GC, no artificial DOP cap).
        /// </summary>
        [OperationContract]
        IDictionary<string, WordAnalysisDto[]> ParseWordsBatch(string[] words, bool guessRoots);
    }

    [DataContract(Namespace = "http://sil.org/machine/hermitcrab/worker")]
    public class HCGrammarDto
    {
        [DataMember]
        public string CompiledGrammarXml { get; set; }

        [DataMember]
        public int DeletionReapplications { get; set; }

        [DataMember]
        public int MaxStemCount { get; set; }

        [DataMember]
        public bool MergeEquivalentAnalyses { get; set; }
    }

    /// <summary>
    /// One parse candidate for a word. Deliberately NOT a flat gloss/category projection: the
    /// FieldWorks-side consumer (HCParser.GetMorphs) maps each morph back to a live LCM object
    /// (IMoForm/IMoMorphSynAnalysis/ILexEntryInflType) via ids that only exist in the LCM-built
    /// grammar's extensible Properties bags - ids the worker's independently-loaded copy of the
    /// grammar still carries (XmlLanguageWriter/XmlLanguageLoader round-trip Properties
    /// generically, see WriteProperties/LoadProperties), but that FieldWorks alone can resolve to
    /// objects (the worker has no LCM cache). So the DTO carries raw ids/strings per morph, in
    /// the order they occur in the word, and FieldWorks runs the LCM-lookup half of the old
    /// GetMorphs algorithm over this list instead of over the live HermitCrab Word.
    /// </summary>
    [DataContract(Namespace = "http://sil.org/machine/hermitcrab/worker")]
    public class WordAnalysisDto
    {
        [DataMember]
        public MorphDto[] Morphs { get; set; }
    }

    [DataContract(Namespace = "http://sil.org/machine/hermitcrab/worker")]
    public class MorphDto
    {
        /// <summary>Allomorph.Properties[HCParser.FormID] ("ID"). 0 if absent (skip, per GetMorphs).</summary>
        [DataMember]
        public int FormId { get; set; }

        /// <summary>Allomorph.Properties[HCParser.FormID2] ("ID2"). 0 if absent.</summary>
        [DataMember]
        public int FormId2 { get; set; }

        /// <summary>Whether this allomorph is an AffixProcessAllomorph (drives circumfix detection).</summary>
        [DataMember]
        public bool IsAffixProcessAllomorph { get; set; }

        /// <summary>The surface form string for this morph's shape range.</summary>
        [DataMember]
        public string FormStr { get; set; }

        /// <summary>Allomorph.Guessed.</summary>
        [DataMember]
        public bool Guessed { get; set; }

        /// <summary>Morpheme.Properties[HCParser.MsaID] ("ID"), parsed to int.</summary>
        [DataMember]
        public int MsaId { get; set; }

        /// <summary>Morpheme.Properties[HCParser.InflTypeID]. 0 if absent.</summary>
        [DataMember]
        public int InflTypeId { get; set; }

        /// <summary>
        /// Index of this morph's owning Morpheme, stable within one WordAnalysisDto and shared
        /// by two MorphDtos when (and only when) they came from the same Morpheme instance - the
        /// wire-safe replacement for GetMorphs' `Dictionary&lt;Morpheme, MorphInfo&gt;` reference-identity
        /// lookup (used to detect the second occurrence of a circumfix).
        /// </summary>
        [DataMember]
        public int MorphemeIndex { get; set; }
    }
}
