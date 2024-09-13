using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Rules;
using SIL.ObjectModel;
#if OUTPUT_ANALYSES
using System.IO;
#endif

#if !SINGLE_THREADED
using System.Collections.Concurrent;
using System.Threading.Tasks;
#endif

namespace SIL.Machine.Morphology.HermitCrab
{
    public class Morpher : IMorphologicalAnalyzer, IMorphologicalGenerator
    {
        private readonly Language _lang;
        private readonly IRule<Word, ShapeNode> _analysisRule;
        private readonly IRule<Word, ShapeNode> _synthesisRule;
        private readonly Dictionary<Stratum, RootAllomorphTrie> _allomorphTries;
        private readonly ITraceManager _traceManager;
        private readonly ReadOnlyObservableCollection<Morpheme> _morphemes;
        private readonly IList<RootAllomorph> _lexicalPatterns = new List<RootAllomorph>();

        public Morpher(ITraceManager traceManager, Language lang)
        {
            _lang = lang;
            _traceManager = traceManager;
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
            _analysisRule = lang.CompileAnalysisRule(this);
            _synthesisRule = lang.CompileSynthesisRule(this);
            MaxStemCount = 2;
            MaxUnapplications = 0;
            LexEntrySelector = entry => true;
            RuleSelector = rule => true;

            _morphemes = new ReadOnlyObservableCollection<Morpheme>(morphemes);
        }

        public ITraceManager TraceManager
        {
            get { return _traceManager; }
        }

        public int DeletionReapplications { get; set; }

        public int MaxStemCount { get; set; }

        /// <summary>
        /// MaxUnapplications limits the number of unapplications to make it possible
        /// to make it possible to debug words that take 30 minutes to parse
        /// because there are too many unapplications.
        /// </summary>
        public int MaxUnapplications { get; set; }

        public Func<LexEntry, bool> LexEntrySelector { get; set; }
        public Func<IHCRule, bool> RuleSelector { get; set; }

        public Language Language
        {
            get { return _lang; }
        }

        public IList<RootAllomorph> LexicalPatterns
        {
            get { return _lexicalPatterns; }
        }

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
            input.Freeze();
            if (_traceManager.IsTracing)
                _traceManager.AnalyzeWord(_lang, input);
            trace = input.CurrentTrace;

            // Unapply rules
            var analyses = new ConcurrentQueue<Word>(_analysisRule.Apply(input));

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
            var origAnalyses = guessRoot ? analyses.ToList() : null;
            var syntheses = Synthesize(word, analyses);
            if (guessRoot && syntheses.Count() == 0)
            {
                // Guess roots when there are no results.
                List<Word> matches = new List<Word>();
                foreach (Word analysisWord in origAnalyses)
                {
                    var lexicalGuesses = LexicalGuess(analysisWord).Distinct();
                    foreach (Word synthesisWord in lexicalGuesses)
                    {
                        foreach (Word validWord in _synthesisRule.Apply(synthesisWord).Where(IsWordValid))
                        {
                            if (IsMatch(word, validWord))
                                matches.Add(validWord);
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

#if SINGLE_THREADED
        private IEnumerable<Word> Synthesize(string word, IEnumerable<Word> analyses)
        {
            var matches = new HashSet<Word>(FreezableEqualityComparer<Word>.Default);
            foreach (Word analysisWord in analyses)
            {
                foreach (Word synthesisWord in LexicalLookup(analysisWord))
                {
                    foreach (Word validWord in _synthesisRule.Apply(synthesisWord).Where(IsWordValid))
                    {
                        if (IsMatch(word, validWord))
                            matches.Add(validWord);
                    }
                }
            }
            return matches;
        }
#else
        private IEnumerable<Word> Synthesize(string word, ConcurrentQueue<Word> analyses)
        {
            var matches = new ConcurrentBag<Word>();
            Exception exception = null;
            Parallel.ForEach(
                Partitioner.Create(0, analyses.Count),
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                (range, state) =>
                {
                    try
                    {
                        for (int i = 0; i < range.Item2 - range.Item1; i++)
                        {
                            analyses.TryDequeue(out Word analysisWord);
                            foreach (Word synthesisWord in LexicalLookup(analysisWord))
                            {
                                foreach (Word validWord in _synthesisRule.Apply(synthesisWord).Where(IsWordValid))
                                {
                                    if (IsMatch(word, validWord))
                                        matches.Add(validWord);
                                }
                            }
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
            return matches.Distinct(FreezableEqualityComparer<Word>.Default);
        }
#endif

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

        private IEnumerable<Word> LexicalGuess(Word input)
        {
            if (_traceManager.IsTracing)
                _traceManager.LexicalLookup(input.Stratum, input);
            CharacterDefinitionTable table = input.Stratum.CharacterDefinitionTable;
            IEnumerable<ShapeNode> shapeNodes = input.Shape.GetNodes(input.Range);
            foreach (RootAllomorph lexicalPattern in _lexicalPatterns)
            {
                IEnumerable<ShapeNode> shapePattern = lexicalPattern.Segments.Shape.GetNodes(
                    lexicalPattern.Segments.Shape.Range
                );
                foreach (List<ShapeNode> match in MatchNodesWithPattern(shapeNodes.ToList(), shapePattern.ToList()))
                {
                    // Create a root allomorph for the guess.
                    string shapeString = match.ToString(table, false);
                    var root = new RootAllomorph(new Segments(table, shapeString)) { Guessed = true };
                    // Point the root allomorph to the lexical pattern in FieldWorks.
                    if (lexicalPattern.Properties.ContainsKey("ID"))
                        root.Properties["ID"] = lexicalPattern.Properties["ID"];
                    if (lexicalPattern.Morpheme != null && lexicalPattern.Morpheme.Properties.ContainsKey("ID"))
                        root.Morpheme.Properties["ID"] = lexicalPattern.Morpheme.Properties["ID"];
                    // Create a lexical entry to hold the root allomorph.
                    // (The root allmorph will point to the lexical entry.)
                    var lexEntry = new LexEntry
                    {
                        Id = shapeString,
                        SyntacticFeatureStruct = input.SyntacticFeatureStruct,
                        Gloss = shapeString,
                        Stratum = input.Stratum,
                        IsPartial = input.SyntacticFeatureStruct.IsEmpty
                    };
                    lexEntry.Allomorphs.Add(root);
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
            if (pattern.Count() == p)
            {
                if (nodes.Count() == n)
                    // We match because we are at the end of both the pattern and the nodes.
                    results.Add(prefix);
                return results;
            }
            if (pattern[p].Annotation.Optional && !obligatory)
                // Try skipping this item in the pattern.
                results.AddRange(MatchNodesWithPattern(nodes, pattern, n, p + 1, false, prefix));
            if (nodes.Count() == n)
            {
                // We fail to match because we are at the end of the nodes but not the pattern.
                return results;
            }
            ShapeNode newNode = UnifyShapeNodes(nodes[n], pattern[p]);
            if (newNode == null)
                // We fail because the pattern didn't match the node here.
                return results;
            // Make a copy of prefix to avoid crosstalk and add newNode.
            prefix = new List<ShapeNode>(prefix)
            {
                newNode
            };
            if (pattern[p].Annotation.Iterative)
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
