using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.FiniteState;
using SIL.Machine.Morphology;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// A token-accumulating FST analyzer for grammars whose affixation is organized into
    /// <em>affix templates</em> (position classes) — the real-grammar case (HERMITCRAB_FST_PLAN.md
    /// §6 Phase 2, §10). Each template becomes prefix-slot automaton → root → suffix-slot automaton;
    /// a template attaches to a root only when the root passes the build-time gate:
    /// <list type="bullet">
    /// <item><b>category</b> — the root's syntactic features unify with the template's
    /// <c>RequiredSyntacticFeatureStruct</c>; and</item>
    /// <item><b>stratum</b> — the root is at the template's stratum or an inner one (a template
    /// cannot apply to a root introduced in a later/outer stratum).</item>
    /// </list>
    /// Gating both prevents over-generation and lets same-category roots share the template's
    /// slot-automaton (states ≈ roots + Σ template automata, not roots × slot-combinations).
    /// Tokens are accumulated along the DFS path (a state carries the morpheme token emitted on
    /// entry). Prefix slots surface in reverse template order (slot 0 applies first → innermost),
    /// suffix slots in template order. A <paramref name="maxStates"/> budget (the §10 knob) aborts
    /// before a blowup. Phonology and reduplication/infix slots are out of scope — it throws on a
    /// non-prefix/suffix slot rather than silently mis-parsing.
    /// </summary>
    public class FstTemplateAnalyzer : IMorphologicalAnalyzer
    {
        private readonly Fst<Shape, ShapeNode> _fsa;
        private readonly State<Shape, ShapeNode> _start;
        private readonly Dictionary<State<Shape, ShapeNode>, uint> _tokenOnEntry =
            new Dictionary<State<Shape, ShapeNode>, uint>();
        private readonly Dictionary<State<Shape, ShapeNode>, int> _stateIds =
            new Dictionary<State<Shape, ShapeNode>, int>();
        private readonly MorphTokenCodec _codec = new MorphTokenCodec();
        private readonly CharacterDefinitionTable _table;
        private readonly Func<Annotation<ShapeNode>, bool> _filter;
        private readonly int _maxStates;
        private readonly Func<RootAllomorph, IReadOnlyCollection<string>> _bareRootSurfaces;
        private readonly Func<string, IReadOnlyCollection<string>> _affixSurfaces;
        private readonly List<MorphemicMorphologicalRule> _derivSuffixRules = new List<MorphemicMorphologicalRule>();
        private readonly List<MorphemicMorphologicalRule> _derivPrefixRules = new List<MorphemicMorphologicalRule>();
        private int _stateCount;
        private readonly HashSet<MorphOp> _uncoveredOps = new HashSet<MorphOp>();

        /// <summary>
        /// Max stacked derivational affixes modelled per side before inflection (tunable per grammar).
        /// 2 (e.g. REV+NZR) is the speed/coverage sweet spot for Sena: depth 3 (PAS+APPLIC+NZR) gains a
        /// word or two but roughly doubles verify cost (more over-gen proposals to reject). Deeper
        /// stacks than this are left to the search backstop rather than inflating every verification.
        /// </summary>
        private readonly int _derivDepth;

        public MorphTokenCodec Codec => _codec;

        /// <summary>Number of FST states built (the precomputed size — to watch for state blow-up).</summary>
        public int StateCount => _stateCount;

        /// <summary>
        /// False if the build skipped a construct it cannot model (an infix/circumfix/reduplication/
        /// process slot or rule). The proposer degrades gracefully — it skips such constructs and
        /// builds the rest — so a grammar using them under-generates on the fast path and must NOT be
        /// certified (it falls to the engine/cache). The empirical set-parity gate enforces this; this
        /// flag is the cheap build-time signal of the same fact.
        /// </summary>
        public bool CoversAllConstructs => _uncoveredOps.Count == 0;

        /// <summary>The set of <see cref="MorphOp"/>s the build skipped because it cannot model them in
        /// the FST (infix/circumfix/reduplication/process). A sibling generator that covers one of these
        /// (see <see cref="CompositeProposer"/>) removes it from the composite's uncovered set.</summary>
        public IReadOnlyCollection<MorphOp> UncoveredOps => _uncoveredOps;

        /// <summary>Build without obligatoriness: every root may stand bare (fine for toy grammars).</summary>
        public FstTemplateAnalyzer(Language language, int maxStates = 1_000_000, int derivDepth = 2)
            : this(language, root => new[] { UnderlyingForm(root) }, s => new[] { s }, maxStates, derivDepth) { }

        /// <summary>
        /// Build with obligatory-inflection enforcement AND surface-allomorph precompile (§C): a root's
        /// bare surface realizations are obtained by synthesizing it bare (HC's own finality check). If
        /// synthesis returns nothing, the bare reading is suppressed (obligatory inflection); if it
        /// returns a phonologically-ALTERED surface, the proposer builds an arc for that surface so a
        /// phonologically-altered bare root is matched (not just the underlying form).
        /// </summary>
        public FstTemplateAnalyzer(Language language, Morpher morpher, int maxStates = 1_000_000, int derivDepth = 2)
            : this(
                language,
                root => BareRootSurfaces(morpher, root),
                new SurfacePhonology(language, morpher).Variants,
                maxStates,
                derivDepth
            ) { }

        private FstTemplateAnalyzer(
            Language language,
            Func<RootAllomorph, IReadOnlyCollection<string>> bareRootSurfaces,
            Func<string, IReadOnlyCollection<string>> affixSurfaces,
            int maxStates,
            int derivDepth
        )
        {
            _bareRootSurfaces = bareRootSurfaces;
            _affixSurfaces = affixSurfaces;
            _maxStates = maxStates;
            _derivDepth = derivDepth;
            _table = language.SurfaceStratum.CharacterDefinitionTable;
            _filter = ann => ann.Type() == HCFeatureSystem.Segment;
            _fsa = new Fst<Shape, ShapeNode> { Filter = _filter, UseUnification = true };
            _start = NewState();
            _fsa.StartState = _start;

            // Collect every root with the stratum index it is introduced at.
            var roots = new List<RootRef>();
            for (int si = 0; si < language.Strata.Count; si++)
            {
                foreach (LexEntry entry in language.Strata[si].Entries)
                {
                    foreach (RootAllomorph allomorph in entry.Allomorphs)
                    {
                        roots.Add(new RootRef(allomorph, entry.SyntacticFeatureStruct, si));
                    }
                }
            }

            // Standalone derivational affix rules (REC/APPLIC/REV/NZR/NEU/PAS/...), distinct from
            // inflectional template slots and from compounding. Suffixal ones become an optional,
            // bounded layer between the root and the inflectional suffix slots (§11.2).
            foreach (Stratum stratum in language.Strata)
            {
                foreach (IMorphologicalRule mrule in stratum.MorphologicalRules)
                {
                    if (!(mrule is MorphemicMorphologicalRule rule))
                    {
                        continue;
                    }
                    MorphOp ruleOp = RuleOp(rule);
                    switch (ruleOp)
                    {
                        case MorphOp.Suffix:
                            _derivSuffixRules.Add(rule);
                            break;
                        case MorphOp.Prefix:
                            _derivPrefixRules.Add(rule);
                            break;
                        case MorphOp.None:
                            break;
                        default:
                            // A standalone rule the proposer cannot build (reduplication/infix/process).
                            // Record the op as uncovered so the grammar does not certify unless a sibling
                            // generator (see CompositeProposer) covers it.
                            _uncoveredOps.Add(ruleOp);
                            break;
                    }
                }
            }

            // Bare-root paths — only for roots the grammar allows to stand uninflected. Surface-allomorph
            // precompile (§C): build a chain for the underlying form AND for each phonologically-altered
            // bare surface realization, so an altered bare root is matched. The emitted token is always
            // the underlying root morpheme; verify re-runs HC (with real phonology) to confirm.
            foreach (RootRef root in roots)
            {
                IReadOnlyCollection<string> surfaces = _bareRootSurfaces(root.Allomorph);
                if (surfaces.Count == 0)
                {
                    continue; // bare root not valid (obligatory inflection)
                }
                State<Shape, ShapeNode> end = BuildRootChain(_start, root.Allomorph);
                end.IsAccepting = true;
                string underlying = UnderlyingForm(root.Allomorph);
                foreach (string s in surfaces)
                {
                    if (s == underlying)
                    {
                        continue; // already built from the underlying shape
                    }
                    State<Shape, ShapeNode> surfaceEnd = BuildRootChainFromSurface(_start, s, root.Allomorph.Morpheme);
                    if (surfaceEnd != null)
                    {
                        surfaceEnd.IsAccepting = true;
                    }
                }
            }

            // Template-less derivational stems: optional derivational prefixes + root + optional
            // derivational suffixes, with NO inflectional template — for roots that derive/associate
            // without inflecting (e.g. a pronoun taking an associative prefix: coisa + d'eles).
            // Shared prefix/suffix derivation layers (built once) keep this additive. Verify-discard
            // removes any over-generation, including a bare stem that should not stand alone.
            if (_derivPrefixRules.Count > 0 || _derivSuffixRules.Count > 0)
            {
                State<Shape, ShapeNode> tlPrefixEntry = NewState();
                State<Shape, ShapeNode> tlRootStart = BuildDerivationPrefixLayer(tlPrefixEntry);
                _start.Arcs.Add(tlPrefixEntry); // epsilon: enter the template-less path
                State<Shape, ShapeNode> tlSuffixEntry = NewState();
                State<Shape, ShapeNode> tlSuffixExit = BuildDerivationSuffixLayer(tlSuffixEntry);
                tlSuffixExit.IsAccepting = true;
                foreach (RootRef root in roots)
                {
                    State<Shape, ShapeNode> end = BuildRootChain(tlRootStart, root.Allomorph);
                    end.Arcs.Add(tlSuffixEntry); // epsilon: root → shared derivational suffixes → accept
                }
            }

            // Each template: prefix automaton → (gated roots) → suffix automaton.
            for (int ti = 0; ti < language.Strata.Count; ti++)
            {
                foreach (AffixTemplate template in language.Strata[ti].AffixTemplates)
                {
                    var prefixSlots = new List<AffixTemplateSlot>();
                    var suffixSlots = new List<AffixTemplateSlot>();
                    ClassifyTemplate(template, prefixSlots, suffixSlots);

                    State<Shape, ShapeNode> prefixEntry = NewState();
                    State<Shape, ShapeNode> prefixExit = AppendSlots(
                        prefixEntry,
                        prefixSlots,
                        MorphOp.Prefix,
                        template.RequiredSyntacticFeatureStruct
                    );
                    // Shared derivational-prefix layer between the inflectional prefixes and the root
                    // (surface order: class-prefix → derivational-prefix → root, e.g.
                    // 10 + nominalizador + [ser]). Roots start after it.
                    State<Shape, ShapeNode> rootStart = BuildDerivationPrefixLayer(prefixExit);
                    State<Shape, ShapeNode> suffixEntry = NewState();
                    State<Shape, ShapeNode> suffixExit = AppendSlots(
                        suffixEntry,
                        suffixSlots,
                        MorphOp.Suffix,
                        template.RequiredSyntacticFeatureStruct
                    );
                    suffixExit.IsAccepting = true;

                    // One derivation layer per template, shared by all its roots (tokens accumulate
                    // on the walk path, so sharing avoids a roots×derivations blowup): root →
                    // derivation suffixes → inflectional suffix slots.
                    State<Shape, ShapeNode> derivEntry = NewState();
                    State<Shape, ShapeNode> derivExit = BuildDerivationSuffixLayer(derivEntry);
                    derivExit.Arcs.Add(suffixEntry); // epsilon: derivation → inflectional suffixes

                    _start.Arcs.Add(prefixEntry); // epsilon: enter this template

                    foreach (RootRef root in roots)
                    {
                        // Attach the root to this template if its category matches directly, OR if a
                        // derivational suffix in the layer changes the root's category to the
                        // template's (e.g. a nominalizer feeding a noun-class template: vencer[verb] +
                        // NZR → noun, then class-10 prefix). The category-changing suffix is in the
                        // shared derivation layer; verify-discard removes any resulting over-gen (§11.4).
                        if (
                            root.StratumIndex <= ti
                            && (
                                CategoryMatches(root.Category, template.RequiredSyntacticFeatureStruct)
                                || DerivableToCategory(root.Category, template.RequiredSyntacticFeatureStruct)
                            )
                        )
                        {
                            State<Shape, ShapeNode> end = BuildRootChain(rootStart, root.Allomorph);
                            end.Arcs.Add(derivEntry); // epsilon: root → derivation → suffix slots
                        }
                    }
                }
            }
        }

        public IEnumerable<WordAnalysis> AnalyzeWord(string word)
        {
            Shape shape;
            try
            {
                shape = _table.Segment(word);
            }
            catch (InvalidShapeException)
            {
                // A word with a phoneme outside this table cannot be a surface form here.
                return Enumerable.Empty<WordAnalysis>();
            }

            var segments = new List<FeatureStruct>();
            for (
                ShapeNode node = shape.GetFirst(n => _filter(n.Annotation));
                node != shape.End;
                node = node.GetNext(n => _filter(n.Annotation))
            )
            {
                segments.Add(node.Annotation.FeatureStruct);
            }

            // NFA simulation: a set of (state, accumulated tokens) configurations advanced one
            // segment at a time, deduped by (state, tokens) so shared states are not re-explored
            // (a naive recursive DFS is exponential on a real grammar's nondeterminism).
            List<Config> current = EpsilonClosure(new List<Config> { Enter(_start, new uint[0]) });
            foreach (FeatureStruct segment in segments)
            {
                var next = new List<Config>();
                var seen = new HashSet<string>();
                foreach (Config config in current)
                {
                    for (int a = 0; a < config.State.Arcs.Count; a++)
                    {
                        Arc<Shape, ShapeNode> arc = config.State.Arcs[a];
                        if (!arc.Input.IsEpsilon && arc.Input.FeatureStruct.IsUnifiable(segment))
                        {
                            Config nc = Enter(arc.Target, config.Tokens);
                            if (seen.Add(Key(nc)))
                            {
                                next.Add(nc);
                            }
                        }
                    }
                }
                current = EpsilonClosure(next);
                if (current.Count == 0)
                {
                    break;
                }
            }

            var results = new List<WordAnalysis>();
            var emitted = new HashSet<string>();
            foreach (Config config in current)
            {
                if (config.State.IsAccepting && emitted.Add(string.Join(",", config.Tokens)))
                {
                    results.Add(ToWordAnalysis(config.Tokens));
                }
            }
            return results;
        }

        private List<Config> EpsilonClosure(List<Config> configs)
        {
            var result = new List<Config>();
            var seen = new HashSet<string>();
            var stack = new Stack<Config>();
            foreach (Config config in configs)
            {
                if (seen.Add(Key(config)))
                {
                    stack.Push(config);
                    result.Add(config);
                }
            }
            while (stack.Count > 0)
            {
                Config config = stack.Pop();
                for (int a = 0; a < config.State.Arcs.Count; a++)
                {
                    Arc<Shape, ShapeNode> arc = config.State.Arcs[a];
                    if (arc.Input.IsEpsilon)
                    {
                        Config nc = Enter(arc.Target, config.Tokens);
                        if (seen.Add(Key(nc)))
                        {
                            stack.Push(nc);
                            result.Add(nc);
                        }
                    }
                }
            }
            return result;
        }

        private Config Enter(State<Shape, ShapeNode> state, uint[] tokens)
        {
            return _tokenOnEntry.TryGetValue(state, out uint token)
                ? new Config(state, Append(tokens, token))
                : new Config(state, tokens);
        }

        private string Key(Config config)
        {
            return _stateIds[config.State] + ":" + string.Join(",", config.Tokens);
        }

        private WordAnalysis ToWordAnalysis(uint[] tokens)
        {
            var morphemes = new List<IMorpheme>(tokens.Length);
            foreach (uint token in tokens)
            {
                morphemes.Add(_codec.GetMorpheme(MorphToken.GetMorphemeId(token)));
            }
            return new WordAnalysis(morphemes, MorphToken.RootIndex(tokens), null);
        }

        /// <summary>Split a template's slots into prefix and suffix; prefixes are reversed to surface order.</summary>
        private void ClassifyTemplate(
            AffixTemplate template,
            List<AffixTemplateSlot> prefixSlots,
            List<AffixTemplateSlot> suffixSlots
        )
        {
            foreach (AffixTemplateSlot slot in template.Slots)
            {
                switch (SlotOp(slot))
                {
                    case MorphOp.Prefix:
                        prefixSlots.Add(slot);
                        break;
                    case MorphOp.Suffix:
                        suffixSlots.Add(slot);
                        break;
                    default:
                        // A slot the proposer cannot build (infix/circumfix/reduplication/process).
                        // Skip it and record the construct op(s) as uncovered — those words fall to the
                        // engine/cache unless a sibling generator covers the op; the parity gate refuses
                        // to certify otherwise. (Was a hard throw that aborted the whole build.)
                        foreach (MorphemicMorphologicalRule rule in slot.Rules)
                        {
                            MorphOp ruleOp = RuleOp(rule);
                            if (ruleOp != MorphOp.Prefix && ruleOp != MorphOp.Suffix && ruleOp != MorphOp.None)
                            {
                                _uncoveredOps.Add(ruleOp);
                            }
                        }
                        break;
                }
            }
            prefixSlots.Reverse(); // slot 0 applies first (innermost) → rightmost prefix on the surface
        }

        /// <summary>The slot's surface role: the first rule that is a prefix or suffix. A slot whose
        /// only rules are zero-segment affixes is a (position-less) suffix so it still builds; a slot
        /// with no prefix/suffix/zero rule (e.g. infix/reduplication only) is None → skipped.</summary>
        private static MorphOp SlotOp(AffixTemplateSlot slot)
        {
            bool hasZero = false;
            foreach (MorphemicMorphologicalRule rule in slot.Rules)
            {
                MorphOp op = RuleOp(rule);
                if (op == MorphOp.Prefix || op == MorphOp.Suffix)
                {
                    return op;
                }
                if (op == MorphOp.None)
                {
                    hasZero = true; // a zero/empty-segment affix — no surface position
                }
            }
            return hasZero ? MorphOp.Suffix : MorphOp.None;
        }

        /// <summary>The surface role (prefix/suffix/…) of a morphological rule, from its first allomorph.</summary>
        private static MorphOp RuleOp(MorphemicMorphologicalRule rule)
        {
            foreach (AffixProcessAllomorph allomorph in Allomorphs(rule))
            {
                return MorphTokenCodec.ClassifyOp(allomorph, false);
            }
            return MorphOp.None;
        }

        /// <summary>
        /// An optional, bounded chain of derivational suffixes (the stratum's standalone affix
        /// rules), shared by every root of a template. Permissive by design: a category-illegal
        /// derivation (e.g. a nominalizer feeding a verbal suffix) is proposed here and removed by
        /// re-synthesis verification (<see cref="VerifiedFstAnalyzer"/>), per the plan §11.2.
        /// </summary>
        private State<Shape, ShapeNode> BuildDerivationSuffixLayer(State<Shape, ShapeNode> entry)
        {
            return BuildDerivationLayer(entry, _derivSuffixRules, MorphOp.Suffix);
        }

        /// <summary>
        /// An optional, bounded chain of derivational prefixes (the stratum's standalone prefix affix
        /// rules) between the inflectional prefixes and the root — mirror of the suffix layer (§12.4).
        /// </summary>
        private State<Shape, ShapeNode> BuildDerivationPrefixLayer(State<Shape, ShapeNode> entry)
        {
            return BuildDerivationLayer(entry, _derivPrefixRules, MorphOp.Prefix);
        }

        /// <summary>Shared builder for an optional, bounded derivational-affix layer of the given op.</summary>
        private State<Shape, ShapeNode> BuildDerivationLayer(
            State<Shape, ShapeNode> entry,
            List<MorphemicMorphologicalRule> rules,
            MorphOp op
        )
        {
            State<Shape, ShapeNode> current = entry;
            for (int k = 0; k < _derivDepth; k++)
            {
                State<Shape, ShapeNode> after = NewState();
                current.Arcs.Add(after); // epsilon: apply no derivation at this level
                foreach (MorphemicMorphologicalRule rule in rules)
                {
                    foreach (AffixProcessAllomorph allomorph in Allomorphs(rule))
                    {
                        if (MorphTokenCodec.ClassifyOp(allomorph, false) != op)
                        {
                            continue;
                        }
                        uint token = MorphToken.Encode(op, _codec.GetOrAddIndex(allomorph.Morpheme));
                        State<Shape, ShapeNode> tokenState = NewState();
                        _tokenOnEntry[tokenState] = token;
                        current.Arcs.Add(tokenState); // epsilon: enter this derivational affix
                        BuildAffixArcs(tokenState, after, allomorph.Rhs.OfType<InsertSegments>().FirstOrDefault());
                    }
                }
                current = after;
            }
            return current;
        }

        /// <summary>
        /// Build an affix's segment arcs from <paramref name="tokenState"/> to <paramref name="after"/>:
        /// the underlying form AND each phonologically-altered surface realization (surface-allomorph
        /// precompile, Point 1, C-internal tier), so an affix whose surface differs from its underlying
        /// segments (e.g. a suffix that devoices word-finally) is matched. A zero-segment affix (null
        /// <paramref name="insert"/>) just reconverges. Sound: the underlying path is always built, the
        /// emitted token is the underlying morpheme, and verify confirms with real phonology; a variant
        /// not actually attested is pruned by verify, a missed cross-boundary variant rides the engine.
        /// </summary>
        private void BuildAffixArcs(
            State<Shape, ShapeNode> tokenState,
            State<Shape, ShapeNode> after,
            InsertSegments insert
        )
        {
            if (insert == null)
            {
                tokenState.Arcs.Add(after); // zero/empty-segment affix: token only
                return;
            }
            State<Shape, ShapeNode> s = tokenState;
            foreach (FeatureStruct fs in GetSegments(insert.Segments.Shape))
            {
                s = AddArc(s, fs);
            }
            s.Arcs.Add(after);

            string underlying = insert.Segments.Representation;
            foreach (string variant in _affixSurfaces(underlying))
            {
                if (variant == underlying)
                {
                    continue; // underlying path already built
                }
                Shape vshape;
                try
                {
                    vshape = _table.Segment(variant);
                }
                catch (InvalidShapeException)
                {
                    continue;
                }
                State<Shape, ShapeNode> sv = tokenState;
                foreach (FeatureStruct fs in GetSegments(vshape))
                {
                    sv = AddArc(sv, fs);
                }
                sv.Arcs.Add(after);
            }
        }

        /// <summary>Allomorphs of a slot rule — both AffixProcessRule and its realizational sibling.</summary>
        /// <summary>
        /// True iff this root may surface uninflected — i.e. synthesizing it with no affixes yields
        /// its own surface form. If the grammar makes a bare stem non-final (obligatory inflection),
        /// synthesis returns nothing and the bare reading is correctly suppressed.
        /// </summary>
        private static string UnderlyingForm(RootAllomorph root)
        {
            return root.Segments.Representation.Normalize(System.Text.NormalizationForm.FormD);
        }

        /// <summary>
        /// The bare-root surface realizations: the surface forms HC synthesizes for the root with no
        /// affixes (phonology applied). Empty ⇒ the bare root is not a valid word (obligatory
        /// inflection). A form ≠ the underlying representation is a phonologically-altered surface the
        /// proposer must match (Solution 1, §C). Reuses the same GenerateWords call the obligatoriness
        /// check needed, so it is zero extra build cost.
        /// </summary>
        private static IReadOnlyCollection<string> BareRootSurfaces(Morpher morpher, RootAllomorph root)
        {
            if (!(root.Morpheme is LexEntry entry))
            {
                return new[] { UnderlyingForm(root) };
            }
            return morpher
                .GenerateWords(entry, System.Linq.Enumerable.Empty<Morpheme>(), new FeatureStruct())
                .Select(g => g.Normalize(System.Text.NormalizationForm.FormD))
                .Distinct()
                .ToList();
        }

        private static FeatureStruct RequiredCategory(MorphemicMorphologicalRule rule)
        {
            switch (rule)
            {
                case AffixProcessRule affix:
                    return affix.RequiredSyntacticFeatureStruct;
                case RealizationalAffixProcessRule realizational:
                    return realizational.RequiredSyntacticFeatureStruct;
                default:
                    return null;
            }
        }

        /// <summary>The category a derivational rule outputs (its <c>OutSyntacticFeatureStruct</c>).</summary>
        private static FeatureStruct OutCategory(MorphemicMorphologicalRule rule)
        {
            return rule is AffixProcessRule affix ? affix.OutSyntacticFeatureStruct : null;
        }

        /// <summary>
        /// True iff <paramref name="rootCategory"/> can be transformed into <paramref name="templateCategory"/>
        /// by a chain of ≤ the derivation-depth bound derivational suffixes (a category-changing
        /// derivation, e.g. verb → noun via a nominalizer). Lets a template attach over a derived stem
        /// of its output category; the category-changing suffix rides the shared derivation layer.
        /// </summary>
        private bool DerivableToCategory(FeatureStruct rootCategory, FeatureStruct templateCategory)
        {
            if (rootCategory == null || templateCategory == null || templateCategory.IsEmpty)
            {
                return false;
            }
            var frontier = new List<FeatureStruct> { rootCategory };
            for (int depth = 0; depth < _derivDepth && frontier.Count > 0; depth++)
            {
                var next = new List<FeatureStruct>();
                foreach (FeatureStruct cat in frontier)
                {
                    foreach (MorphemicMorphologicalRule rule in _derivSuffixRules.Concat(_derivPrefixRules))
                    {
                        FeatureStruct outCat = OutCategory(rule);
                        if (outCat == null || outCat.IsEmpty)
                        {
                            continue; // not a category-changing derivation
                        }
                        FeatureStruct inCat = RequiredCategory(rule);
                        if (inCat != null && !inCat.IsEmpty && !cat.IsUnifiable(inCat))
                        {
                            continue; // rule does not apply to this stem category
                        }
                        if (outCat.IsUnifiable(templateCategory))
                        {
                            return true;
                        }
                        next.Add(outCat);
                    }
                }
                frontier = next;
            }
            return false;
        }

        private static IEnumerable<AffixProcessAllomorph> Allomorphs(MorphemicMorphologicalRule rule)
        {
            switch (rule)
            {
                case AffixProcessRule affix:
                    return affix.Allomorphs;
                case RealizationalAffixProcessRule realizational:
                    return realizational.Allomorphs;
                default:
                    return Enumerable.Empty<AffixProcessAllomorph>();
            }
        }

        /// <summary>Build the slot sequence from <paramref name="start"/>; returns the state after the last slot.</summary>
        private State<Shape, ShapeNode> AppendSlots(
            State<Shape, ShapeNode> start,
            List<AffixTemplateSlot> slots,
            MorphOp op,
            FeatureStruct templateCategory
        )
        {
            State<Shape, ShapeNode> current = start;
            foreach (AffixTemplateSlot slot in slots)
            {
                State<Shape, ShapeNode> after = NewState();
                if (slot.Optional)
                {
                    current.Arcs.Add(after); // epsilon: skip this slot
                }
                foreach (MorphemicMorphologicalRule rule in slot.Rules)
                {
                    // Build-time category gate (faithful for inflectional templates, where the
                    // category is ~constant): a rule whose RequiredSyntacticFeatureStruct cannot
                    // unify with the template's category can never apply here, so omit it. This is
                    // HC's Required.Unify(stem) check, hoisted to compile time — no walk-order issue.
                    FeatureStruct required = RequiredCategory(rule);
                    if (
                        templateCategory != null
                        && required != null
                        && !required.IsEmpty
                        && !templateCategory.IsUnifiable(required)
                    )
                    {
                        continue;
                    }
                    foreach (AffixProcessAllomorph allomorph in Allomorphs(rule))
                    {
                        MorphOp aop = MorphTokenCodec.ClassifyOp(allomorph, false);
                        if (aop != op && aop != MorphOp.None)
                        {
                            // A rule the proposer can't build in this slot (infix/circumfix/redup/
                            // process). Skip it and record the op as uncovered; the engine/cache backstop
                            // and parity gate handle those words unless a sibling generator covers the op.
                            // (Was a hard throw.)
                            _uncoveredOps.Add(aop);
                            continue;
                        }
                        // aop == op (normal affix) or aop == None (a true zero-segment affix: no
                        // InsertSegments) — both emit the morpheme token at this slot's position; a
                        // zero affix simply adds no segment arcs.
                        uint affixToken = MorphToken.Encode(op, _codec.GetOrAddIndex(allomorph.Morpheme));
                        // Enter the affix through a token-bearing state, so the morpheme is emitted
                        // even for a zero/empty-segment affix (its token would otherwise be lost).
                        State<Shape, ShapeNode> tokenState = NewState();
                        _tokenOnEntry[tokenState] = affixToken;
                        current.Arcs.Add(tokenState); // epsilon: enter this affix
                        BuildAffixArcs(tokenState, after, allomorph.Rhs.OfType<InsertSegments>().FirstOrDefault());
                    }
                }
                current = after;
            }
            return current;
        }

        private State<Shape, ShapeNode> BuildRootChain(State<Shape, ShapeNode> from, RootAllomorph root)
        {
            State<Shape, ShapeNode> state = from;
            foreach (FeatureStruct fs in GetSegments(root.Segments.Shape))
            {
                state = AddArc(state, fs);
            }
            _tokenOnEntry[state] = MorphToken.Encode(MorphOp.Root, _codec.GetOrAddIndex(root.Morpheme));
            return state;
        }

        /// <summary>Build a root chain from a surface STRING (a phonologically-altered realization),
        /// segmenting it via the table; the chain ends in the underlying root morpheme's token. Returns
        /// null if the surface has a segment outside the table.</summary>
        private State<Shape, ShapeNode> BuildRootChainFromSurface(State<Shape, ShapeNode> from, string surface, Morpheme morpheme)
        {
            Shape shape;
            try
            {
                shape = _table.Segment(surface);
            }
            catch (InvalidShapeException)
            {
                return null;
            }
            State<Shape, ShapeNode> state = from;
            foreach (FeatureStruct fs in GetSegments(shape))
            {
                state = AddArc(state, fs);
            }
            _tokenOnEntry[state] = MorphToken.Encode(MorphOp.Root, _codec.GetOrAddIndex(morpheme));
            return state;
        }

        private static bool CategoryMatches(FeatureStruct rootCategory, FeatureStruct required)
        {
            if (required == null || required.IsEmpty)
            {
                return true;
            }
            return rootCategory != null && rootCategory.IsUnifiable(required);
        }

        private IReadOnlyList<FeatureStruct> GetSegments(Shape shape)
        {
            var segments = new List<FeatureStruct>();
            for (
                ShapeNode node = shape.GetFirst(n => _filter(n.Annotation));
                node != shape.End;
                node = node.GetNext(n => _filter(n.Annotation))
            )
            {
                FeatureStruct fs = node.Annotation.FeatureStruct.Clone();
                fs.Freeze();
                segments.Add(fs);
            }
            return segments;
        }

        private State<Shape, ShapeNode> AddArc(State<Shape, ShapeNode> state, FeatureStruct condition)
        {
            State<Shape, ShapeNode> next = NewState();
            state.Arcs.Add(condition, next);
            return next;
        }

        private State<Shape, ShapeNode> NewState()
        {
            _stateCount++;
            if (_stateCount > _maxStates)
            {
                throw new NotSupportedException(
                    $"FstTemplateAnalyzer exceeded the state budget ({_maxStates}); this grammar needs the "
                        + "lazy / on-the-fly partition (HERMITCRAB_FST_PLAN.md §10) rather than an eager build."
                );
            }
            State<Shape, ShapeNode> state = _fsa.CreateState();
            _stateIds[state] = _stateCount;
            return state;
        }

        private static uint[] Append(uint[] tokens, uint token)
        {
            var result = new uint[tokens.Length + 1];
            tokens.CopyTo(result, 0);
            result[tokens.Length] = token;
            return result;
        }

        private readonly struct Config
        {
            public Config(State<Shape, ShapeNode> state, uint[] tokens)
            {
                State = state;
                Tokens = tokens;
            }

            public State<Shape, ShapeNode> State { get; }
            public uint[] Tokens { get; }
        }

        private readonly struct RootRef
        {
            public RootRef(RootAllomorph allomorph, FeatureStruct category, int stratumIndex)
            {
                Allomorph = allomorph;
                Category = category;
                StratumIndex = stratumIndex;
            }

            public RootAllomorph Allomorph { get; }
            public FeatureStruct Category { get; }
            public int StratumIndex { get; }
        }
    }
}
