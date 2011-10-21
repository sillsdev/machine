using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SIL.APRE.Fsa;
using SIL.APRE.Matching.Fluent;

namespace SIL.APRE.Matching
{
	public class Pattern<TData, TOffset> : Expression<TData, TOffset> where TData : IData<TOffset>
	{
		public static IPatternSyntax<TData, TOffset> New(SpanFactory<TOffset> spanFactory)
		{
			return new PatternBuilder<TData, TOffset>(spanFactory);
		}

		private const string EntireMatch = "*entire*";

		private readonly SpanFactory<TOffset> _spanFactory;
		private FiniteStateAutomaton<TData, TOffset> _fsa;

		public Pattern(SpanFactory<TOffset> spanFactory)
			: this(spanFactory, Enumerable.Empty<PatternNode<TData, TOffset>>())
        {
        }

		public Pattern(SpanFactory<TOffset> spanFactory, IEnumerable<PatternNode<TData, TOffset>> nodes)
			: base(nodes)
		{
			Filter = ann => true;
			Direction = Direction.LeftToRight;
			_spanFactory = spanFactory;
		}

		/// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="pattern">The phonetic pattern.</param>
        public Pattern(Pattern<TData, TOffset> pattern)
			: base(pattern)
        {
			_spanFactory = pattern._spanFactory;
			Direction = pattern.Direction;
			Filter = pattern.Filter;
        }

		public SpanFactory<TOffset> SpanFactory
		{
			get { return _spanFactory; }
		}

		public Direction Direction { get; set; }

		public Func<Annotation<TOffset>, bool> Filter { get; set; }

		public bool IsCompiled
		{
			get { return _fsa != null; }
		}

		public void Compile()
		{
			_fsa = new FiniteStateAutomaton<TData, TOffset>(Direction, Filter);
			int nextPriority = 0;
			GenerateExpressionNfa(_fsa.StartState, this, null, new Func<TData, PatternMatch<TOffset>, bool>[0], ref nextPriority);
			_fsa.MarkArcPriorities();

			var writer = new StreamWriter(string.Format("c:\\{0}-nfa.dot", Direction == Direction.LeftToRight ? "ltor" : "rtol"));
			_fsa.ToGraphViz(writer);
			writer.Close();

			_fsa.Determinize();

			writer = new StreamWriter(string.Format("c:\\{0}-dfa.dot", Direction == Direction.LeftToRight ? "ltor" : "rtol"));
			_fsa.ToGraphViz(writer);
			writer.Close();
		}

		private void GenerateExpressionNfa(State<TData, TOffset> startState, Expression<TData, TOffset> expr, string parentName,
			Func<TData, PatternMatch<TOffset>, bool>[] acceptables, ref int nextPriority)
		{
			string name = parentName == null ? expr.Name : parentName + "*" + expr.Name;
			acceptables = acceptables.Concat(expr.Acceptable).ToArray();
			if (expr.Children.All(node => node is Expression<TData, TOffset>))
			{
				foreach (Expression<TData, TOffset> childExpr in expr.Children.GetNodes(_fsa.Direction))
					GenerateExpressionNfa(startState, childExpr, name, acceptables, ref nextPriority);
			}
			else
			{
				startState = _fsa.CreateTag(startState, _fsa.CreateState(), EntireMatch, true);
				startState = expr.GenerateNfa(_fsa, startState);
				startState = _fsa.CreateTag(startState, _fsa.CreateState(), EntireMatch, false);
				State<TData, TOffset> acceptingState = _fsa.CreateAcceptingState(name,
					(input, match) =>
						{
							PatternMatch<TOffset> patMatch = CreatePatternMatch(match);
							return acceptables.All(acceptable => acceptable(input, patMatch));
						}, nextPriority++);
				startState.AddArc(acceptingState);
			}
		}

		public bool IsMatch(TData data, Annotation<TOffset> start)
		{
			IEnumerable<PatternMatch<TOffset>> matches;
			return IsMatch(data, start, false, out matches);
		}

		public bool IsMatch(TData data, Annotation<TOffset> start, out PatternMatch<TOffset> match)
		{
			IEnumerable<PatternMatch<TOffset>> matches;
			if (IsMatch(data, start, false, out matches))
			{
				match = matches.First();
				return true;
			}
			match = null;
			return false;
		}

		public bool IsMatch(TData data, Annotation<TOffset> start, out IEnumerable<PatternMatch<TOffset>> matches)
		{
			return IsMatch(data, start, true, out matches);
		}

		public bool IsMatch(TData data)
		{
			IEnumerable<PatternMatch<TOffset>> matches;
			return IsMatch(data, data.Annotations.GetFirst(Direction, Filter), false, out matches);
		}

		public bool IsMatch(TData data, out PatternMatch<TOffset> match)
		{
			IEnumerable<PatternMatch<TOffset>> matches;
			if (IsMatch(data, data.Annotations.GetFirst(Direction, Filter), false, out matches))
			{
				match = matches.First();
				return true;
			}
			match = null;
			return false;
		}

		public bool IsMatch(TData data, out IEnumerable<PatternMatch<TOffset>> matches)
		{
			return IsMatch(data, data.Annotations.GetFirst(Direction, Filter), true, out matches);
		}

		private bool IsMatch(TData data, Annotation<TOffset> start, bool allMatches, out IEnumerable<PatternMatch<TOffset>> matches)
		{
			if (!IsCompiled)
				Compile();

			List<PatternMatch<TOffset>> matchesList = null;
			IEnumerable<FsaMatch<TOffset>> fsaMatches;
			if (_fsa.IsMatch(data, start, allMatches, out fsaMatches))
			{
				matchesList = new List<PatternMatch<TOffset>>();
				foreach (FsaMatch<TOffset> match in fsaMatches)
				{
					matchesList.Add(CreatePatternMatch(match));
				}
			}

			matches = matchesList;
			return matchesList != null;
		}

		public Pattern<TData, TOffset> Reverse()
		{
			return new Pattern<TData, TOffset>(_spanFactory, Children.Clone())
			       	{
			       		Direction = Direction == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight,
			       		Filter = Filter,
			       		Acceptable = Acceptable
			       	};
		}

		private PatternMatch<TOffset> CreatePatternMatch(FsaMatch<TOffset> match)
		{
			var groups = new Dictionary<string, Span<TOffset>>();
			TOffset matchStart, matchEnd;
			_fsa.GetOffsets(EntireMatch, match.Registers, out matchStart, out matchEnd);
			Span<TOffset> matchSpan = _spanFactory.Create(matchStart, matchEnd);
			foreach (string groupName in _fsa.GroupNames)
			{
				if (groupName == EntireMatch)
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

			return new PatternMatch<TOffset>(matchSpan, groups,
				string.IsNullOrEmpty(match.ID) ? Enumerable.Empty<string>() : match.ID.Split('*'), match.VariableBindings);
		}

		public override PatternNode<TData, TOffset> Clone()
        {
            return new Pattern<TData, TOffset>(this);
        }

        public override string ToString()
        {
        	return Children.ToString();
        }
	}
}
