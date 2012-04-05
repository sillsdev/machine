using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	public class Morpher
	{
		private static readonly IEqualityComparer<IEnumerable<Allomorph>> MorphsEqualityComparer = SequenceEqualityComparer.Create(ProjectionEqualityComparer<Allomorph>.Create(allo => allo.Morpheme));
		private static readonly IComparer<IEnumerable<Allomorph>> MorphsComparer = SequenceComparer.Create(ProjectionComparer<Allomorph>.Create(allo => allo.Index));

		private readonly Language _lang;
		private readonly IRule<Word, ShapeNode> _analysisRule;
		private readonly IRule<Word, ShapeNode> _synthesisRule;
		private readonly Dictionary<Stratum, Tuple<ShapeTrie, IDBearerSet<RootAllomorph>>> _allomorphTries;

		private readonly TraceRuleCollection _traceRules;
		private readonly IDBearerSet<IHCRule> _rules; 

		public Morpher(SpanFactory<ShapeNode> spanFactory, Language lang)
		{
			_lang = lang;
			_rules = new IDBearerSet<IHCRule>();
			_lang.Traverse(rule => _rules.Add(rule));
			_allomorphTries = new Dictionary<Stratum, Tuple<ShapeTrie, IDBearerSet<RootAllomorph>>>();
			foreach (Stratum stratum in _lang.Strata)
			{
				var allomorphs = new IDBearerSet<RootAllomorph>(stratum.Entries.SelectMany(entry => entry.Allomorphs));
				var trie = new ShapeTrie(ann => ann.Type() == HCFeatureSystem.Segment);
				foreach (RootAllomorph allomorph in allomorphs)
					trie.Add(allomorph.Shape, allomorph.ID);
				_allomorphTries[stratum] = Tuple.Create(trie, allomorphs);
			}
			_analysisRule = lang.CompileAnalysisRule(spanFactory, this);
			_synthesisRule = lang.CompileSynthesisRule(spanFactory, this);
			_traceRules = new TraceRuleCollection(_rules);
		}

		public bool IsTracing
		{
			get { return TraceBlocking || TraceLexicalLookup || TraceSuccess || _traceRules.Count > 0; }
		}

		public bool TraceAll
		{
			get { return TraceBlocking && TraceLexicalLookup && TraceSuccess && _traceRules.IsTracingAllRules; }
			set
			{
				if (value)
					_traceRules.AddAllRules();
				else
					_traceRules.Clear();
				TraceBlocking = value;
				TraceLexicalLookup = value;
				TraceSuccess = value;
			}
		}

		public bool TraceLexicalLookup { get; set; }
		public bool TraceSuccess { get; set; }

		public bool TraceBlocking { get; set; }

		public TraceRuleCollection TraceRules
		{
			get { return _traceRules; }
		}

		public int DeletionReapplications { get; set; }

		/// <summary>
		/// Morphs the specified word.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <returns>All valid word synthesis records.</returns>
		public IEnumerable<Word> ParseWord(string word)
		{
			Trace trace;
			return ParseWord(word, out trace);
		}

		public IEnumerable<Word> ParseWord(string word, out Trace trace)
		{
			// convert the word to its phonetic shape
			Shape shape;
			if (!_lang.SurfaceStratum.SymbolTable.ToShape(word, out shape))
				throw new ArgumentException(string.Format("The word '{0}' cannot be converted to a shape.", word), "word");

			var input = new Word(_lang.SurfaceStratum, shape);
			trace = new Trace(TraceType.WordAnalysis, _lang) { Input = input.DeepClone() };
			input.CurrentTrace = trace;

			// Unapply rules
			var validWords = new HashSet<Word>();
			foreach (Word analysisWord in _analysisRule.Apply(input))
			{
				foreach (Word synthesisWord in LexicalLookup(analysisWord))
					validWords.UnionWith(_synthesisRule.Apply(synthesisWord).Where(w => IsWordValid(w) && _lang.SurfaceStratum.SymbolTable.IsMatch(word, w.Shape)));
			}

			var matchList = new List<Word>();
			foreach (IGrouping<IEnumerable<Allomorph>, Word> group in validWords.GroupBy(validWord => validWord.AllomorphsInMorphOrder, MorphsEqualityComparer))
			{
				// enforce the disjunctive property of allomorphs by ensuring that this word synthesis
				// has the highest order of precedence for its allomorphs while also allowing for free fluctuation
				Word prevMatch = null;
				foreach (Word match in group.OrderBy(w => w.AllomorphsInMorphOrder, MorphsComparer))
				{
					if (prevMatch != null && match.AllomorphsInMorphOrder.Zip(prevMatch.AllomorphsInMorphOrder).All(tuple => tuple.Item1 == tuple.Item2 || !tuple.Item1.ConstraintsEqual(tuple.Item2)))
						break;

					if (TraceSuccess)
						match.CurrentTrace.Children.Add(new Trace(TraceType.ReportSuccess, _lang) {Output = match});
					matchList.Add(match);
					prevMatch = match;
				}
			}

			return matchList;
		}

		internal IEnumerable<RootAllomorph> SearchRootAllomorphs(Stratum stratum, Shape shape)
		{
			Tuple<ShapeTrie, IDBearerSet<RootAllomorph>> alloSearcher = _allomorphTries[stratum];
			return alloSearcher.Item1.Search(shape).Select(id => alloSearcher.Item2[id]).Distinct();
		}

		private IEnumerable<Word> LexicalLookup(Word input)
		{
			Trace lookupTrace = null;
			if (TraceLexicalLookup)
			{
				lookupTrace = new Trace(TraceType.LexicalLookup, input.Stratum) { Input = input.DeepClone() };
				input.CurrentTrace.Children.Add(lookupTrace);
			}
			foreach (LexEntry entry in SearchRootAllomorphs(input.Stratum, input.Shape).Select(allo => allo.Morpheme).Distinct())
			{
				foreach (RootAllomorph allomorph in entry.Allomorphs)
				{
					Word newWord = input.DeepClone();
					newWord.RootAllomorph = allomorph;
					if (lookupTrace != null)
					{
						var wsTrace = new Trace(TraceType.WordSynthesis, _lang) { Input = newWord.DeepClone() };
						lookupTrace.Children.Add(wsTrace);
						newWord.CurrentTrace = wsTrace;
					}
					yield return newWord;
				}
			}
		}

		private bool IsWordValid(Word word)
		{
			if (!word.ObligatorySyntacticFeatures.All(feature => ContainsFeature(word.SyntacticFeatureStruct, feature, new HashSet<FeatureStruct>(new ReferenceEqualityComparer<FeatureStruct>()))))
				return false;

			foreach (Allomorph allo in word.Allomorphs)
			{
				if (!allo.RequiredAllomorphCoOccurrences.All(c => c.CoOccurs(word)))
					return false;

				if (allo.ExcludedAllomorphCoOccurrences.Any(c => c.CoOccurs(word)))
					return false;

				if (!allo.RequiredEnvironments.All(env => env.IsMatch(word)))
					return false;

				if (allo.ExcludedEnvironments.Any(env => env.IsMatch(word)))
					return false;

				if (!allo.Morpheme.RequiredMorphemeCoOccurrences.All(c => c.CoOccurs(word)))
					return false;

				if (allo.Morpheme.ExcludedMorphemeCoOccurrences.Any(c => c.CoOccurs(word)))
					return false;
			}

			return true;
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
