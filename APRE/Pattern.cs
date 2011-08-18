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
		public static PatternBuilder<TOffset> Build(SpanFactory<TOffset> spanFactory)
		{
			return new PatternBuilder<TOffset>(spanFactory);
		}

		protected const string EntireGroupName = "*entire*";

		private readonly SpanFactory<TOffset> _spanFactory;
		private readonly Func<Annotation<TOffset>, bool> _filter;
		private FiniteStateAutomaton<TOffset> _fsa;
		private readonly Direction _dir;

        public Pattern(SpanFactory<TOffset> spanFactory)
			: this(spanFactory, Direction.LeftToRight)
        {
        }

		public Pattern(SpanFactory<TOffset> spanFactory, Direction dir)
			: this(spanFactory, dir, ann => true)
		{
		}

		public Pattern(SpanFactory<TOffset> spanFactory, Direction dir, Func<Annotation<TOffset>, bool> filter)
		{
			_spanFactory = spanFactory;
			_dir = dir;
			_filter = filter;
		}

		public Pattern(SpanFactory<TOffset> spanFactory, params PatternNode<TOffset>[] nodes)
			: this(spanFactory, Direction.LeftToRight, nodes)
		{
		}

		public Pattern(SpanFactory<TOffset> spanFactory, IEnumerable<PatternNode<TOffset>> nodes)
			: this(spanFactory, Direction.LeftToRight, nodes)
		{
		}

		public Pattern(SpanFactory<TOffset> spanFactory, Direction dir, params PatternNode<TOffset>[] nodes)
			: this(spanFactory, dir, ann => true, nodes)
		{
		}

		public Pattern(SpanFactory<TOffset> spanFactory, Direction dir, IEnumerable<PatternNode<TOffset>> nodes)
			: this(spanFactory, dir, ann => true, nodes)
		{
		}

		public Pattern(SpanFactory<TOffset> spanFactory, Direction dir, Func<Annotation<TOffset>, bool> filter,
			params PatternNode<TOffset>[] nodes)
			: this(spanFactory, dir, filter, (IEnumerable<PatternNode<TOffset>>) nodes)
		{
		}

		public Pattern(SpanFactory<TOffset> spanFactory, Direction dir, Func<Annotation<TOffset>, bool> filter,
			IEnumerable<PatternNode<TOffset>> nodes)
			: this(spanFactory, dir, filter)
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
        	_dir = pattern._dir;
        	_filter = pattern._filter;
			AddMany(pattern.Select(node => node.Clone()));
        }

		public SpanFactory<TOffset> SpanFactory
		{
			get { return _spanFactory; }
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

		public FiniteStateAutomaton<TOffset> Fsa
		{
			get { return _fsa; }
		}

		public Direction Direction
		{
			get { return _dir; }
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

		public void Compile()
		{
			_fsa = GenerateFsa(_dir);
		}

		protected virtual FiniteStateAutomaton<TOffset> GenerateFsa(Direction dir)
		{
			var fsa = new FiniteStateAutomaton<TOffset>(dir, _filter);
			State<TOffset> startState = fsa.CreateGroupTag(fsa.StartState, EntireGroupName, true);
			State<TOffset> endState = GetFirst(dir).GenerateNfa(fsa, startState);
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

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList)
		{
			IEnumerable<PatternMatch<TOffset>> matches;
			return IsMatch(annList, false, out matches);
		}

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList, out PatternMatch<TOffset> match)
		{
			IEnumerable<PatternMatch<TOffset>> matches;
			if (IsMatch(annList, false, out matches))
			{
				match = matches.First();
				return true;
			}
			match = null;
			return false;
		}

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList, out IEnumerable<PatternMatch<TOffset>> matches)
		{
			return IsMatch(annList, true, out matches);
		}

		private bool IsMatch(IBidirList<Annotation<TOffset>> annList, bool allMatches, out IEnumerable<PatternMatch<TOffset>> matches)
		{
			if (_fsa == null)
				_fsa = GenerateFsa(_dir);

			List<PatternMatch<TOffset>> matchesList = null;
			Annotation<TOffset> first = annList.GetFirst(_dir, _filter);
			while (first != null)
			{
				IEnumerable<FsaMatch<TOffset>> fsaMatches;
				if (_fsa.IsMatch(annList.GetView(first, _dir), allMatches, out fsaMatches))
				{
					if (matchesList == null)
						matchesList = new List<PatternMatch<TOffset>>();

					foreach (FsaMatch<TOffset> match in fsaMatches)
					{
						var groups = new Dictionary<string, Span<TOffset>>();
						TOffset matchStart, matchEnd;
						_fsa.GetOffsets(EntireGroupName, match.Registers, out matchStart, out matchEnd);
						var matchSpan = _spanFactory.Create(matchStart, matchEnd);

						foreach (string groupName in _fsa.GroupNames)
						{
							if (groupName == EntireGroupName)
								continue;

							TOffset start, end;
							if (_fsa.GetOffsets(groupName, match.Registers, out start, out end))
							{
								if (_spanFactory.IsValidSpan(start, end))
								{
									Span<TOffset> span = _spanFactory.Create(start, end);
									if (matchSpan.Contains(span))
										groups[groupName] = span;
								}
							}
						}

						matchesList.Add(new PatternMatch<TOffset>(matchSpan, groups, match.VariableBindings));
					}

					if (!allMatches)
					{
						matches = matchesList;
						return true;
					}
				}

				first = first.GetNext(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
			}

			matches = matchesList;
			return matchesList != null;
		}

		public Pattern<TOffset> Reverse()
		{
			return new Pattern<TOffset>(_spanFactory, _dir == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight,
				_filter, this.Select(node => node.Clone()));
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
