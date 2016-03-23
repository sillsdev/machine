using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.HermitCrab
{
	public class Morpher
	{
		private static readonly IEqualityComparer<IEnumerable<Allomorph>> MorphsEqualityComparer = SequenceEqualityComparer.Create(ProjectionEqualityComparer<Allomorph>.Create(allo => allo.Morpheme));
		private static readonly IComparer<IEnumerable<Allomorph>> MorphsComparer = SequenceComparer.Create(ProjectionComparer<Allomorph>.Create(allo => allo.Index));

		private readonly Language _lang;
		private readonly IRule<Word, ShapeNode> _analysisRule;
		private readonly IRule<Word, ShapeNode> _synthesisRule;
		private readonly Dictionary<Stratum, RootAllomorphTrie> _allomorphTries;
		private readonly ITraceManager _traceManager;

		public Morpher(SpanFactory<ShapeNode> spanFactory, ITraceManager traceManager, Language lang)
		{
			_lang = lang;
			_traceManager = traceManager;
			_allomorphTries = new Dictionary<Stratum, RootAllomorphTrie>();
			foreach (Stratum stratum in _lang.Strata)
			{
				var allomorphs = new HashSet<RootAllomorph>(stratum.Entries.SelectMany(entry => entry.Allomorphs));
				var trie = new RootAllomorphTrie(ann => ann.Type() == HCFeatureSystem.Segment);
				foreach (RootAllomorph allomorph in allomorphs)
					trie.Add(allomorph);
				_allomorphTries[stratum] = trie;
			}
			_analysisRule = lang.CompileAnalysisRule(spanFactory, this);
			_synthesisRule = lang.CompileSynthesisRule(spanFactory, this);
			MaxStemCount = 2;
			LexEntrySelector = entry => true;
			RuleSelector = rule => true;
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
			Shape shape = _lang.SurfaceStratum.SymbolTable.Segment(word);

			var input = new Word(_lang.SurfaceStratum, shape);
			input.Freeze();
			if (_traceManager.IsTracing)
				_traceManager.AnalyzeWord(_lang, input);
			trace = input.CurrentTrace;

			// Unapply rules
			var validWordsStack = new ConcurrentStack<Word>();
			IEnumerable<Word> analyses = _analysisRule.Apply(input);

			Exception exception = null;
			Parallel.ForEach(analyses, (analysisWord, state) =>
			{
				try
				{
					foreach (Word synthesisWord in LexicalLookup(analysisWord))
					{
						Word[] valid = _synthesisRule.Apply(synthesisWord).Where(IsWordValid).ToArray();
						if (valid.Length > 0)
							validWordsStack.PushRange(valid);
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

			var matchList = new List<Word>();
			foreach (Word w in CheckDisjunction(validWordsStack.Distinct(FreezableEqualityComparer<Word>.Default)))
			{
				if (_lang.SurfaceStratum.SymbolTable.IsMatch(word, w.Shape))
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
		public IEnumerable<string> GenerateWords(LexEntry rootEntry, IEnumerable<Morpheme> morphemes, FeatureStruct realizationalFS)
		{
			object trace;
			return GenerateWords(rootEntry, morphemes, realizationalFS, out trace);
		}

		public IEnumerable<string> GenerateWords(LexEntry rootEntry, IEnumerable<Morpheme> morphemes, FeatureStruct realizationalFS, out object trace)
		{
			Stack<Tuple<IMorphologicalRule, RootAllomorph>>[] rulePermutations = PermuteRules(morphemes.ToArray()).ToArray();

			object rootTrace = _traceManager.IsTracing ? _traceManager.GenerateWords(_lang) : null;
			trace = rootTrace;

			var validWordsStack = new ConcurrentStack<Word>();

			Exception exception = null;
			Parallel.ForEach(rootEntry.Allomorphs.SelectMany(a => rulePermutations, (a, p) => new {Allomorph = a, RulePermutation = p}), (synthesisInfo, state) =>
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
				words.Add(w.Shape.ToString(_lang.SurfaceStratum.SymbolTable, false));
			}
			return words;
		}

		private IEnumerable<Stack<Tuple<IMorphologicalRule, RootAllomorph>>> PermuteRules(Morpheme[] morphemes, int index = 0)
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
						foreach (Stack<Tuple<IMorphologicalRule, RootAllomorph>> permutation in PermuteRules(morphemes, index + 1))
						{
							permutation.Push(Tuple.Create((IMorphologicalRule) null, allo));
							yield return permutation;
						}
					}
				}
				else
				{
					foreach (Stack<Tuple<IMorphologicalRule, RootAllomorph>> permutation in PermuteRules(morphemes, index + 1))
					{
						permutation.Push(Tuple.Create((IMorphologicalRule) morphemes[index], (RootAllomorph) null));
						yield return permutation;
					}
				}
			}
		}

		private IEnumerable<Word> CheckDisjunction(IEnumerable<Word> validWords)
		{
			foreach (IGrouping<IEnumerable<Allomorph>, Word> group in validWords.GroupBy(validWord => validWord.AllomorphsInMorphOrder, MorphsEqualityComparer))
			{
				// enforce the disjunctive property of allomorphs by ensuring that this word synthesis
				// has the highest order of precedence for its allomorphs
				Word[] words = group.OrderBy(w => w.AllomorphsInMorphOrder, MorphsComparer).ToArray();
				int i;
				for (i = 0; i < words.Length; i++)
				{
					// if there is no free fluctuation with any allomorphs in the previous parse,
					// then the rest of the parses are invalid, because of disjunction
					if (i > 0 && words[i].AllomorphsInMorphOrder.Zip(words[i - 1].AllomorphsInMorphOrder).All(tuple => !tuple.Item1.FreeFluctuatesWith(tuple.Item2)))
						break;

					yield return words[i];
				}

				if (_traceManager.IsTracing)
				{
					Word lastWord = words[i - 1];
					for (; i < words.Length; i++)
						_traceManager.Failed(_lang, words[i], FailureReason.DisjunctiveAllomorph, null, lastWord);
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
			foreach (LexEntry entry in SearchRootAllomorphs(input.Stratum, input.Shape).Select(allo => allo.Morpheme).Cast<LexEntry>().Where(LexEntrySelector).Distinct())
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

			Feature feature = word.ObligatorySyntacticFeatures.FirstOrDefault(f => !ContainsFeature(word.SyntacticFeatureStruct, f, new HashSet<FeatureStruct>(new ReferenceEqualityComparer<FeatureStruct>())));
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
	}
}
