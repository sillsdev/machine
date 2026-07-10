using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.FiniteState;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Rules;
using SIL.ObjectModel;
#if OUTPUT_ANALYSES
using System.IO;
#endif

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <remarks>
    /// <para>
    /// <b>Corpus-batch hosts running Server GC MUST set a heap hard limit</b> (e.g.
    /// <c>DOTNET_GCHeapHardLimit</c> or <c>GCHeapHardLimitPercent</c>) when parallelizing across
    /// words (parse-optimization.md Phase 8, see also <see cref="Morpher(ITraceManager, Language,
    /// int)"/>'s <c>maxDegreeOfParallelism</c> remarks). Measured 2026-07-03: 16-way concurrency
    /// with Server GC and no limit reached 45GB on a 64GB host and had to be killed; the same
    /// workload with <c>DOTNET_GCHeapHardLimit=0x600000000</c> (24GB) completed. A follow-up
    /// measurement (13 of the heaviest known Sena words, all running concurrently at once --
    /// a harder case than a real mixed corpus, where lighter words finish early and relieve
    /// pressure) found this is not always free: wall-clock rose ~30-45% under the same limit
    /// versus unlimited (e.g. one word went 96.9s → 130.8s) even though every word still
    /// completed and results stayed byte-identical. The blowup is <em>not</em> the per-parse memo
    /// tables (<see cref="AnalysisScope.Memo"/>/<see cref="AnalysisScope.TemplateMemo"/>) retaining
    /// too much -- measured at 6K-8K and 35K-58K stored <see cref="Word"/> instances respectively
    /// for the heaviest known words, tens of MB at most given <see cref="Shape"/>'s and
    /// <see cref="FeatureStruct"/>'s copy-on-write sharing -- it is Server GC deferring collection
    /// of the much larger volume of transient search/replay garbage for throughput, under
    /// concurrent heavy-word pressure. Set a limit sized to what the host can spare, and expect a
    /// real (not cosmetic) throughput/memory trade-off under sustained all-heavy concurrent load;
    /// do not assume the limit is a free safety net on every workload shape.
    /// </para>
    /// </remarks>
    public class Morpher : IMorphologicalAnalyzer, IMorphologicalGenerator
    {
        private readonly Language _lang;
        private readonly IRule<Word, int> _analysisRule;
        private readonly IRule<Word, int> _synthesisRule;
        private readonly Dictionary<Stratum, RootAllomorphTrie> _allomorphTries;
        private readonly Dictionary<Stratum, List<RootAllomorphTrie>> _reachabilityTries;
        private readonly bool _lexicalGatingQualified;
        private readonly ITraceManager _traceManager;
        private readonly ReadOnlyObservableCollection<Morpheme> _morphemes;
        private readonly IList<RootAllomorph> _lexicalPatterns = new List<RootAllomorph>();

        public Morpher(ITraceManager traceManager, Language lang)
            : this(traceManager, lang, -1) { }

        /// <param name="maxDegreeOfParallelism">
        /// Caps the parallelism used <em>within</em> a single parse. A value of 1 makes the
        /// morpher fully single-threaded (analysis cascade, affix-template unapplication, and
        /// synthesis all run sequentially) — this is the mode a caller should use when it
        /// parallelizes <em>across</em> words itself (e.g. FieldWorks' "Parse All Words"), to
        /// avoid nested parallelism / thread-pool oversubscription. Any value &lt;= 0 defaults
        /// to <see cref="Environment.ProcessorCount"/> (the historical behavior).
        /// </param>
        public Morpher(ITraceManager traceManager, Language lang, int maxDegreeOfParallelism)
        {
            _lang = lang;
            _traceManager = traceManager;
            // Must be set before CompileAnalysisRule: the analysis rules choose a sequential vs.
            // parallel cascade at construction time based on this value.
            MaxDegreeOfParallelism = maxDegreeOfParallelism <= 0 ? Environment.ProcessorCount : maxDegreeOfParallelism;
            _allomorphTries = new Dictionary<Stratum, RootAllomorphTrie>();
            var morphemes = new ObservableList<Morpheme>();
            foreach (Stratum stratum in _lang.Strata)
            {
                var allomorphs = new HashSet<RootAllomorph>(stratum.Entries.SelectMany(entry => entry.Allomorphs));
                var trie = new RootAllomorphTrie(ann => ann.Type() == HCFeatureSystem.Segment);
                foreach (RootAllomorph allomorph in allomorphs)
                {
                    if (allomorph.IsPattern)
                        _lexicalPatterns.Add(allomorph);
                    else
                        trie.Add(allomorph);
                }
                _allomorphTries[stratum] = trie;

                morphemes.AddRange(stratum.Entries);
                morphemes.AddRange(stratum.MorphologicalRules.OfType<AffixProcessRule>());
                morphemes.AddRange(stratum.AffixTemplates.SelectMany(t => t.Slots).SelectMany(s => s.Rules).Distinct());
            }
            // parse-optimization.md Phase 5: for each stratum, the tries of itself and every stratum
            // "deeper" than it -- deeper meaning closer to the root, i.e. earlier in Language.Strata's own
            // (root-most-first) order, since AnalysisLanguageRule walks strata in the OPPOSITE order
            // (Reverse(), surface-first) and a candidate currently at stratum S can still be transformed
            // by every stratum S has yet to reach on its way to the root.
            _reachabilityTries = new Dictionary<Stratum, List<RootAllomorphTrie>>();
            var soFar = new List<RootAllomorphTrie>();
            foreach (Stratum stratum in _lang.Strata)
            {
                soFar.Add(_allomorphTries[stratum]);
                _reachabilityTries[stratum] = new List<RootAllomorphTrie>(soFar);
            }
            _lexicalGatingQualified = GrammarAnalyzer.IsEdgeStripperQualified(_lang);

            _analysisRule = lang.CompileAnalysisRule(this);
            _synthesisRule = lang.CompileSynthesisRule(this);
            ((InstrumentedRule<Word, int>)_synthesisRule).Name = "Synthesis";
            MaxStemCount = 2;
            MaxUnapplications = 0;
            MergeEquivalentAnalyses = true;
            LexEntrySelector = entry => true;
            RuleSelector = rule => true;

            _morphemes = new ReadOnlyObservableCollection<Morpheme>(morphemes);
        }

        public ITraceManager TraceManager
        {
            get { return _traceManager; }
        }

        public int DeletionReapplications { get; set; }

        private int? _maxAnalysisLengthOverride;
        private bool _maxAnalysisLengthOverrideSet;

        /// <summary>
        /// The longest underlying form (in real segments, i.e. <see cref="HermitCrabExtensions.SegmentCount"/>)
        /// any analysis candidate can be before it is pruned as unreachable (parse-optimization.md Phase 4's
        /// "Gate B") -- auto-derived from the grammar (<see cref="GrammarAnalyzer.ComputeMaxAnalysisLength"/>:
        /// the longest lexicon root plus every rule's own maximum possible insertion) unless explicitly set.
        /// Setting this (including to <c>null</c>, which disables the gate entirely) overrides the
        /// auto-derived value; re-derived fresh from the current grammar on every read otherwise, so it
        /// never goes stale if the grammar changes after construction. Auto-derives to <c>null</c> (gate
        /// off) when the grammar contains a compounding rule or a phonological rule shape this analysis
        /// can't measure exactly -- see <see cref="GrammarAnalyzer"/>'s remarks.
        /// </summary>
        public int? MaxAnalysisLength
        {
            get
            {
                return _maxAnalysisLengthOverrideSet
                    ? _maxAnalysisLengthOverride
                    : GrammarAnalyzer.ComputeMaxAnalysisLength(_lang);
            }
            set
            {
                _maxAnalysisLengthOverride = value;
                _maxAnalysisLengthOverrideSet = true;
            }
        }

        /// <summary>
        /// parse-optimization.md Phase 5: prune an analysis subtree before descending into it when no root
        /// in the current stratum (or any stratum deeper than it) can match ANY contiguous window of the
        /// candidate's current shape -- see <see cref="GrammarAnalyzer.IsEdgeStripperQualified"/> and
        /// <see cref="RootAllomorphTrie.ContainsRootAnywhere"/>. <b>Default off</b>, as the plan requires:
        /// even when set, the gate only actually activates for a given parse when the grammar itself
        /// qualifies (<see cref="GrammarAnalyzer.IsEdgeStripperQualified"/>, checked once at construction)
        /// and the call isn't tracing or root-guessing (<see cref="ParseWord(string, out object, bool)"/>'s
        /// <c>guessRoot</c> synthesizes from lexical PATTERNS, bypassing the real lexicon entirely, so a
        /// real-lexicon reachability gate would be unsound applied to it). This is the plan's own
        /// highest-risk phase; turn on only after the corpus A/B protocol in parse-optimization.md's Phase
        /// 5 section holds for your grammar.
        /// </summary>
        public bool EnableLexicalGating { get; set; }

        /// <summary>Reachability check backing <see cref="EnableLexicalGating"/> -- see its remarks.</summary>
        internal bool HasReachableRoot(Word word)
        {
            return _reachabilityTries[word.Stratum].Any(trie => trie.ContainsRootAnywhere(word.Shape));
        }

        public int MaxStemCount { get; set; }

        /// <summary>
        /// MaxUnapplications limits the number of unapplications to make it possible
        /// to make it possible to debug words that take 30 minutes to parse
        /// because there are too many unapplications.
        /// </summary>
        public int MaxUnapplications { get; set; }

        /// <summary>
        /// Merge analyses that have equivalent shapes.
        /// Merged analyses will be expanded if lexical lookup succeeds.
        /// </summary>
        public bool MergeEquivalentAnalyses { get; set; }

        /// <summary>
        /// Caps parallelism used within a single parse; 1 = fully single-threaded.
        /// Set via the constructor (it influences how the analysis rules are compiled).
        /// </summary>
        public int MaxDegreeOfParallelism { get; }

        public Func<LexEntry, bool> LexEntrySelector { get; set; }
        public Func<IHCRule, bool> RuleSelector { get; set; }

        public Language Language
        {
            get { return _lang; }
        }

        /// <summary>
        /// When true, ParseWord does not clear rule stats (InstrumentedRule.InputCount/OutputCount/
        /// ElapsedTime/BucketGroups) at the start of each parse, so they accumulate across an entire corpus
        /// batch instead of reflecting only the most recent word. Off by default: existing single-word
        /// callers (e.g. an interactive "why didn't this parse" UI) expect ClearStats every call. The rule
        /// tree is shared across every ParseWord call on this Morpher, so a caller enabling this on a Morpher
        /// used from multiple threads is responsible for keeping calls single-threaded (see
        /// MemoizedCombinationRuleCascade's doc comment on maxDegreeOfParallelism: 1 for corpus-batch runs).
        /// </summary>
        public bool AccumulateRuleStats { get; set; }

        public InstrumentedRule<Word, int> AnalysisRuleStats => _analysisRule as InstrumentedRule<Word, int>;

        public InstrumentedRule<Word, int> SynthesisRuleStats => _synthesisRule as InstrumentedRule<Word, int>;

        /// <summary>
        /// Parses the specified surface form.
        /// </summary>
        public IEnumerable<Word> ParseWord(string word)
        {
            return ParseWord(word, out _, false);
        }

        public IEnumerable<Word> ParseWord(string word, out object trace)
        {
            return ParseWord(word, out trace, false);
        }

        /// <summary>
        /// Parse the specified surface form, possibly tracing the parse.
        /// If there are no analyses and guessRoot is true, then guess the root.
        /// </summary>
        public IEnumerable<Word> ParseWord(string word, out object trace, bool guessRoot)
        {
            // convert the word to its phonetic shape
            Shape shape = _lang.SurfaceStratum.CharacterDefinitionTable.Segment(word);

            var input = new Word(_lang.SurfaceStratum, shape);
            // Skipped while tracing: the nogood cascade this backs skips expansions outright on a hit,
            // which would also skip the trace events those expansions fire (parse-optimization.md Phase 2
            // ground rules -- traces must stay byte-identical to the unmemoized engine).
            if (!_traceManager.IsTracing)
            {
                // Phase 5's lexical gate is unsound under guessRoot (it synthesizes from lexical PATTERNS,
                // bypassing the real lexicon the gate's reachability index is built from) -- this check
                // covers the whole parse, not just guessRoot's own fallback branch further down, since the
                // gate would otherwise have already pruned candidates during _analysisRule.Apply below,
                // before guessRoot's branch ever runs.
                bool lexicalGatingActive = EnableLexicalGating && _lexicalGatingQualified && !guessRoot;
                input.AnalysisScope = new AnalysisScope(lexicalGatingActive);
            }
            input.Freeze();
            if (_traceManager.IsTracing)
                _traceManager.AnalyzeWord(_lang, input);
            trace = input.CurrentTrace;

            if (!AccumulateRuleStats)
            {
                AnalysisRuleStats?.ClearStats();
                SynthesisRuleStats?.ClearStats();
            }

            // Unapply rules
            IList<Word> analyses = _analysisRule.Apply(input).ToList();

#if OUTPUT_ANALYSES
            var lines = new List<string>();
            foreach (Word w in analyses)
            {
                string shapeStr = w.ToString();
                string rulesStr = string.Join(", ", w.MorphologicalRules.Select(r => r.Name));
                lines.Add(string.Format("{0} : {1}", shapeStr, rulesStr));
            }

            File.WriteAllLines("analyses.txt", lines.OrderBy(l => l));
#endif
            // analyses is already materialized and Synthesize doesn't mutate it, so no copy needed.
            IList<Word> origAnalyses = guessRoot ? analyses : null;
            IList<Word> syntheses = Synthesize(word, analyses).ToList();
            if (guessRoot && syntheses.Count == 0)
            {
                // Guess roots when there are no results.
                List<Word> matches = new List<Word>();
                foreach (Word analysisWord in origAnalyses)
                {
                    var lexicalGuesses = LexicalGuess(analysisWord).Distinct();
                    foreach (Word synthesisWord in lexicalGuesses)
                    {
                        foreach (Word alternative in synthesisWord.ExpandAlternatives())
                        {
                            foreach (Word validWord in _synthesisRule.Apply(alternative).Where(IsWordValid))
                            {
                                if (IsMatch(word, validWord))
                                    matches.Add(validWord);
                            }
                        }
                    }
                }

                matches.Sort((x, y) => y.Morphs.Count().CompareTo(x.Morphs.Count()));

                return matches;
            }
            return syntheses;
        }

        /// <summary>
        /// Generates surface forms from the specified word synthesis information.
        /// </summary>
        public IEnumerable<string> GenerateWords(
            LexEntry rootEntry,
            IEnumerable<Morpheme> otherMorphemes,
            FeatureStruct realizationalFS
        )
        {
            return GenerateWords(rootEntry, otherMorphemes, realizationalFS, out _);
        }

        public IEnumerable<string> GenerateWords(
            LexEntry rootEntry,
            IEnumerable<Morpheme> otherMorphemes,
            FeatureStruct realizationalFS,
            out object trace
        )
        {
            Stack<Tuple<IMorphologicalRule, RootAllomorph>>[] rulePermutations = PermuteRules(otherMorphemes.ToArray())
                .ToArray();

            object rootTrace = _traceManager.IsTracing ? _traceManager.GenerateWords(_lang) : null;
            trace = rootTrace;

            var words = new ConcurrentBag<string>();

            Exception exception = null;
            Parallel.ForEach(
                rootEntry.Allomorphs.SelectMany(
                    a => rulePermutations,
                    (a, p) => new { Allomorph = a, RulePermutation = p }
                ),
                new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism },
                (synthesisInfo, state) =>
                {
                    try
                    {
                        var synthesisWord = new Word(synthesisInfo.Allomorph, realizationalFS);
                        foreach (Tuple<IMorphologicalRule, RootAllomorph> rule in synthesisInfo.RulePermutation)
                        {
                            synthesisWord.MorphologicalRuleUnapplied(rule.Item1);
                            if (rule.Item2 != null)
                                synthesisWord.NonHeadUnapplied(new Word(rule.Item2, new FeatureStruct()));
                        }

                        synthesisWord.CurrentTrace = rootTrace;

                        if (_traceManager.IsTracing)
                            _traceManager.SynthesizeWord(_lang, synthesisWord);

                        synthesisWord.Freeze();

                        foreach (Word validWord in _synthesisRule.Apply(synthesisWord).Where(IsWordValid))
                        {
                            if (_traceManager.IsTracing)
                                _traceManager.Successful(_lang, validWord);
                            words.Add(validWord.Shape.ToString(_lang.SurfaceStratum.CharacterDefinitionTable, false));
                        }
                    }
                    catch (Exception e)
                    {
                        state.Stop();
                        exception = e;
                    }
                }
            );

            if (exception != null)
                throw exception;

            return words.Distinct();
        }

        private IEnumerable<Stack<Tuple<IMorphologicalRule, RootAllomorph>>> PermuteRules(
            Morpheme[] morphemes,
            int index = 0
        )
        {
            if (index == morphemes.Length)
            {
                yield return new Stack<Tuple<IMorphologicalRule, RootAllomorph>>();
            }
            else
            {
                if (morphemes[index] is LexEntry entry)
                {
                    foreach (RootAllomorph allo in entry.Allomorphs)
                    {
                        foreach (
                            Stack<Tuple<IMorphologicalRule, RootAllomorph>> permutation in PermuteRules(
                                morphemes,
                                index + 1
                            )
                        )
                        {
                            permutation.Push(Tuple.Create((IMorphologicalRule)null, allo));
                            yield return permutation;
                        }
                    }
                }
                else
                {
                    foreach (
                        Stack<Tuple<IMorphologicalRule, RootAllomorph>> permutation in PermuteRules(
                            morphemes,
                            index + 1
                        )
                    )
                    {
                        permutation.Push(Tuple.Create((IMorphologicalRule)morphemes[index], (RootAllomorph)null));
                        yield return permutation;
                    }
                }
            }
        }

        private IEnumerable<Word> Synthesize(string word, IList<Word> analyses)
        {
            // Single-threaded: used when the caller parallelizes across words itself.
            if (MaxDegreeOfParallelism == 1)
            {
                var matches = new HashSet<Word>(FreezableEqualityComparer<Word>.Default);
                foreach (Word analysisWord in analyses)
                {
                    foreach (Word validWord in SynthesizeAnalysis(word, analysisWord))
                        matches.Add(validWord);
                }
                return matches;
            }

            // Parallel across the candidate analyses of this one word.
            var parallelMatches = new ConcurrentBag<Word>();
            Exception exception = null;
            Parallel.ForEach(
                analyses,
                new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism },
                (analysisWord, state) =>
                {
                    try
                    {
                        foreach (Word validWord in SynthesizeAnalysis(word, analysisWord))
                            parallelMatches.Add(validWord);
                    }
                    catch (Exception e)
                    {
                        state.Stop();
                        exception = e;
                    }
                }
            );
            if (exception != null)
                throw exception;
            return parallelMatches.Distinct(FreezableEqualityComparer<Word>.Default);
        }

        private IEnumerable<Word> SynthesizeAnalysis(string word, Word analysisWord)
        {
            // Gate A from parse-optimization.md's Phase 4 sketch (pre-phonology length-vs-target pruning)
            // was attempted and reverted here: `alternative` at this point is still essentially the bare
            // root allomorph -- the pending affix trail's own insertions haven't been applied yet, they
            // happen inside _synthesisRule.Apply below alongside phonology -- so comparing its length to
            // the target surface length without also accounting for that trail's own insertions produced
            // false rejections (confirmed against the unit suite: CompoundingRuleTests/MetathesisRuleTests
            // regressed). A correct version would need to sum each pending trail rule's own max insertion
            // (GrammarAnalyzer already computes this per-rule for Gate B) rather than compare bare-root
            // length directly -- left as follow-up, not attempted this pass.
            foreach (Word synthesisWord in LexicalLookup(analysisWord))
            {
                foreach (Word alternative in synthesisWord.ExpandAlternatives())
                {
                    foreach (Word validWord in _synthesisRule.Apply(alternative).Where(IsWordValid))
                    {
                        if (IsMatch(word, validWord))
                            yield return validWord;
                    }
                }
            }
        }

        internal IEnumerable<RootAllomorph> SearchRootAllomorphs(Stratum stratum, Shape shape)
        {
            RootAllomorphTrie alloSearcher = _allomorphTries[stratum];
            return alloSearcher.Search(shape).Distinct();
        }

        private IEnumerable<Word> LexicalLookup(Word input)
        {
            if (_traceManager.IsTracing)
                _traceManager.LexicalLookup(input.Stratum, input);
            foreach (
                LexEntry entry in SearchRootAllomorphs(input.Stratum, input.Shape)
                    .Select(allo => allo.Morpheme)
                    .Cast<LexEntry>()
                    .Where(LexEntrySelector)
                    .Distinct()
            )
            {
                foreach (RootAllomorph allomorph in entry.Allomorphs)
                {
                    Word newWord = input.Clone();
                    newWord.RootAllomorph = allomorph;
                    if (_traceManager.IsTracing)
                        _traceManager.SynthesizeWord(_lang, newWord);
                    newWord.Freeze();
                    yield return newWord;
                }
            }
        }

        /// <summary>
        /// Match the input against lexical patterns and return matches.
        /// </summary>
        private IEnumerable<Word> LexicalGuess(Word input)
        {
            if (_traceManager.IsTracing)
                _traceManager.LexicalLookup(input.Stratum, input);
            CharacterDefinitionTable table = input.Stratum.CharacterDefinitionTable;
            IEnumerable<ShapeNode> shapeNodes = input.Shape.GetNodes(input.Shape.Range);
            foreach (RootAllomorph lexicalPattern in _lexicalPatterns)
            {
                HashSet<string> shapeSet = new HashSet<string>();
                IEnumerable<ShapeNode> shapePattern = lexicalPattern.Segments.Shape.GetNodes(
                    lexicalPattern.Segments.Shape.Range
                );
                foreach (List<ShapeNode> match in MatchNodesWithPattern(shapeNodes.ToList(), shapePattern.ToList()))
                {
                    IEnumerable<string> shapeStrings = new List<string>() { match.ToString(table, false) };
                    // We could set shapeStrings to GetShapeStrings(match, table),
                    // but that produces spurious ambiguities that don't seem to have any value.
                    foreach (string shapeString in shapeStrings)
                    {
                        if (shapeSet.Contains(shapeString))
                            // Avoid duplicates caused by multiple paths through pattern (e.g. ([Seg])([Seg])).
                            continue;
                        shapeSet.Add(shapeString);
                        // Create a root allomorph for the guess.
                        var root = new RootAllomorph(new Segments(table, shapeString)) { Guessed = true };
                        root.AllomorphCoOccurrenceRules.AddRange(lexicalPattern.AllomorphCoOccurrenceRules);
                        root.Environments.AddRange(lexicalPattern.Environments);
                        root.Properties.AddRange(lexicalPattern.Properties);
                        root.StemName = lexicalPattern.StemName;
                        root.IsBound = lexicalPattern.IsBound;
                        // Create a lexical entry to hold the root allomorph.
                        // (The root's Morpheme will point to the lexical entry.)
                        var lexEntry = new LexEntry
                        {
                            Id = shapeString,
                            Gloss = shapeString,
                            IsPartial = input.SyntacticFeatureStruct.IsEmpty,
                            SyntacticFeatureStruct = input.SyntacticFeatureStruct,
                            Stratum = input.Stratum,
                        };
                        lexEntry.Allomorphs.Add(root);
                        // Point the root allomorph to the lexical pattern in FieldWorks.
                        if (lexicalPattern.Morpheme != null)
                        {
                            // Copy Morpheme fields.
                            Morpheme morpheme = lexicalPattern.Morpheme;
                            lexEntry.MorphemeCoOccurrenceRules.AddRange(morpheme.MorphemeCoOccurrenceRules);
                            lexEntry.Properties.AddRange(morpheme.Properties);
                            lexEntry.Stratum = morpheme.Stratum;
                            LexEntry patternEntry = (LexEntry)morpheme;
                            if (patternEntry != null)
                            {
                                // Copy LexEntry fields.
                                lexEntry.MprFeatures = patternEntry.MprFeatures;
                                lexEntry.SyntacticFeatureStruct = patternEntry.SyntacticFeatureStruct;
                                lexEntry.IsPartial = patternEntry.IsPartial;
                            }
                        }
                        // Create a new word that uses the root allomorph.
                        Word newWord = input.Clone();
                        newWord.RootAllomorph = root;
                        if (_traceManager.IsTracing)
                            _traceManager.SynthesizeWord(_lang, newWord);
                        newWord.Freeze();
                        yield return newWord;
                    }
                }
            }
        }

        /// <summary>
        /// Match the shape nodes against the shape pattern.
        /// This can produce multiple outputs if there is more than one path.
        /// The outputs can be different because it unifies the nodes.
        /// </summary>
        public IEnumerable<List<ShapeNode>> MatchNodesWithPattern(
            IList<ShapeNode> nodes,
            IList<ShapeNode> pattern,
            int n = 0,
            int p = 0,
            bool obligatory = false,
            List<ShapeNode> prefix = null
        )
        {
            var results = new List<List<ShapeNode>>();
            if (prefix == null)
                prefix = new List<ShapeNode>();
            if (pattern.Count == p)
            {
                if (nodes.Count == n)
                    // We match because we are at the end of both the pattern and the nodes.
                    results.Add(prefix);
                return results;
            }
            if (pattern[p].Annotation.Optional && !obligatory)
                // Try skipping this item in the pattern.
                results.AddRange(MatchNodesWithPattern(nodes, pattern, n, p + 1, false, prefix));
            if (nodes.Count == n)
            {
                // We fail to match because we are at the end of the nodes but not the pattern.
                return results;
            }
            ShapeNode newNode = UnifyShapeNodes(nodes[n], pattern[p]);
            if (newNode == null)
                // We fail because the pattern didn't match the node here.
                return results;
            // Make a copy of prefix to avoid crosstalk and add newNode.
            prefix = new List<ShapeNode>(prefix) { newNode };
            if (pattern[p].IsIterative())
                // Try using this item in the pattern again.
                results.AddRange(MatchNodesWithPattern(nodes, pattern, n + 1, p, true, prefix));
            // Try the remainder of the nodes against the remainder of the pattern.
            results.AddRange(MatchNodesWithPattern(nodes, pattern, n + 1, p + 1, false, prefix));
            return results;
        }

        ShapeNode UnifyShapeNodes(ShapeNode node, ShapeNode pattern)
        {
            FeatureStruct fs = null;
            node.Annotation.FeatureStruct.Unify(pattern.Annotation.FeatureStruct, out fs);
            if (fs == null)
                return null;
            if (fs.ValueEquals(node.Annotation.FeatureStruct))
                return node;
            return new ShapeNode(fs);
        }

        private IEnumerable<string> GetShapeStrings(IList<ShapeNode> nodes, CharacterDefinitionTable table)
        {
            IList<string> strings = new List<string>();
            if (nodes.Count == 0)
            {
                // We are at the end of the nodes.
                strings.Add("");
                return strings;
            }

            // Pop the first node.
            ShapeNode node = nodes[0];
            nodes.RemoveAt(0);

            // Get suffixes.
            IEnumerable<string> suffixes = GetShapeStrings(nodes, table);
            if ((node.Annotation.Type() == HCFeatureSystem.Boundary) || node.IsDeleted())
                // Skip this node.
                return suffixes;
            IEnumerable<string> strReps = table.GetMatchingStrReps(node);
            if (strReps.Count() == 0)
                // Skip this node;
                return suffixes;

            // Get string reps with unique feature structures.
            IList<string> uniqueStrReps = new List<string>();
            foreach (string strRep in strReps)
            {
                CharacterDefinition cd = table[strRep];
                bool found = false;
                foreach (string uniqueStrRep in uniqueStrReps)
                {
                    CharacterDefinition uniqueCd = table[uniqueStrRep];
                    if (uniqueCd.FeatureStruct.ValueEquals(cd.FeatureStruct))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    uniqueStrReps.Add(strRep);
            }

            // take the cross-product of uniqueStrReps and suffixes.
            foreach (string uniqueStrRep in uniqueStrReps)
            {
                foreach (string suffix in suffixes)
                    strings.Add(uniqueStrRep + suffix);
            }
            return strings;
        }

        private bool IsWordValid(Word word)
        {
            if (
                !word.RealizationalFeatureStruct.IsUnifiable(word.SyntacticFeatureStruct)
                || !word.IsAllMorphologicalRulesApplied
            )
            {
                if (_traceManager.IsTracing)
                    _traceManager.Failed(_lang, word, FailureReason.PartialParse, null, null);
                return false;
            }

            Feature feature = word.ObligatorySyntacticFeatures.FirstOrDefault(f =>
                !ContainsFeature(
                    word.SyntacticFeatureStruct,
                    f,
                    new HashSet<FeatureStruct>(new ReferenceEqualityComparer<FeatureStruct>())
                )
            );
            if (feature != null)
            {
                if (_traceManager.IsTracing)
                    _traceManager.Failed(_lang, word, FailureReason.ObligatorySyntacticFeatures, null, feature);
                return false;
            }

            return word.Allomorphs.All(allo => allo.IsWordValid(this, word));
        }

        private bool IsMatch(string word, Word validWord)
        {
            if (_lang.SurfaceStratum.CharacterDefinitionTable.IsMatch(word, validWord.Shape))
            {
                if (_traceManager.IsTracing)
                    _traceManager.Successful(_lang, validWord);
                return true;
            }
            else if (_traceManager.IsTracing)
            {
                _traceManager.Failed(_lang, validWord, FailureReason.SurfaceFormMismatch, null, word);
            }
            return false;
        }

        private bool ContainsFeature(FeatureStruct fs, Feature feature, ISet<FeatureStruct> visited)
        {
            if (visited.Contains(fs))
                return false;

            if (fs.ContainsFeature(feature))
                return true;

            if (fs.Features.OfType<ComplexFeature>().Any(cf => ContainsFeature(fs.GetValue(cf), feature, visited)))
                return true;

            return false;
        }

        public IEnumerable<WordAnalysis> AnalyzeWord(string word)
        {
            try
            {
                return ParseWord(word).Select(CreateWordAnalysis);
            }
            catch (InvalidShapeException)
            {
                return Enumerable.Empty<WordAnalysis>();
            }
        }

        public IEnumerable<WordAnalysis> AnalyzeWord(string word, bool guessRoot)
        {
            try
            {
                return ParseWord(word, out _, guessRoot).Select(CreateWordAnalysis);
            }
            catch (InvalidShapeException)
            {
                return Enumerable.Empty<WordAnalysis>();
            }
        }

        private WordAnalysis CreateWordAnalysis(Word result)
        {
            int rootMorphemeIndex = -1;
            var morphemes = new List<IMorpheme>();
            int i = 0;
            foreach (Allomorph allo in result.AllomorphsInMorphOrder)
            {
                morphemes.Add(allo.Morpheme);
                if (allo == result.RootAllomorph)
                    rootMorphemeIndex = i;
                i++;
            }

            FeatureSymbol pos = result.SyntacticFeatureStruct.PartsOfSpeech().FirstOrDefault();
            return new WordAnalysis(morphemes, rootMorphemeIndex, pos?.ID);
        }

        public IReadOnlyObservableCollection<IMorpheme> Morphemes
        {
            get { return _morphemes; }
        }

        public IEnumerable<string> GenerateWords(WordAnalysis wordAnalysis)
        {
            if (wordAnalysis.Morphemes.Count == 0)
                return Enumerable.Empty<string>();

            List<Morpheme> morphemes = wordAnalysis.Morphemes.Cast<Morpheme>().ToList();
            var rootEntry = (LexEntry)morphemes[wordAnalysis.RootMorphemeIndex];
            var realizationalFS = new FeatureStruct();
            var results = new HashSet<string>();
            foreach (
                Stack<Morpheme> otherMorphemes in PermuteOtherMorphemes(
                    morphemes,
                    wordAnalysis.RootMorphemeIndex - 1,
                    wordAnalysis.RootMorphemeIndex + 1
                )
            )
            {
                results.UnionWith(GenerateWords(rootEntry, otherMorphemes, realizationalFS));
            }
            return results;
        }

        private IEnumerable<Stack<Morpheme>> PermuteOtherMorphemes(
            List<Morpheme> morphemes,
            int leftIndex,
            int rightIndex
        )
        {
            if (leftIndex == -1 && rightIndex == morphemes.Count)
            {
                yield return new Stack<Morpheme>();
            }
            else
            {
                if (rightIndex < morphemes.Count)
                {
                    foreach (Stack<Morpheme> p in PermuteOtherMorphemes(morphemes, leftIndex, rightIndex + 1))
                    {
                        p.Push(morphemes[rightIndex]);
                        yield return p;
                    }
                }

                if (leftIndex > -1)
                {
                    foreach (Stack<Morpheme> p in PermuteOtherMorphemes(morphemes, leftIndex - 1, rightIndex))
                    {
                        p.Push(morphemes[leftIndex]);
                        yield return p;
                    }
                }
            }
        }
    }
}
