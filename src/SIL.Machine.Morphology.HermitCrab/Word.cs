using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        /// True when <see cref="_shape"/> is a reference shared with another Word (the source this word was
        /// cloned from) rather than one this word exclusively owns. A shared shape is always frozen -- it is
        /// only ever adopted from a frozen source -- so any attempted mutation through it throws, which is
        /// the safety net for a missed <see cref="EnsureOwnShape"/>/<see cref="ResetShape"/> call. See the
        /// copy constructor below.
        /// </summary>
        private bool _shapeShared;
        private FeatureStruct _syntacticFS;

        /// <summary>
        /// True when <see cref="_syntacticFS"/> is a reference shared with another Word (the source this
        /// word was cloned from) rather than one this word exclusively owns. Mirrors <see cref="_shapeShared"/>,
        /// with one difference driven by <see cref="FreezeImpl"/> deliberately never freezing this field
        /// (rules mutate <see cref="SyntacticFeatureStruct"/> in place on already-frozen Words -- see
        /// <see cref="AnalysisAffixTemplateRule"/>,
        /// <see cref="MorphologicalRules.AnalysisAffixProcessRule"/>,
        /// <see cref="MorphologicalRules.AnalysisCompoundingRule"/>): the sharing decision below is keyed on
        /// <c>word.SyntacticFeatureStruct.IsFrozen</c> (the struct's own frozen state), not <c>word.IsFrozen</c>.
        /// A shared struct is always frozen -- shared only from a source whose SyntacticFeatureStruct was
        /// already frozen -- so a missed <see cref="EnsureOwnSyntacticFeatureStruct"/> call throws instead of
        /// corrupting the source word's features. Freezing a struct that is already frozen (e.g.
        /// <see cref="AnalysisStateKey.PinAndKey"/> freezing a struct a child shares with its still-live
        /// parent) is a no-op -- <c>FeatureStruct.Freeze</c> returns immediately when <c>IsFrozen</c> is
        /// already true -- so sharing the same immutable instance onto many Words and re-freezing it from any
        /// of them is safe.
        /// </summary>
        private bool _syntacticFSShared;

        /// <summary>
        /// Whether the internal engine clone path may share this word's frozen
        /// <see cref="SyntacticFeatureStruct"/>. A Morpher stamps its instance setting on each parse or
        /// generation root, and internal clones propagate it. Publicly constructed Words default to false,
        /// and public <see cref="Clone"/> always deep-copies the struct regardless of this value.
        /// </summary>
        internal bool ShareSyntacticFeatureStructs { get; set; }

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
            _shapeShared = false;
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
            : this(word, cloneNonHeadApps: true, useEngineClone: false) { }

        // ReplayOnto passes false: it rebuilds the non-head list wholesale, so cloning it here would be
        // discarded work.
        private Word(Word word, bool cloneNonHeadApps, bool useEngineClone)
        {
            _allomorphs = new Dictionary<string, Allomorph>(word._allomorphs);
            Stratum = word.Stratum;
            Source = word;
            // Don't copy Alternatives.
            // A frozen source's shape is immutable, so it is safe to share the reference instead of paying
            // for a deep copy; the shape is only actually cloned (EnsureOwnShape) or replaced wholesale
            // (ResetShape) at the few call sites that go on to mutate it. An unfrozen source's shape could
            // still change out from under us, so it must be deep-copied as before.
            if (useEngineClone && word._shape.IsFrozen)
            {
                _shape = word._shape;
                _shapeShared = true;
            }
            else
            {
                _shape = word._shape.Clone();
                _shapeShared = false;
            }
            _rootAllomorph = word._rootAllomorph;
            ShareSyntacticFeatureStructs = word.ShareSyntacticFeatureStructs;
            // Mirrors the Shape sharing above, except keyed on the struct's own IsFrozen (see _syntacticFSShared):
            // FreezeImpl never freezes SyntacticFeatureStruct, so word.IsFrozen says nothing about it. In the
            // memoized analysis cascade, AnalysisStateKey.PinAndKey freezes a word's SyntacticFeatureStruct
            // before using it as a memo key, so most parents are already frozen here by the time they are cloned.
            if (useEngineClone && ShareSyntacticFeatureStructs && word.SyntacticFeatureStruct.IsFrozen)
            {
                _syntacticFS = word.SyntacticFeatureStruct;
                _syntacticFSShared = true;
            }
            else
            {
                _syntacticFS = word.SyntacticFeatureStruct.Clone();
                _syntacticFSShared = false;
            }
            RealizationalFeatureStruct = word.RealizationalFeatureStruct.Clone();
            _mprFeatures = word.MprFeatures.Clone();
            _mruleApps = new List<IMorphologicalRule>(word._mruleApps);
            _mruleAppIndex = word._mruleAppIndex;
            _mrulesUnapplied = new Dictionary<IMorphologicalRule, int>(word._mrulesUnapplied);
            _mrulesApplied = new Dictionary<IMorphologicalRule, int>(word._mrulesApplied);
            _nonHeadApps = cloneNonHeadApps
                ? word
                    ._nonHeadApps.Select(nonHead => useEngineClone ? nonHead.CloneForEngine() : nonHead.Clone())
                    .ToList()
                : new List<Word>();
            _nonHeadAppIndex = word._nonHeadAppIndex;
            _obligatorySyntacticFeatures = new IDBearerSet<Feature>(word._obligatorySyntacticFeatures);
            _isLastAppliedRuleFinal = word._isLastAppliedRuleFinal;
            _isPartial = word._isPartial;
            CurrentTrace = word.CurrentTrace;
            AnalysisScope = word.AnalysisScope;
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
                _shapeShared = false;
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

        public FeatureStruct SyntacticFeatureStruct
        {
            get { return _syntacticFS; }
            // Deliberately no CheckFrozen(): FreezeImpl never freezes this field, so rules assign/mutate it
            // (via this setter or EnsureOwnSyntacticFeatureStruct + an in-place mutator) on Words that are
            // already frozen. Any assignment -- a brand-new struct, or one cloned from a shared source -- is
            // by definition no longer shared with this word's clone source, so the flag is cleared here.
            internal set
            {
                _syntacticFS = value;
                _syntacticFSShared = false;
            }
        }

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
                    Word alternative = original.CloneForEngine();
                    // this must be frozen here: its shape is about to be shared onto alternative, and a
                    // shared shape is only ever safe to hand out because it is immutable.
                    Debug.Assert(IsFrozen, "ExpandAlternatives requires a frozen word");
                    alternative._shape = this.Shape;
                    alternative._shapeShared = true;
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
            var clone = new Word(this, cloneNonHeadApps: false, useEngineClone: true);

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
                clone._nonHeadApps.AddRange(queryNode._nonHeadApps.Select(nonHead => nonHead.CloneForEngine()));
            clone._nonHeadApps.AddRange(
                _nonHeadApps
                    .GetRange(nonHeadPrefixLength, _nonHeadApps.Count - nonHeadPrefixLength)
                    .Select(nonHead => nonHead.CloneForEngine())
            );
            clone._nonHeadAppIndex = clone._nonHeadApps.Count - 1;

            clone.Freeze();
            return clone;
        }

        // Hoisted out of the per-result loop by AnalysisScope.TryReplay; see ReplayOnto.
        internal List<Word> CloneNonHeadsForReplay()
        {
            return _nonHeadApps.Select(nonHead => nonHead.CloneForEngine()).ToList();
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
                        ShareSyntacticFeatureStructs = ShareSyntacticFeatureStructs,
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

        /// <summary>
        /// Clones this Word for continued in-assembly processing, allowing a frozen syntactic feature
        /// structure to be shared when the originating Morpher enabled that optimization.
        /// </summary>
        internal Word CloneForEngine()
        {
            return new Word(this, cloneNonHeadApps: true, useEngineClone: true);
        }

        /// <summary>
        /// Ensures this word owns its <see cref="Shape"/> outright rather than sharing it with the word it
        /// was cloned from, cloning it (once) if necessary. Callers that clone a word and are about to
        /// mutate the shape in place -- rather than rebuild it from scratch, see <see cref="ResetShape"/> --
        /// must call this first, before anything reads a node reference out of the shape to mutate later:
        /// once shared, the shape is frozen, so any mutation attempted through a missed call throws instead
        /// of silently corrupting the source word's shape.
        /// </summary>
        internal void EnsureOwnShape()
        {
            CheckFrozen();
            if (_shapeShared)
            {
                _shape = _shape.Clone();
                _shapeShared = false;
            }
        }

        /// <summary>
        /// Ensures this word owns its <see cref="SyntacticFeatureStruct"/> outright rather than sharing it
        /// with the word it was cloned from, cloning it (once) if necessary. Callers that are about to mutate
        /// it in place (<c>Add</c>/<c>Clear</c>/etc., as opposed to assigning a whole new struct, which the
        /// property setter already handles) must call this first. Unlike <see cref="EnsureOwnShape"/>, this
        /// does NOT call <see cref="CheckFrozen"/>: <see cref="FreezeImpl"/> deliberately never freezes
        /// <see cref="SyntacticFeatureStruct"/>, and several rules mutate it on Words that are already frozen
        /// (see <see cref="_syntacticFSShared"/>), so this must remain callable on a frozen Word.
        /// </summary>
        internal void EnsureOwnSyntacticFeatureStruct()
        {
            if (_syntacticFSShared)
            {
                _syntacticFS = _syntacticFS.Clone();
                _syntacticFSShared = false;
            }
        }

        /// <summary>
        /// Replaces this word's <see cref="Shape"/> with a new, empty, unfrozen shape configured the same
        /// way as the current one, without paying for a deep copy of content that is about to be discarded.
        /// For call sites that clone a word only to immediately clear and rebuild its shape from other
        /// input (e.g. <c>GenerateShape</c>, the synthesis rule specs).
        /// </summary>
        internal void ResetShape()
        {
            CheckFrozen();
            _shape = _shape.CreateEmptyLike();
            _shapeShared = false;
        }

        public override string ToString()
        {
            return Shape.ToRegexString(Stratum.CharacterDefinitionTable, true);
        }
    }
}
