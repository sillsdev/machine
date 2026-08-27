using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    public class Word : Freezable<Word>, IAnnotatedData<ShapeNode>, ICloneable<Word>
    {
        public const string RootMorphID = "ROOT";

        private readonly Dictionary<string, Allomorph> _allomorphs;
        private RootAllomorph _rootAllomorph;
        private Shape _shape;
        private readonly List<IMorphologicalRule> _mruleApps;
        private int _mruleAppIndex = -1;
        private readonly Dictionary<IMorphologicalRule, int> _mrulesUnapplied;
        private readonly Dictionary<IMorphologicalRule, int> _mrulesApplied;
        private readonly List<Word> _nonHeadApps;
        private int _nonHeadAppIndex = -1;
        private readonly MprFeatureSet _mprFeatures;
        private readonly IDBearerSet<Feature> _obligatorySyntacticFeatures;
        private FeatureStruct _realizationalFS;
        private Stratum _stratum;
        private bool? _isLastAppliedRuleFinal;
        private bool _isPartial;
        private readonly Dictionary<string, HashSet<int>> _disjunctiveAllomorphIndices;
        private int _mruleAppCount = 0;
        private readonly IList<Word> _alternatives = new List<Word>();

        public Word(RootAllomorph rootAllomorph, FeatureStruct realizationalFS)
        {
            _allomorphs = new Dictionary<string, Allomorph>();
            _mprFeatures = new MprFeatureSet();
            _shape = rootAllomorph.Segments.Shape.Clone();
            ResetDirty();
            SetRootAllomorph(rootAllomorph);
            RealizationalFeatureStruct = realizationalFS;
            _mruleApps = new List<IMorphologicalRule>();
            _mrulesUnapplied = new Dictionary<IMorphologicalRule, int>();
            _mrulesApplied = new Dictionary<IMorphologicalRule, int>();
            _nonHeadApps = new List<Word>();
            _obligatorySyntacticFeatures = new IDBearerSet<Feature>();
            _isLastAppliedRuleFinal = null;
            _disjunctiveAllomorphIndices = new Dictionary<string, HashSet<int>>();
        }

        public Word(Stratum stratum, Shape shape)
        {
            _allomorphs = new Dictionary<string, Allomorph>();
            Stratum = stratum;
            _shape = shape;
            ResetDirty();
            SyntacticFeatureStruct = new FeatureStruct();
            RealizationalFeatureStruct = new FeatureStruct();
            _mprFeatures = new MprFeatureSet();
            _mruleApps = new List<IMorphologicalRule>();
            _mrulesUnapplied = new Dictionary<IMorphologicalRule, int>();
            _mrulesApplied = new Dictionary<IMorphologicalRule, int>();
            _nonHeadApps = new List<Word>();
            _obligatorySyntacticFeatures = new IDBearerSet<Feature>();
            _isLastAppliedRuleFinal = null;
            _isPartial = false;
            _disjunctiveAllomorphIndices = new Dictionary<string, HashSet<int>>();
        }

        protected Word(Word word)
            : this(word, cloneNonHeadApps: true) { }

        // ReplayOnto passes false: it rebuilds the non-head list wholesale, so cloning it here would be
        // discarded work.
        private Word(Word word, bool cloneNonHeadApps)
        {
            _allomorphs = new Dictionary<string, Allomorph>(word._allomorphs);
            Stratum = word.Stratum;
            Source = word;
            // Don't copy Alternatives.
            _shape = word._shape.Clone();
            _rootAllomorph = word._rootAllomorph;
            SyntacticFeatureStruct = word.SyntacticFeatureStruct.Clone();
            RealizationalFeatureStruct = word.RealizationalFeatureStruct.Clone();
            _mprFeatures = word.MprFeatures.Clone();
            _mruleApps = new List<IMorphologicalRule>(word._mruleApps);
            _mruleAppIndex = word._mruleAppIndex;
            _mrulesUnapplied = new Dictionary<IMorphologicalRule, int>(word._mrulesUnapplied);
            _mrulesApplied = new Dictionary<IMorphologicalRule, int>(word._mrulesApplied);
            _nonHeadApps = cloneNonHeadApps ? new List<Word>(word._nonHeadApps.CloneItems()) : new List<Word>();
            _nonHeadAppIndex = word._nonHeadAppIndex;
            _obligatorySyntacticFeatures = new IDBearerSet<Feature>(word._obligatorySyntacticFeatures);
            _isLastAppliedRuleFinal = word._isLastAppliedRuleFinal;
            _isPartial = word._isPartial;
            CurrentTrace = word.CurrentTrace;
            AnalysisScope = word.AnalysisScope;
            SynthesisFoldScope = word.SynthesisFoldScope;
            _disjunctiveAllomorphIndices = word._disjunctiveAllomorphIndices.ToDictionary(
                kvp => kvp.Key,
                kvp => new HashSet<int>(kvp.Value)
            );
            _mruleAppCount = word._mruleAppCount;
        }

        public IEnumerable<Annotation<ShapeNode>> Morphs
        {
            get
            {
                var morphs = new List<Annotation<ShapeNode>>();
                foreach (Annotation<ShapeNode> ann in Annotations)
                {
                    ann.PostorderTraverse(a =>
                    {
                        if (a.Type() == HCFeatureSystem.Morph)
                            morphs.Add(a);
                    });
                }
                return morphs;
            }
        }

        // there can be multiple morphs for a single allomorph, but we only want to return an allomorph on its
        // first occurrence, so we use distinct
        public IEnumerable<Allomorph> AllomorphsInMorphOrder => Morphs.Select(GetAllomorph).Distinct();

        public ICollection<Allomorph> Allomorphs
        {
            get { return _allomorphs.Values; }
        }

        public RootAllomorph RootAllomorph
        {
            get { return _rootAllomorph; }
            internal set
            {
                CheckFrozen();
                _shape = value.Segments.Shape.Clone();
                SetRootAllomorph(value);
            }
        }

        private void SetRootAllomorph(RootAllomorph rootAllomorph)
        {
            _rootAllomorph = rootAllomorph;
            var entry = (LexEntry)_rootAllomorph.Morpheme;
            Stratum = entry.Stratum;
            MarkMorph(_shape, _rootAllomorph, RootMorphID);
            SyntacticFeatureStruct = entry.SyntacticFeatureStruct.Clone();
            _mprFeatures.Clear();
            _mprFeatures.UnionWith(entry.MprFeatures);
            _isPartial = entry.IsPartial;
        }

        public Shape Shape
        {
            get { return _shape; }
        }

        public FeatureStruct SyntacticFeatureStruct { get; internal set; }

        public FeatureStruct RealizationalFeatureStruct
        {
            get { return _realizationalFS; }
            internal set
            {
                CheckFrozen();
                _realizationalFS = value;
            }
        }

        public MprFeatureSet MprFeatures
        {
            get { return _mprFeatures; }
        }

        public ICollection<Feature> ObligatorySyntacticFeatures
        {
            get { return _obligatorySyntacticFeatures; }
        }

        public Range<ShapeNode> Range
        {
            get { return _shape.Range; }
        }

        public AnnotationList<ShapeNode> Annotations
        {
            get { return _shape.Annotations; }
        }

        public Stratum Stratum
        {
            get { return _stratum; }
            internal set
            {
                CheckFrozen();
                _stratum = value;
            }
        }

        public IEnumerable<Morpheme> MorphemesInApplicationOrder
        {
            get
            {
                yield return _rootAllomorph.Morpheme;
                int j = _nonHeadApps.Count - 1;
                for (int i = _mruleApps.Count - 1; i >= 0; i--)
                {
                    IMorphologicalRule rule = _mruleApps[i];
                    if (rule == null || rule is CompoundingRule)
                        yield return _nonHeadApps[j--].RootAllomorph.Morpheme;
                    else
                        yield return (MorphemicMorphologicalRule)rule;
                }
            }
        }

        public object CurrentTrace { get; set; }

        /// <summary>
        /// Carrier for the analysis-cascade memo. Reference-shared like
        /// <see cref="CurrentTrace"/> and excluded from <c>FreezeImpl</c>/<c>ValueEquals</c> for the same
        /// reason. Null while tracing, and for words not routed through
        /// <see cref="Morpher.ParseWord(string, out object)"/> at all, so readers must fall back to
        /// unmemoized behavior rather than throw. Cleared again on entry to synthesis so returned words do
        /// not pin the per-parse tables.
        /// </summary>
        internal AnalysisScope AnalysisScope { get; set; }

        /// <summary>
        /// Carrier for the synthesis fold-step memo (docs/hermitcrab-synthesis-fold-probes.md). Same
        /// contract as <see cref="AnalysisScope"/>: reference-shared through clones, excluded from
        /// <c>FreezeImpl</c>/<c>ValueEquals</c>, null unless <see cref="Morpher.UseSynthesisFoldMemo"/> is
        /// on and the parse is running sequentially and untraced. One instance per
        /// <see cref="Morpher.ParseWord(string, out object)"/> call, installed on every alternative that
        /// enters <c>_synthesisRule.Apply</c> in <c>Morpher.SynthesizeSequential</c> -- shared across every
        /// analysis word's alternatives for that one surface-word parse, which is exactly the scope P1c/N1
        /// measured sharing over.
        /// </summary>
        internal SynthesisFoldScope SynthesisFoldScope { get; set; }

        public bool IsPartial
        {
            get { return _isPartial; }
            internal set
            {
                CheckFrozen();
                _isPartial = value;
            }
        }

        public IEnumerable<IMorphologicalRule> MorphologicalRules
        {
            get { return _mruleApps; }
        }

        internal int MorphologicalRuleApplicationCount => _mruleAppCount;

        internal bool IsAllMorphologicalRulesApplied
        {
            get { return _mruleAppIndex == -1; }
        }

        internal bool IsMorphologicalRuleApplicable(IMorphologicalRule rule)
        {
            if (_mruleAppIndex < 0)
                return false;

            IMorphologicalRule curRule = _mruleApps[_mruleAppIndex];
            return curRule == rule || (curRule == null && rule is CompoundingRule);
        }

        internal bool HasRemainingRulesFromStratum(Stratum stratum)
        {
            if (_mruleAppIndex < 0)
                return false;

            IMorphologicalRule curRule = _mruleApps[_mruleAppIndex];
            if (curRule == null)
                return CurrentNonHead != null && CurrentNonHead.Stratum == stratum;
            return curRule.Stratum == stratum;
        }

        internal Annotation<ShapeNode> MarkMorph(IEnumerable<ShapeNode> nodes, Allomorph allomorph, string morphID)
        {
            ShapeNode[] nodeArray = nodes.ToArray();
            Annotation<ShapeNode> ann = null;
            if (nodeArray.Length > 0)
            {
                ann = new Annotation<ShapeNode>(
                    Range<ShapeNode>.Create(nodeArray[0], nodeArray[nodeArray.Length - 1]),
                    FeatureStruct
                        .New()
                        .Symbol(HCFeatureSystem.Morph)
                        .Feature(HCFeatureSystem.Allomorph)
                        .EqualTo(allomorph.ID)
                        .Feature(HCFeatureSystem.MorphID)
                        .EqualTo(morphID)
                        .Value
                );
                ann.Children.AddRange(nodeArray.Select(n => n.Annotation));
                _shape.Annotations.Add(ann, false);
            }
            _allomorphs[allomorph.ID] = allomorph;
            return ann;
        }

        internal Annotation<ShapeNode> MarkSubsumedMorph(
            Annotation<ShapeNode> morph,
            Allomorph allomorph,
            string morphID
        )
        {
            Annotation<ShapeNode> ann = new Annotation<ShapeNode>(
                morph.Range,
                FeatureStruct
                    .New()
                    .Symbol(HCFeatureSystem.Morph)
                    .Feature(HCFeatureSystem.Allomorph)
                    .EqualTo(allomorph.ID)
                    .Feature(HCFeatureSystem.MorphID)
                    .EqualTo(morphID)
                    .Value
            );
            morph.Children.Add(ann, false);
            _allomorphs[allomorph.ID] = allomorph;
            return ann;
        }

        internal void RemoveMorph(Annotation<ShapeNode> morphAnn)
        {
            var alloID = (string)morphAnn.FeatureStruct.GetValue(HCFeatureSystem.Allomorph);
            _allomorphs.Remove(alloID);
            foreach (ShapeNode node in _shape.GetNodes(morphAnn.Range).ToArray())
                node.Remove();
        }

        /// <summary>
        /// Notifies this word that the specified morphological rule was unapplied. Null
        /// indicates that an unknown compounding rule was unapplied. This is used when
        /// generating a compound word, because the compounding rule is usually not known just
        /// the non-head allomorph.
        /// <para>
        /// The trail push and the count increment below must stay in lockstep: <see cref="ReplayOnto"/>
        /// splits a stored result on the assumption that equal unapplication multisets imply equal trail
        /// lengths. Realizational rules incrementing the count without extending the trail is safe because
        /// they do so on both sides of any comparison; any other divergence misaligns the graft.
        /// </para>
        /// </summary>
        internal void MorphologicalRuleUnapplied(IMorphologicalRule mrule)
        {
            CheckFrozen();
            if (mrule != null)
                _mrulesUnapplied.UpdateValue(mrule, () => 0, count => count + 1);
            if (!(mrule is RealizationalAffixProcessRule))
            {
                _mruleApps.Add(mrule);
                _mruleAppIndex++;
            }
        }

        /// <summary>
        /// Gets the number of times the specified morphological rule has been unapplied.
        /// </summary>
        /// <param name="mrule">The morphological rule.</param>
        /// <returns>The number of unapplications.</returns>
        internal int GetUnapplicationCount(IMorphologicalRule mrule)
        {
            if (!_mrulesUnapplied.TryGetValue(mrule, out int numUnapplies))
                numUnapplies = 0;
            return numUnapplies;
        }

        /// <summary>
        /// The full multiset backing <see cref="GetUnapplicationCount"/>, for <see cref="AnalysisStateKey"/>.
        /// </summary>
        internal IReadOnlyDictionary<IMorphologicalRule, int> UnappliedRuleCounts => _mrulesUnapplied;

        /// <summary>
        /// Notifies this word synthesis that the specified morphological rule has applied.
        /// </summary>
        internal void MorphologicalRuleApplied(IMorphologicalRule mrule, IEnumerable<int> allomorphIndices = null)
        {
            CheckFrozen();
            if (IsMorphologicalRuleApplicable(mrule))
                _mruleAppIndex--;
            // indicate that the current non-head was applied if this is a compounding rule
            if (mrule is CompoundingRule)
                _nonHeadAppIndex--;
            _mrulesApplied.UpdateValue(mrule, () => 0, count => count + 1);
            if (allomorphIndices != null)
                _disjunctiveAllomorphIndices.GetOrCreate(_mruleAppCount.ToString()).UnionWith(allomorphIndices);
            _mruleAppCount++;
        }

        internal bool? IsLastAppliedRuleFinal
        {
            get { return _isLastAppliedRuleFinal; }
            set
            {
                CheckFrozen();
                _isLastAppliedRuleFinal = value;
            }
        }

        /// <summary>
        /// Gets the number of times the specified morphological rule has been applied.
        /// </summary>
        /// <param name="mrule">The morphological rule.</param>
        /// <returns>The number of applications.</returns>
        internal int GetApplicationCount(IMorphologicalRule mrule)
        {
            if (!_mrulesApplied.TryGetValue(mrule, out int numApplies))
                numApplies = 0;
            return numApplies;
        }

        /// <summary>
        /// The full application-count table backing <see cref="GetApplicationCount"/>. Read-only exposure
        /// for <see cref="SynthesisProbe"/>'s P1c fold-step fingerprint (see
        /// docs/hermitcrab-synthesis-fold-probes.md section 3) -- mirrors how <see cref="UnappliedRuleCounts"/>
        /// already exposes the analysis-side equivalent.
        /// </summary>
        internal IReadOnlyDictionary<IMorphologicalRule, int> AppliedRuleCounts => _mrulesApplied;

        /// <summary>
        /// The disjunctive-allomorph-application table, keyed by morph id. Read-only exposure for
        /// <see cref="SynthesisProbe"/>'s P1c fingerprint: <see cref="Allomorph.IsWordValid"/> reads this
        /// per morph, so two words that differ here can validly produce different outcomes even with
        /// everything else equal, and a fingerprint that omitted it would be exactly the kind of
        /// incomplete key <c>Word.ValueEquals</c> already is (see the class remarks at the top of this file's
        /// callers).
        /// </summary>
        internal IReadOnlyDictionary<string, HashSet<int>> DisjunctiveAllomorphIndices => _disjunctiveAllomorphIndices;

        /// <summary>
        /// The trail index a synthesis step reads via <see cref="IsMorphologicalRuleApplicable"/>: how far
        /// through <c>_mruleApps</c> this word has progressed. Exposed as "pending-trail position" for
        /// <see cref="SynthesisProbe"/>'s P1c fingerprint.
        /// </summary>
        internal int PendingTrailPosition => _mruleAppIndex;

        /// <summary>
        /// The full morphological-rule trail list, for <see cref="SynthesisStateKey"/>. Read together with
        /// <see cref="PendingTrailPosition"/>: entries at indices <c>0..PendingTrailPosition</c> are the
        /// still-pending trail content a synthesis step's continuation depends on (what
        /// <see cref="SynthesisProbe"/>'s P1c fingerprint omits -- position only, no content, see the plan
        /// doc's trap #1); entries past <c>PendingTrailPosition</c> are already consumed and never read
        /// again by any rule, only by <see cref="MorphemesInApplicationOrder"/> on a fully-finished result.
        /// This list itself never mutates during synthesis -- <see cref="MorphologicalRuleApplied"/> only
        /// moves <see cref="PendingTrailPosition"/> -- so exposing the live list is safe.
        /// </summary>
        internal IReadOnlyList<IMorphologicalRule> MorphologicalRuleTrail => _mruleApps;

        internal Word CurrentNonHead
        {
            get
            {
                if (_nonHeadAppIndex == -1)
                    return null;
                return _nonHeadApps[_nonHeadAppIndex];
            }
        }

        internal int NonHeadCount
        {
            get { return _nonHeadApps.Count; }
        }

        internal IReadOnlyList<Word> NonHeads => _nonHeadApps;

        /// <summary>
        /// Length of the morphological-rule trail so far. Recorded with <see cref="NonHeadCount"/> when a
        /// subtree is memoized, to mark where a replayed result's kept suffix begins; see
        /// <see cref="ReplayOnto"/>.
        /// </summary>
        internal int MorphologicalRuleTrailLength => _mruleApps.Count;

        internal void NonHeadUnapplied(Word nonHead)
        {
            CheckFrozen();
            _nonHeadApps.Add(nonHead);
            _nonHeadAppIndex++;
        }

        internal Word Source { get; set; }

        internal IList<Word> Alternatives
        {
            get { return _alternatives; }
        }

        internal IList<Word> ExpandAlternatives()
        {
            IList<Word> alternatives = new List<Word>();
            IList<Word> originals = Source?.ExpandAlternatives();
            // Update the alternatives of Source with any changes made since Source.
            if (originals == null || originals.Count < 2)
            {
                // Special case.
                alternatives.Add(this);
            }
            else
            {
                foreach (Word original in originals)
                {
                    Word alternative = original.Clone();
                    alternative._shape = this.Shape;
                    // Add new rules to alternative.
                    int m_start = Source == null ? 0 : Source._mruleApps.Count();
                    for (int i = m_start; i < _mruleApps.Count(); i++)
                        alternative.MorphologicalRuleUnapplied(_mruleApps[i]);
                    int nh_start = Source == null ? 0 : Source._nonHeadApps.Count();
                    for (int i = nh_start; i < _nonHeadApps.Count(); i++)
                        alternative.NonHeadUnapplied(_nonHeadApps[i]);
                    // Add changes to feature structures to alternative.
                    if (!_realizationalFS.ValueEquals(Source._realizationalFS))
                    {
                        FeatureStruct diff = _realizationalFS.Clone();
                        diff.Subtract(Source._realizationalFS);
                        FeatureStruct newFS;
                        alternative._realizationalFS.Unify(diff, out newFS);
                        alternative._realizationalFS = newFS;
                    }
                    if (RootAllomorph != Source.RootAllomorph)
                        alternative.RootAllomorph = RootAllomorph;
                    alternative.Freeze();
                    alternatives.Add(alternative);
                }
            }
            // Add local alternatives.
            foreach (Word alternative in _alternatives)
                alternatives.AddRange(alternative.ExpandAlternatives());
            return alternatives;
        }

        /// <summary>
        /// Re-parents this Word -- computed while exploring the subtree below some cascade node N -- onto
        /// <paramref name="queryNode"/>, which reached N's <see cref="AnalysisStateKey"/> via a different
        /// unapplication order.
        /// <para>
        /// Sound because an equal key means N and <paramref name="queryNode"/> agree on Shape, both
        /// FeatureStructs, the unapplication multiset and the non-head count, so everything computed
        /// inside the subtree is a function of state they share and carries over untouched. Only the two
        /// ordered structures the key reduces to counts -- the rule trail and the non-head list -- can
        /// differ, and only in the prefix accumulated before reaching N, which is what gets replaced.
        /// </para>
        /// </summary>
        /// <param name="queryNode">The word that hit the memo; its trail and non-heads become the prefix.</param>
        /// <param name="mruleTrailPrefixLength">
        /// N's <c>_mruleApps.Count</c> when its subtree was memoized: this word's trail from that index on
        /// is the subtree-local suffix to keep.
        /// </param>
        /// <param name="nonHeadPrefixLength">Same, for <c>_nonHeadApps</c>.</param>
        /// <param name="queryNonHeadPrefix">
        /// Pre-cloned non-heads from <paramref name="queryNode"/>, so one memo hit clones them once rather
        /// than per stored result; see <c>AnalysisScope.TryReplay</c>. Null clones them here instead.
        /// </param>
        internal Word ReplayOnto(
            Word queryNode,
            int mruleTrailPrefixLength,
            int nonHeadPrefixLength,
            IReadOnlyList<Word> queryNonHeadPrefix = null
        )
        {
            var clone = new Word(this, cloneNonHeadApps: false);

            List<IMorphologicalRule> mruleSuffix = clone._mruleApps.GetRange(
                mruleTrailPrefixLength,
                clone._mruleApps.Count - mruleTrailPrefixLength
            );
            clone._mruleApps.Clear();
            clone._mruleApps.AddRange(queryNode._mruleApps);
            clone._mruleApps.AddRange(mruleSuffix);
            clone._mruleAppIndex = clone._mruleApps.Count - 1;

            // The clone's non-head list starts empty, so it is built as query prefix + this word's
            // subtree-local suffix without ever cloning the prefix this word arrived with, which the graft
            // discards anyway.
            if (queryNonHeadPrefix != null)
                clone._nonHeadApps.AddRange(queryNonHeadPrefix);
            else
                clone._nonHeadApps.AddRange(queryNode._nonHeadApps.CloneItems());
            clone._nonHeadApps.AddRange(
                _nonHeadApps.GetRange(nonHeadPrefixLength, _nonHeadApps.Count - nonHeadPrefixLength).CloneItems()
            );
            clone._nonHeadAppIndex = clone._nonHeadApps.Count - 1;

            clone.Freeze();
            return clone;
        }

        // Hoisted out of the per-result loop by AnalysisScope.TryReplay; see ReplayOnto.
        internal List<Word> CloneNonHeadsForReplay()
        {
            return new List<Word>(_nonHeadApps.CloneItems());
        }

        /// <summary>
        /// Grafts a memoized synthesis fold-step result (<c>this</c>, produced by applying one rule to some
        /// <c>storedInput</c>) onto <paramref name="queryInput"/>, whose <see cref="SynthesisStateKey"/>
        /// equals <c>storedInput</c>'s. The single-step mirror of <see cref="ReplayOnto"/> -- see
        /// docs/hermitcrab-synthesis-fold-probes.md, "What to build" item 2.
        /// <para>
        /// Sound because <see cref="SynthesisStateKey"/> equality guarantees <c>storedInput</c> and
        /// <paramref name="queryInput"/> agree on everything the step read (Shape -- including every morph
        /// annotation's MorphID/Allomorph feature values, so this word's own already-marked
        /// <c>_allomorphs</c> and the new step's MorphID stamp are already correct as computed -- both
        /// FeatureStructs, MprFeatures, disjunctive-allomorph indices, per-rule applied counts, IsPartial,
        /// IsLastAppliedRuleFinal, Stratum, and the pending trail content) and on everything that field
        /// determines transitively (<c>ObligatorySyntacticFeatures</c> is a deterministic union over which
        /// rules have applied at least once, i.e. over <c>AppliedRuleCounts</c>' support). So this word's
        /// own computed content carries over UNCHANGED: it is exactly what re-running the step on
        /// <paramref name="queryInput"/> would have produced too.
        /// </para>
        /// <para>
        /// Two things do NOT transfer, because they are exactly what the key -- correctly -- does not pin
        /// down, and because neither of the two rule classes this memo covers
        /// (<see cref="MorphologicalRules.SynthesisAffixProcessRule"/>,
        /// <see cref="MorphologicalRules.SynthesisRealizationalAffixProcessRule"/>) touches them:
        /// <list type="bullet">
        /// <item>The already-consumed suffix of the morphological-rule trail (entries past
        /// <paramref name="queryInput"/>'s <see cref="PendingTrailPosition"/>). No rule reads it again, but
        /// <c>MorphemesInApplicationOrder</c> walks the whole list on a finished result, so it must be
        /// <paramref name="queryInput"/>'s own history, not the stored candidate's.</item>
        /// <item>The non-head list -- neither memoized rule class calls <c>NonHeadUnapplied</c>, so it must
        /// stay exactly what <paramref name="queryInput"/> already had.</item>
        /// </list>
        /// Both are spliced from <paramref name="queryInput"/>, this word's own copies discarded.
        /// </para>
        /// <para>
        /// <b>Blocking exception.</b> <c>Apply</c>'s <c>CheckBlocking</c> branch can replace a step's output
        /// with a wholly fresh <see cref="Word"/> built from a sibling <c>LexEntry</c> (<see cref="CheckBlocking"/>),
        /// which is born with an EMPTY morphological-rule trail and no reference to the input that produced
        /// it at all -- it is not a continuation, it overrides one. Reaching this method's call sites only
        /// happens once <see cref="IsMorphologicalRuleApplicable"/> has already required a non-empty trail on
        /// the real input, so an empty trail on <c>this</c> can only mean blocking fired, never a
        /// coincidentally-empty ordinary continuation. Such a result is already correct for any
        /// same-key query and is returned as-is, unspliced: grafting <paramref name="queryInput"/>'s trail
        /// onto it would fabricate morphemes <c>MorphemesInApplicationOrder</c> was never meant to report
        /// and could make <c>Morpher.IsWordValid</c>'s <c>IsAllMorphologicalRulesApplied</c> check reject a
        /// word the unmemoized engine would have accepted.
        /// </para>
        /// </summary>
        /// <param name="queryInput">
        /// The word that hit the memo. Its trail (advanced by exactly this step) and non-heads become this
        /// result's identity.
        /// </param>
        /// <param name="trailConsuming">
        /// True for a trail-driven rule (<see cref="MorphologicalRules.SynthesisAffixProcessRule"/>), which
        /// always advances <see cref="PendingTrailPosition"/> by one once
        /// <see cref="IsMorphologicalRuleApplicable"/> has already gated entry; false for a trail-exempt
        /// realizational rule (<see cref="MorphologicalRules.SynthesisRealizationalAffixProcessRule"/>),
        /// which never does. Mirrors exactly what <see cref="MorphologicalRuleApplied"/> itself would have
        /// done to <paramref name="queryInput"/>.
        /// </param>
        internal Word ReanchorSynthesisStep(Word queryInput, bool trailConsuming)
        {
            // See the "Blocking exception" remarks above.
            if (_mruleApps.Count == 0)
                return this;

            var clone = new Word(this, cloneNonHeadApps: false);

            clone._mruleApps.Clear();
            clone._mruleApps.AddRange(queryInput._mruleApps);
            clone._mruleAppIndex = trailConsuming ? queryInput._mruleAppIndex - 1 : queryInput._mruleAppIndex;

            clone._nonHeadApps.AddRange(queryInput._nonHeadApps.CloneItems());
            clone._nonHeadAppIndex = queryInput._nonHeadAppIndex;

            clone.CurrentTrace = queryInput.CurrentTrace;
            clone.AnalysisScope = queryInput.AnalysisScope;
            clone.SynthesisFoldScope = queryInput.SynthesisFoldScope;
            clone.Source = queryInput;

            clone.Freeze();
            return clone;
        }

        public Allomorph GetAllomorph(Annotation<ShapeNode> morph)
        {
            var alloID = (string)morph.FeatureStruct.GetValue(HCFeatureSystem.Allomorph);
            return _allomorphs[alloID];
        }

        internal IEnumerable<Annotation<ShapeNode>> GetMorphs(Allomorph allomorph)
        {
            return Morphs.Where(m => (string)m.FeatureStruct.GetValue(HCFeatureSystem.Allomorph) == allomorph.ID);
        }

        internal IEnumerable<int> GetDisjunctiveAllomorphApplications(Annotation<ShapeNode> morph)
        {
            var morphID = (string)morph.FeatureStruct.GetValue(HCFeatureSystem.MorphID);
            if (_disjunctiveAllomorphIndices.TryGetValue(morphID, out HashSet<int> indices))
                return indices;
            return null;
        }

        internal bool CheckBlocking(out Word word)
        {
            word = null;
            LexFamily family = ((LexEntry)RootAllomorph.Morpheme).Family;
            if (family == null)
                return false;

            foreach (LexEntry entry in family.Entries)
            {
                if (
                    entry != RootAllomorph.Morpheme
                    && entry.Stratum == Stratum
                    && SyntacticFeatureStruct.Subsumes(entry.SyntacticFeatureStruct)
                )
                {
                    word = new Word(entry.PrimaryAllomorph, RealizationalFeatureStruct.Clone())
                    {
                        CurrentTrace = CurrentTrace,
                    };
                    word.Freeze();
                    return true;
                }
            }

            return false;
        }

        internal void ResetDirty()
        {
            CheckFrozen();
            foreach (ShapeNode node in _shape)
                node.SetDirty(false);
        }

        internal IDictionary<int, Tuple<FailureReason, object>> CurrentRuleResults { get; set; }

        protected override int FreezeImpl()
        {
            int code = 23;
            _shape.Freeze();
            code = code * 31 + _shape.GetFrozenHashCode();
            _realizationalFS.Freeze();
            code = code * 31 + _realizationalFS.GetFrozenHashCode();
            foreach (Word nonHead in _nonHeadApps)
            {
                nonHead.Freeze();
                code = code * 31 + nonHead.GetFrozenHashCode();
            }
            code = code * 31 + _nonHeadAppIndex.GetHashCode();
            code = code * 31 + _stratum.GetHashCode();
            code = code * 31 + (_rootAllomorph == null ? 0 : _rootAllomorph.GetHashCode());
            code = code * 31 + _mruleApps.GetSequenceHashCode();
            code = code * 31 + _mruleAppIndex.GetHashCode();
            code = code * 31 + _isLastAppliedRuleFinal.GetHashCode();
            return code;
        }

        public override bool ValueEquals(Word other)
        {
            if (other == null)
                return false;

            if (IsFrozen && other.IsFrozen && GetFrozenHashCode() != other.GetFrozenHashCode())
                return false;

            return _shape.ValueEquals(other._shape)
                && _realizationalFS.ValueEquals(other._realizationalFS)
                && _nonHeadApps.SequenceEqual(other._nonHeadApps, FreezableEqualityComparer<Word>.Default)
                && _nonHeadAppIndex == other._nonHeadAppIndex
                && _stratum == other._stratum
                && _rootAllomorph == other._rootAllomorph
                && _mruleApps.SequenceEqual(other._mruleApps)
                && _mruleAppIndex == other._mruleAppIndex
                && _isLastAppliedRuleFinal == other._isLastAppliedRuleFinal;
        }

        public Word Clone()
        {
            return new Word(this);
        }

        public override string ToString()
        {
            return Shape.ToRegexString(Stratum.CharacterDefinitionTable, true);
        }
    }
}
