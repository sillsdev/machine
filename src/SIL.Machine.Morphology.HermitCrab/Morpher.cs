using System;
#if !SINGLE_THREADED
using System.Collections.Concurrent;
using System.Threading.Tasks;
#endif
using System.Collections.Generic;
#if OUTPUT_ANALYSES
using System.IO;
#endif
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
	public class Morpher : IMorphologicalAnalyzer, IMorphologicalGenerator
	{
		private static readonly IEqualityComparer<IEnumerable<Allomorph>> MorphsEqualityComparer =
			SequenceEqualityComparer.Create(ProjectionEqualityComparer<Allomorph>.Create(allo => allo.Morpheme));

		private readonly Language _lang;
		private readonly IRule<Word, ShapeNode> _analysisRule;
		private readonly IRule<Word, ShapeNode> _synthesisRule;
		private readonly Dictionary<Stratum, RootAllomorphTrie> _allomorphTries;
		private readonly ITraceManager _traceManager;
		private readonly ReadOnlyObservableCollection<Morpheme> _morphemes;

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
					trie.Add(allomorph);
				_allomorphTries[stratum] = trie;

				morphemes.AddRange(stratum.Entries);
				morphemes.AddRange(stratum.MorphologicalRules.OfType<AffixProcessRule>());
				morphemes.AddRange(stratum.AffixTemplates.SelectMany(t => t.Slots).SelectMany(s => s.Rules).Distinct());
			}
			_analysisRule = lang.CompileAnalysisRule(this);
			_synthesisRule = lang.CompileSynthesisRule(this);
			MaxStemCount = 2;
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

		public Func<LexEntry, bool> LexEntrySelector { get; set; }
		public Func<IHCRule, bool> RuleSelector { get; set; }

		public Language Language
		{
			get { return _lang; }
		}

		/// <summary>
		/// Parses the specified surface form.
		/// </summary>
		public IEnumerable<Word> ParseWord(string word)
		{
			object trace;
			return ParseWord(word, out trace);
		}

		public IEnumerable<Word> ParseWord(string word, out object trace)
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

#if SINGLE_THREADED
			IEnumerable<Word> validWords = Synthesize(analyses);
#else
			IEnumerable<Word> validWords = ParallelSynthesize(analyses);
#endif

			var matchList = new List<Word>();
			foreach (Word w in CheckDisjunction(validWords))
			{
				if (_lang.SurfaceStratum.CharacterDefinitionTable.IsMatch(word, w.Shape))
				{
					if (_traceManager.IsTracing)
						_traceManager.Successful(_lang, w);
					matchList.Add(w);
				}
				else if (_traceManager.IsTracing)
				{
					_traceManager.Failed(_lang, w, FailureReason.SurfaceFormMismatch, null, word);
				}
			}
			return matchList;
		}

		/// <summary>
		/// Generates surface forms from the specified word synthesis information.
		/// </summary>
		public IEnumerable<string> GenerateWords(LexEntry rootEntry, IEnumerable<Morpheme> otherMorphemes,
			FeatureStruct realizationalFS)
		{
			object trace;
			return GenerateWords(rootEntry, otherMorphemes, realizationalFS, out trace);
		}

		public IEnumerable<string> GenerateWords(LexEntry rootEntry, IEnumerable<Morpheme> otherMorphemes,
			FeatureStruct realizationalFS, out object trace)
		{
			Stack<Tuple<IMorphologicalRule, RootAllomorph>>[] rulePermutations = PermuteRules(otherMorphemes.ToArray())
				.ToArray();

			object rootTrace = _traceManager.IsTracing ? _traceManager.GenerateWords(_lang) : null;
			trace = rootTrace;

			var validWordsStack = new ConcurrentStack<Word>();

			Exception exception = null;
			Parallel.ForEach(rootEntry.Allomorphs.SelectMany(a => rulePermutations,
				(a, p) => new { Allomorph = a, RulePermutation = p }), (synthesisInfo, state) =>
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

					  Word[] valid = _synthesisRule.Apply(synthesisWord).Where(IsWordValid).ToArray();
					  if (valid.Length > 0)
						  validWordsStack.PushRange(valid);
				  }
				  catch (Exception e)
				  {
					  state.Stop();
					  exception = e;
				  }
			  });

			if (exception != null)
				throw exception;

			var words = new List<string>();
			foreach (Word w in CheckDisjunction(validWordsStack.Distinct(FreezableEqualityComparer<Word>.Default)))
			{
				if (_traceManager.IsTracing)
					_traceManager.Successful(_lang, w);
				words.Add(w.Shape.ToString(_lang.SurfaceStratum.CharacterDefinitionTable, false));
			}
			return words;
		}

		private IEnumerable<Stack<Tuple<IMorphologicalRule, RootAllomorph>>> PermuteRules(Morpheme[] morphemes,
			int index = 0)
		{
			if (index == morphemes.Length)
			{
				yield return new Stack<Tuple<IMorphologicalRule, RootAllomorph>>();
			}
			else
			{
				var entry = morphemes[index] as LexEntry;
				if (entry != null)
				{
					foreach (RootAllomorph allo in entry.Allomorphs)
					{
						foreach (Stack<Tuple<IMorphologicalRule, RootAllomorph>> permutation in PermuteRules(morphemes,
							index + 1))
						{
							permutation.Push(Tuple.Create((IMorphologicalRule)null, allo));
							yield return permutation;
						}
					}
				}
				else
				{
					foreach (Stack<Tuple<IMorphologicalRule, RootAllomorph>> permutation in PermuteRules(morphemes,
						index + 1))
					{
						permutation.Push(Tuple.Create((IMorphologicalRule)morphemes[index], (RootAllomorph)null));
						yield return permutation;
					}
				}
			}
		}

		private IEnumerable<Word> CheckDisjunction(IEnumerable<Word> validWords)
		{
			foreach (IGrouping<IEnumerable<Allomorph>, Word> group in validWords
				.GroupBy(validWord => validWord.AllomorphsInMorphOrder, MorphsEqualityComparer))
			{
				// enforce the disjunctive property of allomorphs by ensuring that this word synthesis
				// has the highest order of precedence for its allomorphs
				Word[] words = group.ToArray();
				for (int i = 0; i < words.Length; i++)
				{
					bool disjunctive = false;
					for (int j = 0; j < words.Length; j++)
					{
						if (i == j)
							continue;

						// if the two parses differ by one allomorph and that allomorph does not free fluctuate
						// and has a lower precedence, than the parse fails
						Tuple<Allomorph, Allomorph>[] differentAllomorphs = words[i].AllomorphsInMorphOrder
							.Zip(words[j].AllomorphsInMorphOrder).Where(t => t.Item1 != t.Item2).ToArray();
						if (differentAllomorphs.Length == 1
							&& !differentAllomorphs[0].Item1.FreeFluctuatesWith(differentAllomorphs[0].Item2)
							&& differentAllomorphs[0].Item1.Index >= differentAllomorphs[0].Item2.Index)
						{
							disjunctive = true;
							if (_traceManager.IsTracing)
								_traceManager.Failed(_lang, words[i], FailureReason.DisjunctiveAllomorph, null, words[j]);
							break;
						}
					}

					if (!disjunctive)
						yield return words[i];
				}
			}
		}

#if SINGLE_THREADED
		private IEnumerable<Word> Synthesize(IEnumerable<Word> analyses)
		{
			var validWords = new HashSet<Word>(FreezableEqualityComparer<Word>.Default);
			foreach (Word analysisWord in analyses)
			{
				foreach (Word synthesisWord in LexicalLookup(analysisWord))
					validWords.UnionWith(_synthesisRule.Apply(synthesisWord).Where(IsWordValid));
			}
			return validWords;
		}
#else
		private IEnumerable<Word> ParallelSynthesize(ConcurrentQueue<Word> analyses)
		{
			var validWords = new ConcurrentDictionary<Word, byte>(FreezableEqualityComparer<Word>.Default);
			Exception exception = null;
			Parallel.For(0, analyses.Count, (_, state) =>
			{
				analyses.TryDequeue(out Word analysisWord);
				try
				{
					foreach (Word synthesisWord in LexicalLookup(analysisWord))
					{
						foreach (Word validWord in _synthesisRule.Apply(synthesisWord).Where(IsWordValid))
							validWords.TryAdd(validWord, 0);
					}
				}
				catch (Exception e)
				{
					state.Stop();
					exception = e;
				}
			});
			if (exception != null)
				throw exception;
			return validWords.Keys;
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
			foreach (LexEntry entry in SearchRootAllomorphs(input.Stratum, input.Shape).Select(allo => allo.Morpheme)
				.Cast<LexEntry>().Where(LexEntrySelector).Distinct())
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

		private bool IsWordValid(Word word)
		{
			if (!word.RealizationalFeatureStruct.IsUnifiable(word.SyntacticFeatureStruct) || !word.IsAllMorphologicalRulesApplied)
			{
				if (_traceManager.IsTracing)
					_traceManager.Failed(_lang, word, FailureReason.PartialParse, null, null);
				return false;
			}

			Feature feature = word.ObligatorySyntacticFeatures
				.FirstOrDefault(f => !ContainsFeature(word.SyntacticFeatureStruct, f,
					new HashSet<FeatureStruct>(new ReferenceEqualityComparer<FeatureStruct>())));
			if (feature != null)
			{
				if (_traceManager.IsTracing)
					_traceManager.Failed(_lang, word, FailureReason.ObligatorySyntacticFeatures, null, feature);
				return false;
			}

			return word.Allomorphs.All(allo => allo.IsWordValid(this, word));
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
			foreach (Stack<Morpheme> otherMorphemes in PermuteOtherMorphemes(morphemes,
				wordAnalysis.RootMorphemeIndex - 1, wordAnalysis.RootMorphemeIndex + 1))
			{
				results.UnionWith(GenerateWords(rootEntry, otherMorphemes, realizationalFS));
			}
			return results;
		}

		private IEnumerable<Stack<Morpheme>> PermuteOtherMorphemes(List<Morpheme> morphemes, int leftIndex,
			int rightIndex)
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
