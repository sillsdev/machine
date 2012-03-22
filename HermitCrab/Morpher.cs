using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	public class Morpher
	{
		private class MorphsEqualityComparer : IEqualityComparer<IEnumerable<Allomorph>>
		{
			private static readonly IEqualityComparer<Allomorph> MorphemeEqualityComparer = ProjectionEqualityComparer<Allomorph>.Create(allo => allo.Morpheme);

			public bool Equals(IEnumerable<Allomorph> x, IEnumerable<Allomorph> y)
			{
				return x.SequenceEqual(y, MorphemeEqualityComparer);
			}

			public int GetHashCode(IEnumerable<Allomorph> obj)
			{
				return obj.Aggregate(23, (code, allo) => code * 31 + allo.Morpheme.GetHashCode());
			}
		}

		private class MorphsComparer : IComparer<IEnumerable<Allomorph>>
		{
			public int Compare(IEnumerable<Allomorph> x, IEnumerable<Allomorph> y)
			{
				foreach (Tuple<Allomorph, Allomorph> tuple in x.Zip(y))
				{
					int res = string.CompareOrdinal(tuple.Item1.Morpheme.ID, tuple.Item2.Morpheme.ID);
					if (res != 0)
						return res;

					res = tuple.Item1.Index.CompareTo(tuple.Item2.Index);
					if (res != 0)
						return res;
				}
				return 0;
			}
		}

		private static readonly MorphsEqualityComparer WordMorphsEqualityComparer = new MorphsEqualityComparer();
		private static readonly MorphsComparer WordMorphsComparer = new MorphsComparer();

		private readonly Language _lang;
		private readonly IRule<Word, ShapeNode> _analysisRule;
		private readonly IRule<Word, ShapeNode> _synthesisRule;
		private readonly Dictionary<Stratum, Tuple<Matcher<Shape, ShapeNode>, IDBearerSet<RootAllomorph>>> _allomorphSearchers;

		private bool _traceAll;
		private bool _traceLexLookup;
		private bool _traceSuccess;
		private bool _traceBlocking;
		private readonly HashSet<IHCRule> _traceRules; 

		public Morpher(SpanFactory<ShapeNode> spanFactory, Language lang)
		{
			_lang = lang;
			_allomorphSearchers = new Dictionary<Stratum, Tuple<Matcher<Shape, ShapeNode>, IDBearerSet<RootAllomorph>>>();
			foreach (Stratum stratum in _lang.Strata)
			{
				var allomorphs = new IDBearerSet<RootAllomorph>(stratum.Entries.SelectMany(entry => entry.Allomorphs));
				var matcher = new Matcher<Shape, ShapeNode>(spanFactory, new Pattern<Shape, ShapeNode>(allomorphs.Select(CreateSubpattern)),
					new MatcherSettings<ShapeNode>
						{
							Filter = ann => ann.Type() == HCFeatureSystem.Segment,
							FastCompile = true,
							AnchoredToStart = true,
							AnchoredToEnd = true
						});
				_allomorphSearchers[stratum] = Tuple.Create(matcher, allomorphs);
			}
			_analysisRule = lang.CompileAnalysisRule(spanFactory, this);
			_synthesisRule = lang.CompileSynthesisRule(spanFactory, this);
			_traceRules = new HashSet<IHCRule>();
		}

		private Pattern<Shape, ShapeNode> CreateSubpattern(RootAllomorph allomorph)
		{
			var subpattern = new Pattern<Shape, ShapeNode>(allomorph.ID);
			foreach (ShapeNode node in allomorph.Shape.Where(node => node.Annotation.Type() == HCFeatureSystem.Segment))
				subpattern.Children.Add(new Constraint<Shape, ShapeNode>(node.Annotation.FeatureStruct.DeepClone()));
			return subpattern;
		}

		public bool IsTracing
		{
			get { return _traceAll || _traceBlocking || _traceLexLookup || _traceSuccess || _traceRules.Count > 0; }
		}

		public bool TraceAll
		{
			get { return _traceAll; }
			set
			{
				_traceAll = value;
				_traceBlocking = value;
				_traceLexLookup = value;
				_traceSuccess = value;
				_traceRules.Clear();
			}
		}

		public bool TraceLexicalLookup
		{
			get { return _traceLexLookup; }
			set
			{
				if (!_traceAll)
					_traceLexLookup = value;
			}
		}

		public bool TraceSuccess
		{
			get { return _traceSuccess; }
			set
			{
				if (!_traceAll)
					_traceSuccess = value;
			}
		}

		public bool TraceBlocking
		{
			get { return _traceBlocking; }
			set
			{
				if (!_traceAll)
					_traceBlocking = value;
			}
		}

		public void SetTraceRule(IHCRule rule, bool trace)
		{
			if (!_traceAll)
			{
				if (trace)
					_traceRules.Add(rule);
				else
					_traceRules.Remove(rule);
			}
		}

		public bool GetTraceRule(IHCRule rule)
		{
			if (_traceAll)
				return true;

			return _traceRules.Contains(rule);
		}

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
				throw new ArgumentException("The word '{0}' cannot be converted to a shape.", "word");

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
			foreach (Word match in validWords.GroupBy(validWord => validWord.AllomorphsInMorphOrder, WordMorphsEqualityComparer)
				.Select(group => group.MaxBy(validWord => validWord.AllomorphsInMorphOrder, WordMorphsComparer)))
			{
				if (_traceSuccess)
					match.CurrentTrace.Children.Add(new Trace(TraceType.ReportSuccess, _lang) { Output = match });
				matchList.Add(match);
			}

			trace = null;
			return matchList;
		}

		internal IEnumerable<RootAllomorph> SearchRootAllomorphs(Stratum stratum, Shape shape)
		{
			Tuple<Matcher<Shape, ShapeNode>, IDBearerSet<RootAllomorph>> alloSearcher = _allomorphSearchers[stratum];
			return alloSearcher.Item1.AllMatches(shape).Select(match => alloSearcher.Item2[match.PatternPath.Single()]).Distinct();
		}

		private IEnumerable<Word> LexicalLookup(Word input)
		{
			Trace lookupTrace = null;
			if (_traceLexLookup)
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
