using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SIL.APRE.Fsa;
using SIL.APRE.Matching.Fluent;

namespace SIL.APRE.Matching
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

	public class Pattern<TOffset> : Expression<TOffset>
	{
		public new static IPatternSyntax<TOffset> With(SpanFactory<TOffset> spanFactory)
		{
			return new PatternBuilder<TOffset>(spanFactory);
		}

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
			: this(spanFactory, dir, filter, (input, match) => true)
		{
		}

		public Pattern(SpanFactory<TOffset> spanFactory, Direction dir, Func<Annotation<TOffset>, bool> filter,
			Func<IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, bool> acceptable)
			: base("*entire*", acceptable)
		{
			_spanFactory = spanFactory;
			_dir = dir;
			_filter = filter;
		}

		public Pattern(SpanFactory<TOffset> spanFactory, params PatternNode<TOffset>[] nodes)
			: this(spanFactory, (IEnumerable<PatternNode<TOffset>>) nodes)
		{
		}

		public Pattern(SpanFactory<TOffset> spanFactory, IEnumerable<PatternNode<TOffset>> nodes)
			: this(spanFactory, Direction.LeftToRight, nodes)
		{
		}

		public Pattern(SpanFactory<TOffset> spanFactory, Direction dir, params PatternNode<TOffset>[] nodes)
			: this(spanFactory, dir, (IEnumerable<PatternNode<TOffset>>) nodes)
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
			: this(spanFactory, dir, filter, (input, match) => true, nodes)
		{
		}

		public Pattern(SpanFactory<TOffset> spanFactory, Direction dir, Func<Annotation<TOffset>, bool> filter,
			Func<IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, bool> acceptable, params PatternNode<TOffset>[] nodes)
			: this(spanFactory, dir, filter, acceptable, (IEnumerable<PatternNode<TOffset>>) nodes)
		{
		}

		public Pattern(SpanFactory<TOffset> spanFactory, Direction dir, Func<Annotation<TOffset>, bool> filter,
			Func<IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, bool> acceptable, IEnumerable<PatternNode<TOffset>> nodes)
			: base("*entire*", acceptable, nodes)
		{
			_spanFactory = spanFactory;
			_dir = dir;
			_filter = filter;
		}

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="pattern">The phonetic pattern.</param>
        public Pattern(Pattern<TOffset> pattern)
			: base(pattern)
        {
			_spanFactory = pattern._spanFactory;
			_dir = pattern._dir;
			_filter = pattern._filter;
        }

		public SpanFactory<TOffset> SpanFactory
		{
			get { return _spanFactory; }
		}

		public Direction Direction
		{
			get { return _dir; }
		}

		public Func<Annotation<TOffset>, bool> Filter
		{
			get { return _filter; }
		}

		public void Compile()
		{
			_fsa = GenerateFsa(_dir);
		}

		protected FiniteStateAutomaton<TOffset> GenerateFsa(Direction dir)
		{
			var fsa = new FiniteStateAutomaton<TOffset>(dir, _filter);
			State<TOffset> startState = fsa.StartState.AddArc(fsa.CreateTag(fsa.CreateState(), Name, true));
			GenerateNfa(fsa, startState);

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
						matchesList.Add(CreatePatternMatch(match));

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
				_filter, Acceptable, Children.Clone());
		}

		internal PatternMatch<TOffset> CreatePatternMatch(FsaMatch<TOffset> match)
		{
			var groups = new Dictionary<string, Span<TOffset>>();
			TOffset matchStart, matchEnd;
			_fsa.GetOffsets(Name, match.Registers, out matchStart, out matchEnd);
			var matchSpan = _spanFactory.Create(matchStart, matchEnd);

			foreach (string groupName in _fsa.GroupNames)
			{
				if (groupName == Name)
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

			return new PatternMatch<TOffset>(matchSpan, groups, match.ID.Split('*'), match.VariableBindings);
		}

        public override PatternNode<TOffset> Clone()
        {
            return new Pattern<TOffset>(this);
        }

        public override string ToString()
        {
        	return Children.ToString();
        }
	}
}
