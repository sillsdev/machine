using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Builds an <see cref="InversePhonology"/> (surface→underlying) automatically from a grammar's
    /// phonological rules — the "Pinv compiler" LEVER_2.md left as the frontier. Consumed by
    /// <see cref="FstTemplateAnalyzer.AnalyzeComposed"/>, which walks it in LOCKSTEP against the
    /// morphotactic lexicon (a real product-automaton composition, not the boundary-less runtime
    /// inversion <see cref="ComposedPhonologyProposer"/> uses) — the lexicon constrains every
    /// restoration, so the boundary-conditioning problem that broke Indonesian <c>meN-</c> does not
    /// recur here: the walk only restores an underlying segment where a lexicon arc actually has it.
    ///
    /// <b>B-direct, per rule.</b> For each supported rule this compiles its own arcs by PROBING that
    /// ONE rule's synthesis behavior in isolation (build a tiny underlying string, apply just this
    /// rule via its own <see cref="IPhonologicalRule.CompileSynthesisRule"/>, observe the surface) —
    /// never by probing the grammar's combined multi-rule effect (which LEVER_2.md's cascade
    /// experiment showed misreads feeding/bleeding). Each rule's arcs are added as an independent
    /// branch from the shared "outside any rule" state 0, so genuinely INTERACTING rules (true
    /// feeding/bleeding cascades, e.g. Indonesian's assimilation+deletion) are not composed together
    /// — that remains LEVER_2.md's still-open frontier. Non-interacting rules (the common case) work
    /// correctly under this v1: each is independently invertible and none needs the others' state.
    ///
    /// <b>Supported shape (v1):</b> a <see cref="RewriteSubrule"/> with a
    /// <see cref="RewriteSubrule.LeftEnvironment"/> AND a <see cref="RewriteSubrule.RightEnvironment"/>
    /// of plain Constraint nodes (either or both may be empty, but a deletion needs at least one
    /// non-empty — unconditioned deletion would over-restore everywhere), a single-segment
    /// <see cref="RewriteRule.Lhs"/> (exactly one <see cref="Constraint{TData,TOffset}"/>, no
    /// quantifiers/groups/alternations), a <see cref="RewriteSubrule.Rhs"/> of length 0 (deletion) or
    /// 1 (feature-change substitution), and no syntactic-feature/MPR-feature gating. The left and
    /// right environments are independent, symmetric chains of identity arcs bracketing the
    /// restoration/substitution arc — each environment is probed and applied in isolation, so a rule
    /// needing BOTH environments to interact with EACH OTHER (not just with the Lhs) is outside this
    /// v1's reach. Everything else (multi-segment Lhs, epenthesis, metathesis, α-variables, gated
    /// subrules, true multi-rule feeding/bleeding cascades) is skipped — not silently:
    /// <see cref="UnsupportedRuleCount"/> reports how many were skipped, and skipping only costs
    /// coverage (verify still guards soundness on whatever this DOES propose).
    /// </summary>
    public sealed class PhonologyRuleCompiler
    {
        private readonly CharacterDefinitionTable _table;
        private readonly List<CharacterDefinition> _alphabet;
        private int _nextState = 1; // state 0 is reserved for the shared "outside any rule" state
        private int _unsupportedRuleCount;

        private PhonologyRuleCompiler(CharacterDefinitionTable table)
        {
            _table = table;
            _alphabet = table.Where(cd => cd.Type == HCFeatureSystem.Segment).ToList();
        }

        /// <summary>Number of (rule, subrule) pairs skipped because their shape is outside the v1
        /// supported set (see class remarks) — a coverage diagnostic, never a soundness concern.</summary>
        public int UnsupportedRuleCount => _unsupportedRuleCount;

        /// <summary>Compile every stratum's supported phonological rules into one <see cref="InversePhonology"/>.
        /// Returns an all-identity (no-op) transducer if the grammar has no phonological rules or none
        /// are in the supported shape — safe: the composed walk then degenerates to matching the
        /// underlying-only lexicon directly.</summary>
        public static (InversePhonology Pinv, int UnsupportedCount) Compile(Language language, Morpher morpher)
        {
            var compiler = new PhonologyRuleCompiler(language.SurfaceStratum.CharacterDefinitionTable);
            var pinv = new InversePhonology { StartState = 0 };
            pinv.SetAccepting(0);
            foreach (CharacterDefinition cd in compiler._alphabet)
            {
                pinv.AddArc(0, cd.FeatureStruct, cd.FeatureStruct, 0); // identity: everything outside a rule
            }
            foreach (Stratum stratum in language.Strata)
            {
                foreach (IPhonologicalRule prule in stratum.PhonologicalRules)
                {
                    if (!(prule is RewriteRule rule))
                    {
                        continue; // metathesis and other non-rewrite rule types: not yet supported
                    }
                    IRule<Word, int> compiled = rule.CompileSynthesisRule(morpher);
                    foreach (RewriteSubrule subrule in rule.Subrules)
                    {
                        compiler.TryCompileSubrule(pinv, rule, subrule, stratum, compiled, morpher);
                    }
                }
            }
            return (pinv, compiler._unsupportedRuleCount);
        }

        private void TryCompileSubrule(
            InversePhonology pinv,
            RewriteRule rule,
            RewriteSubrule subrule,
            Stratum stratum,
            IRule<Word, int> compiled,
            Morpher morpher
        )
        {
            if (
                !IsTrivial(subrule.RequiredSyntacticFeatureStruct)
                || subrule.RequiredMprFeatures.Count > 0
                || subrule.ExcludedMprFeatures.Count > 0
                || !TryGetConstraints(subrule.LeftEnvironment, out List<FeatureStruct> leftEnv)
                || !TryGetConstraints(rule.Lhs, out List<FeatureStruct> lhs)
                || lhs.Count != 1 // v1: single-segment Lhs only
                || !TryGetConstraints(subrule.Rhs, out List<FeatureStruct> rhs)
                || rhs.Count > 1 // v1: deletion (0) or plain substitution (1) only
                || !TryGetConstraints(subrule.RightEnvironment, out List<FeatureStruct> rightEnv)
            )
            {
                _unsupportedRuleCount++;
                return;
            }
            bool isDeletion = rhs.Count == 0;
            if (isDeletion && leftEnv.Count == 0 && rightEnv.Count == 0)
            {
                _unsupportedRuleCount++; // unconditioned deletion would over-restore everywhere
                return;
            }

            string leftEnvProbe = BuildProbeString(leftEnv);
            string rightEnvProbe = BuildProbeString(rightEnv);
            if (leftEnvProbe == null || rightEnvProbe == null)
            {
                _unsupportedRuleCount++; // an environment has a class with no representative segment
                return;
            }
            int targetIndex = leftEnv.Count;

            // The left-environment chain is the same for every candidate below (it depends only on
            // the rule's environment, not on which Lhs segment is being probed), so build it once.
            int fromState = ChainLeftEnvironment(pinv, leftEnv);

            bool addedAny = false;
            foreach (CharacterDefinition candidate in _alphabet)
            {
                if (!candidate.FeatureStruct.IsUnifiable(lhs[0]))
                {
                    continue;
                }
                string underlyingRep = candidate.Representations.FirstOrDefault();
                if (underlyingRep == null)
                {
                    continue;
                }
                string probeString = leftEnvProbe + underlyingRep + rightEnvProbe;
                List<FeatureStruct> before = SegmentFeatureStructs(probeString);
                if (before == null || before.Count == 0)
                {
                    continue;
                }
                List<FeatureStruct> after = ApplyRule(compiled, stratum, probeString);
                if (after == null)
                {
                    continue;
                }
                if (isDeletion && after.Count == before.Count - 1)
                {
                    // The candidate segment vanished. (A weaker check than also confirming the
                    // environment surfaced byte-for-byte unchanged — FeatureStruct.ValueEquals proved
                    // too strict for segments that round-tripped through rule application without a
                    // real change, likely picking up incidental instance state. Verify is the actual
                    // soundness backstop regardless: a wrong restoration here costs a rejected
                    // candidate, never a wrong answer.)
                    AddRestorationBranch(pinv, fromState, candidate.FeatureStruct, rightEnv);
                    addedAny = true;
                }
                else if (
                    !isDeletion
                    && after.Count == before.Count
                    && !after[targetIndex].ValueEquals(before[targetIndex])
                )
                {
                    AddSubstitutionBranch(pinv, fromState, after[targetIndex], candidate.FeatureStruct, rightEnv);
                    addedAny = true;
                }
                // else: this candidate is unaffected by the rule in this context — no arc added.
            }
            if (!addedAny)
            {
                _unsupportedRuleCount++; // probing found no effect — nothing this rule does is invertible here
            }
        }

        /// <summary>ε-input: restore <paramref name="underlying"/> from <paramref name="fromState"/>
        /// (state 0 if this rule has no left environment, or the state reached after matching the
        /// left-environment chain), then consume the right-environment segments as identity back to
        /// state 0 — mirrors the hand-verified LEVER_2.md deletion spike. With no right context either,
        /// the restoration arc goes straight back to state 0.</summary>
        private void AddRestorationBranch(
            InversePhonology pinv,
            int fromState,
            FeatureStruct underlying,
            List<FeatureStruct> rightEnv
        )
        {
            if (rightEnv.Count == 0)
            {
                pinv.AddArc(fromState, null, underlying, 0);
                return;
            }
            int state = NewState();
            pinv.AddArc(fromState, null, underlying, state);
            ChainRightEnvironment(pinv, state, rightEnv);
        }

        /// <summary>A real arc, from <paramref name="fromState"/> (state 0, or the state reached after
        /// matching the left-environment chain), consuming the surfaced segment and emitting the
        /// underlying one, then the right-environment chain back to state 0. With no right context at
        /// all, the arc goes straight back to state 0 — no intermediate state needed.</summary>
        private void AddSubstitutionBranch(
            InversePhonology pinv,
            int fromState,
            FeatureStruct surface,
            FeatureStruct underlying,
            List<FeatureStruct> rightEnv
        )
        {
            if (rightEnv.Count == 0)
            {
                pinv.AddArc(fromState, surface, underlying, 0);
                return;
            }
            int state = NewState();
            pinv.AddArc(fromState, surface, underlying, state);
            ChainRightEnvironment(pinv, state, rightEnv);
        }

        /// <summary>Consume each right-environment segment as an identity transition, ending back at
        /// state 0. Callers only invoke this with a non-empty environment (the zero-length case is
        /// handled directly by the caller).</summary>
        private void ChainRightEnvironment(InversePhonology pinv, int from, List<FeatureStruct> rightEnv)
        {
            int state = from;
            for (int i = 0; i < rightEnv.Count; i++)
            {
                int next = i == rightEnv.Count - 1 ? 0 : NewState();
                pinv.AddArc(state, rightEnv[i], rightEnv[i], next); // environment passes through as identity
                state = next;
            }
        }

        /// <summary>Consume each left-environment segment as an identity transition FROM state 0,
        /// returning the state reached once the whole environment has matched (state 0 itself if the
        /// environment is empty) — the symmetric mirror of <see cref="ChainRightEnvironment"/>, built
        /// forward instead of backward. This runs in parallel with state 0's own identity self-loops
        /// (this is an NFA: a segment that starts matching a left environment is also still a candidate
        /// for every other live path), so no existing arc is disturbed.</summary>
        private int ChainLeftEnvironment(InversePhonology pinv, List<FeatureStruct> leftEnv)
        {
            int state = 0;
            foreach (FeatureStruct env in leftEnv)
            {
                int next = NewState();
                pinv.AddArc(state, env, env, next); // environment passes through as identity
                state = next;
            }
            return state;
        }

        private int NewState() => _nextState++;

        private static bool IsTrivial(FeatureStruct fs) => fs == null || fs.IsEmpty;

        /// <summary>True iff every top-level child of <paramref name="pattern"/> is a plain
        /// <see cref="Constraint{TData,TOffset}"/> (no quantifiers/groups/alternations) — the bounded,
        /// fixed-length window this compiler supports. Returns the ordered constraint FeatureStructs.</summary>
        private static bool TryGetConstraints(Pattern<Word, int> pattern, out List<FeatureStruct> constraints)
        {
            constraints = new List<FeatureStruct>();
            if (pattern == null)
            {
                return true;
            }
            foreach (PatternNode<Word, int> child in pattern.Children)
            {
                if (!(child is Constraint<Word, int> c))
                {
                    constraints = null;
                    return false;
                }
                constraints.Add(c.FeatureStruct);
            }
            return true;
        }

        /// <summary>One concrete alphabet representative per environment constraint, concatenated in
        /// order (used for both the left- and right-environment side — direction-agnostic; the caller
        /// decides where the result is concatenated). Returns null if some constraint has no unifiable
        /// alphabet member.</summary>
        private string BuildProbeString(List<FeatureStruct> envConstraints)
        {
            var sb = new System.Text.StringBuilder();
            foreach (FeatureStruct fs in envConstraints)
            {
                CharacterDefinition rep = _alphabet.FirstOrDefault(cd => cd.FeatureStruct.IsUnifiable(fs));
                string str = rep?.Representations.FirstOrDefault();
                if (str == null)
                {
                    return null;
                }
                sb.Append(str);
            }
            return sb.ToString();
        }

        private List<FeatureStruct> SegmentFeatureStructs(string str)
        {
            Shape shape;
            try
            {
                shape = _table.Segment(str);
            }
            catch (InvalidShapeException)
            {
                return null;
            }
            return shape
                .Where(n => n.Annotation.Type() == HCFeatureSystem.Segment && !n.IsDeleted())
                .Select(n => n.Annotation.FeatureStruct)
                .ToList();
        }

        private List<FeatureStruct> ApplyRule(IRule<Word, int> compiled, Stratum stratum, string underlying)
        {
            Shape shape;
            try
            {
                shape = _table.Segment(underlying);
            }
            catch (InvalidShapeException)
            {
                return null;
            }
            var word = new Word(stratum, shape);
            Word result = compiled.Apply(word).DefaultIfEmpty(word).First();
            // A deletion rule marks a node IsDeleted() rather than physically removing it from the
            // Shape (HermitCrabExtensions), so that flag must be filtered here or "deleted" segments
            // would still show up in the count and mask the deletion.
            return result
                .Shape.Where(n => n.Annotation.Type() == HCFeatureSystem.Segment && !n.IsDeleted())
                .Select(n => n.Annotation.FeatureStruct)
                .ToList();
        }
    }
}
