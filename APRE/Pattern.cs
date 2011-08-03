using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SIL.APRE.Fsa;

namespace SIL.APRE
{
	/// <summary>
	/// This enumeration represents the morpher mode type.
	/// </summary>
	public enum ModeType
	{
		/// <summary>
		/// Analysis mode (unapplication of rules)
		/// </summary>
		Analysis,
		/// <summary>
		/// Synthesis mode (application of rules)
		/// </summary>
		Synthesis
	}

	public class Pattern<TOffset> : BidirList<PatternNode<TOffset>>, ICloneable
	{
		protected const string EntireGroupName = "*entire*";

		private readonly SpanFactory<TOffset> _spanFactory;
		private readonly bool _checkSynthesisClean;
		private readonly bool _checkAnalysisClean;
		private readonly HashSet<string> _synthesisTypes;
		private readonly HashSet<string> _analysisTypes;
		private FiniteStateAutomaton<TOffset> _rightToLeftFsa;
		private FiniteStateAutomaton<TOffset> _leftToRightFsa;

		private readonly IDictionary<string, IEnumerable<Feature>> _varFeatures;
		private readonly List<IEnumerable<Tuple<string, IEnumerable<Feature>, FeatureSymbol>>> _varValues; 

        /// <summary>
		/// Initializes a new instance of the <see cref="Pattern&lt;TOffset&gt;"/> class.
        /// </summary>
        public Pattern(SpanFactory<TOffset> spanFactory)
			: this(spanFactory, (IEnumerable<string>) null)
        {
        }

		public Pattern(SpanFactory<TOffset> spanFactory, IEnumerable<string> types)
			: this(spanFactory, types, types)
		{
		}

		public Pattern(SpanFactory<TOffset> spanFactory, IEnumerable<string> synthesisTypes, IEnumerable<string> analysisTypes)
			: this(spanFactory, synthesisTypes, analysisTypes, false, false, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Pattern&lt;TOffset&gt;"/> class.
		/// </summary>
		/// <param name="spanFactory"></param>
		/// <param name="synthesisTypes"></param>
		/// <param name="analysisTypes"></param>
		/// <param name="checkSynthesisClean"></param>
		/// <param name="checkAnalysisClean"></param>
		/// <param name="varFeatures"></param>
		public Pattern(SpanFactory<TOffset> spanFactory, IEnumerable<string> synthesisTypes, IEnumerable<string> analysisTypes,
			bool checkSynthesisClean, bool checkAnalysisClean, IDictionary<string, IEnumerable<Feature>> varFeatures)
		{
			_spanFactory = spanFactory;
			if (synthesisTypes != null)
				_synthesisTypes = new HashSet<string>(synthesisTypes);
			if (analysisTypes != null)
				_analysisTypes = new HashSet<string>(analysisTypes);
			_checkSynthesisClean = checkSynthesisClean;
			_checkAnalysisClean = checkAnalysisClean;
			_varFeatures = varFeatures;
			_varValues = new List<IEnumerable<Tuple<string, IEnumerable<Feature>, FeatureSymbol>>>();
		}

		public Pattern(SpanFactory<TOffset> spanFactory, params PatternNode<TOffset>[] nodes)
			: this(spanFactory, null, nodes)
		{
		}

		public Pattern(SpanFactory<TOffset> spanFactory, IEnumerable<string> types, params PatternNode<TOffset>[] nodes)
			: this(spanFactory, types, types, nodes)
		{
		}

		public Pattern(SpanFactory<TOffset> spanFactory, IEnumerable<string> synthesisTypes, IEnumerable<string> analysisTypes,
			params PatternNode<TOffset>[] nodes)
			: this(spanFactory, synthesisTypes, analysisTypes, false, false, null, nodes)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Pattern&lt;TOffset&gt;"/> class.
		/// </summary>
		/// <param name="spanFactory"></param>
		/// <param name="synthesisTypes"></param>
		/// <param name="analysisTypes"></param>
		/// <param name="checkSynthesisClean"></param>
		/// <param name="checkAnalysisClean"></param>
		/// <param name="varFeatures"></param>
		/// <param name="nodes"></param>
		public Pattern(SpanFactory<TOffset> spanFactory, IEnumerable<string> synthesisTypes, IEnumerable<string> analysisTypes,
			bool checkSynthesisClean, bool checkAnalysisClean, IDictionary<string, IEnumerable<Feature>> varFeatures,
			params PatternNode<TOffset>[] nodes)
			: this(spanFactory, synthesisTypes, analysisTypes, checkSynthesisClean, checkAnalysisClean, varFeatures)
		{
			AddMany(nodes);
		}

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="pattern">The phonetic pattern.</param>
        public Pattern(Pattern<TOffset> pattern)
        {
        	_spanFactory = pattern._spanFactory;
			_checkSynthesisClean = pattern._checkSynthesisClean;
			_checkAnalysisClean = pattern._checkAnalysisClean;
			if (_synthesisTypes != null)
				_synthesisTypes = new HashSet<string>(pattern._synthesisTypes);
			if (_analysisTypes != null)
				_analysisTypes = new HashSet<string>(pattern._analysisTypes);
			AddMany(pattern.Select(node => node.Clone()));
        }

		public SpanFactory<TOffset> SpanFactory
		{
			get { return _spanFactory; }
		}

		public IEnumerable<string> SynthesisTypes
		{
			get { return _synthesisTypes; }
		}

		public IEnumerable<string> AnalysisTypes
		{
			get { return _analysisTypes; }
		}

        /// <summary>
        /// Gets all of the features referenced in this pattern. 
        /// </summary>
        /// <value>The features.</value>
        public IEnumerable<Feature> Features
        {
            get
            {
                var features = new HashSet<Feature>();
                foreach (PatternNode<TOffset> node in this)
                    features.UnionWith(node.Features);
                return features;
            }
        }

        /// <summary>
        /// Determines whether the phonetic pattern references the specified feature.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <returns>
        /// 	<c>true</c> if the specified feature is referenced, otherwise <c>false</c>.
        /// </returns>
        public bool IsFeatureReferenced(Feature feature)
        {
        	return this.Any(node => node.IsFeatureReferenced(feature));
        }

		public bool CheckClean(ModeType mode)
		{
			return mode == ModeType.Synthesis ? _checkSynthesisClean : _checkAnalysisClean;
		}

		public FiniteStateAutomaton<TOffset> GetFsa(Direction dir)
		{
			return dir == Direction.LeftToRight ? _leftToRightFsa : _rightToLeftFsa;
		}

		public void Compile()
		{
			_leftToRightFsa = GenerateFsa(Direction.LeftToRight);
			_rightToLeftFsa = GenerateFsa(Direction.RightToLeft);
		}

		protected virtual FiniteStateAutomaton<TOffset> GenerateFsa(Direction dir)
		{
			var fsa = new FiniteStateAutomaton<TOffset>(dir, _synthesisTypes, _analysisTypes);
			State<TOffset> startState = fsa.CreateGroupTag(fsa.StartState, EntireGroupName, true);
			State<TOffset> endState;
			if (_varFeatures != null && _varFeatures.Count > 0)
			{
				endState = fsa.CreateState();
				GenerateVariableFsa(fsa, startState, endState, _varFeatures.Select(kvp => Tuple.Create(kvp.Key, kvp.Value)),
					Enumerable.Empty<Tuple<string, IEnumerable<Feature>, FeatureSymbol>>());
			}
			else
			{
				endState = GetFirst(dir).GenerateNfa(fsa, startState, -1, Enumerable.Empty<Tuple<string, IEnumerable<Feature>, FeatureSymbol>>());
			}
			endState = fsa.CreateGroupTag(endState, EntireGroupName, false);
			endState.AddArc(new Arc<TOffset>(fsa.CreateState(true)));

			var writer = new StreamWriter("c:\\nfa.dot");
			fsa.ToGraphViz(writer);
			writer.Close();

			fsa.Determinize();

			writer = new StreamWriter("c:\\dfa.dot");
			fsa.ToGraphViz(writer);
			writer.Close();
			return fsa;
		}

		private void GenerateVariableFsa(FiniteStateAutomaton<TOffset> fsa, State<TOffset> startState, State<TOffset> endState,
			IEnumerable<Tuple<string, IEnumerable<Feature>>> varFeatures, IEnumerable<Tuple<string, IEnumerable<Feature>, FeatureSymbol>> varValues)
		{
			if (!varFeatures.Any())
			{
				State<TOffset> state = GetFirst(fsa.Direction).GenerateNfa(fsa, startState, _varValues.Count, varValues);
				state.AddArc(new Arc<TOffset>(endState));
				_varValues.Add(varValues);
			}
			else
			{
				Tuple<string, IEnumerable<Feature>> varFeature = varFeatures.First();
				var feature = (SymbolicFeature) varFeature.Item2.Last();
				foreach (FeatureSymbol symbol in feature.PossibleSymbols)
					GenerateVariableFsa(fsa, startState, endState, varFeatures.Skip(1),
						varValues.Concat(Tuple.Create(varFeature.Item1, varFeature.Item2, symbol)));
			}
		}

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList, Direction dir, ModeType mode)
		{
			return IsMatch(annList, dir, mode, null);
		}

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList, Direction dir, ModeType mode, FeatureStructure varValues)
		{
			IEnumerable<PatternMatch<TOffset>> matches;
			return IsMatch(annList, dir, mode, varValues, false, out matches);
		}

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList, Direction dir, ModeType mode, out PatternMatch<TOffset> match)
		{
			return IsMatch(annList, dir, mode, null, out match);
		}

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList, Direction dir, ModeType mode, FeatureStructure varValues,
			out PatternMatch<TOffset> match)
		{
			IEnumerable<PatternMatch<TOffset>> matches;
			if (IsMatch(annList, dir, mode, varValues, false, out matches))
			{
				match = matches.First();
				return true;
			}

			match = null;
			return false;
		}

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList, Direction dir, ModeType mode,
			out IEnumerable<PatternMatch<TOffset>> matches)
		{
			return IsMatch(annList, dir, mode, null, out matches);
		}

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList, Direction dir, ModeType mode, FeatureStructure varValues,
			out IEnumerable<PatternMatch<TOffset>> matches)
		{
			return IsMatch(annList, dir, mode, varValues, true, out matches);
		}

		private bool IsMatch(IBidirList<Annotation<TOffset>> annList, Direction dir, ModeType mode, FeatureStructure varValues,
			bool allMatches, out IEnumerable<PatternMatch<TOffset>> matches)
		{
			FiniteStateAutomaton<TOffset> fsa;
			if (dir == Direction.LeftToRight)
			{
				if (_leftToRightFsa == null)
					_leftToRightFsa = GenerateFsa(Direction.LeftToRight);
				fsa = _leftToRightFsa;
			}
			else
			{
				if (_rightToLeftFsa == null)
					_rightToLeftFsa = GenerateFsa(Direction.RightToLeft);
				fsa = _rightToLeftFsa;
			}

			IEnumerable<FsaMatch<TOffset>> fsaMatches;
			if (fsa.IsMatch(annList, mode, allMatches, out fsaMatches))
			{
				var matchesList = new List<PatternMatch<TOffset>>();
				matches = matchesList;
				foreach (FsaMatch<TOffset> match in fsaMatches)
				{
					var groups = new Dictionary<string, Span<TOffset>>();
					TOffset matchStart, matchEnd;
					fsa.GetOffsets(EntireGroupName, match, out matchStart, out matchEnd);
					var matchSpan = _spanFactory.Create(matchStart, matchEnd);

					foreach (string groupName in fsa.GroupNames)
					{
						if (groupName == EntireGroupName)
							continue;

						TOffset start, end;
						if (fsa.GetOffsets(groupName, match, out start, out end))
						{
							if (_spanFactory.IsValidSpan(start, end))
							{
								Span<TOffset> span = _spanFactory.Create(start, end);
								if (matchSpan.Contains(span))
									groups[groupName] = span;
							}
						}
					}

					matchesList.Add(new PatternMatch<TOffset>(matchSpan, groups, null));
				}
				return true;
			}

			matches = null;
			return false;
		}

        object ICloneable.Clone()
        {
            return Clone();
        }

        public Pattern<TOffset> Clone()
        {
            return new Pattern<TOffset>(this);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (PatternNode<TOffset> node in this)
                sb.Append(node.ToString());
            return sb.ToString();
        }
	}
}
