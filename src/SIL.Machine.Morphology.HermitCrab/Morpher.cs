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
			return ParseWord(word, out _);
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

			return Synthesize(word, analyses);
		}

		/// <summary>
		/// Generates surface forms from the specified word synthesis information.
		/// </summary>
		public IEnumerable<string> GenerateWords(LexEntry rootEntry, IEnumerable<Morpheme> otherMorphemes,
			FeatureStruct realizationalFS)
		{
			return GenerateWords(rootEntry, otherMorphemes, realizationalFS, out _);
		}

		public IEnumerable<string> GenerateWords(LexEntry rootEntry, IEnumerable<Morpheme> otherMorphemes,
			FeatureStruct realizationalFS, out object trace)
		{
			Stack<Tuple<IMorphologicalRule, RootAllomorph>>[] rulePermutations = PermuteRules(otherMorphemes.ToArray())
				.ToArray();

			object rootTrace = _traceManager.IsTracing ? _traceManager.GenerateWords(_lang) : null;
			trace = rootTrace;

			var words = new ConcurrentBag<string>();

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
			  });

			if (exception != null)
				throw exception;

			return words.Distinct();
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
			Parallel.ForEach(Partitioner.Create(0, analyses.Count),
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
				});
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
