using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Fsa;

namespace SIL.Machine.Matching
{
	public class Matcher<TData, TOffset> where TData : IData<TOffset>
	{
		public const string EntireMatch = "*entire*";

		private readonly SpanFactory<TOffset> _spanFactory;
		private readonly Pattern<TData, TOffset> _pattern;
		private readonly MatcherSettings<TOffset> _settings; 
		private readonly FiniteStateAutomaton<TData, TOffset> _fsa;

		public Matcher(SpanFactory<TOffset> spanFactory, Pattern<TData, TOffset> pattern)
			: this(spanFactory, pattern, new MatcherSettings<TOffset>())
		{
		}

		public Matcher(SpanFactory<TOffset> spanFactory, Pattern<TData, TOffset> pattern, MatcherSettings<TOffset> settings)
		{
			_spanFactory = spanFactory;
			_pattern = pattern;
			_settings = settings;
			_fsa = new FiniteStateAutomaton<TData, TOffset>(_settings.Direction, _settings.Filter);
			Compile();
		}

		public Pattern<TData, TOffset> Pattern
		{
			get { return _pattern; }
		}

		public MatcherSettings<TOffset> Settings
		{
			get { return _settings; }
		}

		public Direction Direction
		{
			get { return _settings.Direction; }
		}

		private void Compile()
		{
			int nextPriority = 0;
			GeneratePatternNfa(_fsa.StartState, _pattern, null, new Func<Match<TData, TOffset>, bool>[0], ref nextPriority);
			_fsa.MarkArcPriorities();

			//var writer = new StreamWriter(string.Format("c:\\{0}-nfa.dot", _settings.Direction == Direction.LeftToRight ? "ltor" : "rtol"));
			//_fsa.ToGraphViz(writer);
			//writer.Close();

			_fsa.Determinize(_settings.Quasideterministic);

			//writer = new StreamWriter(string.Format("c:\\{0}-dfa.dot", _settings.Direction == Direction.LeftToRight ? "ltor" : "rtol"));
			//_fsa.ToGraphViz(writer);
			//writer.Close();
		}

		private void GeneratePatternNfa(State<TData, TOffset> startState, Pattern<TData, TOffset> pattern, string parentName,
			Func<Match<TData, TOffset>, bool>[] acceptables, ref int nextPriority)
		{
			string name = parentName == null ? pattern.Name : parentName + "*" + pattern.Name;
			acceptables = acceptables.Concat(pattern.Acceptable).ToArray();
			if (pattern.Children.All(node => node is Pattern<TData, TOffset>))
			{
				foreach (Pattern<TData, TOffset> childExpr in pattern.Children)
					GeneratePatternNfa(startState, childExpr, name, acceptables, ref nextPriority);
			}
			else
			{
				startState = _fsa.CreateTag(startState, _fsa.CreateState(), EntireMatch, true);
				startState = pattern.GenerateNfa(_fsa, startState);
				startState = _fsa.CreateTag(startState, _fsa.CreateState(), EntireMatch, false);
				State<TData, TOffset> acceptingState = _fsa.CreateAcceptingState(name,
					(input, match) =>
					{
						Match<TData, TOffset> patMatch = CreatePatternMatch(input, match);
						return acceptables.All(acceptable => acceptable(patMatch));
					}, nextPriority++);
				startState.AddArc(acceptingState);
			}
		}

		private Match<TData, TOffset> CreatePatternMatch(TData input, FsaMatch<TOffset> match)
		{
			TOffset matchStart, matchEnd;
			_fsa.GetOffsets(EntireMatch, match.Registers, out matchStart, out matchEnd);
			Span<TOffset> matchSpan = _spanFactory.Create(matchStart, matchEnd);
			var groupCaptures = new List<GroupCapture<TOffset>>();
			foreach (string groupName in _fsa.GroupNames)
			{
				if (groupName == EntireMatch)
					continue;

				GroupCapture<TOffset> groupCapture = null;
				TOffset start, end;
				if (_fsa.GetOffsets(groupName, match.Registers, out start, out end))
				{
					if (_spanFactory.IsValidSpan(start, end))
					{
						Span<TOffset> span = _spanFactory.Create(start, end);
						if (matchSpan.Contains(span))
							groupCapture = new GroupCapture<TOffset>(groupName, span);
					}
				}

				if (groupCapture == null)
					groupCapture = new GroupCapture<TOffset>(groupName, _spanFactory.Empty);
				groupCaptures.Add(groupCapture);
			}

			return new Match<TData, TOffset>(this, matchSpan, input, groupCaptures,
				string.IsNullOrEmpty(match.ID) ? Enumerable.Empty<string>() : match.ID.Split('*'), match.VariableBindings,
				match.NextAnnotation);
		}

		public bool IsMatch(TData input)
		{
			return Match(input).Success;
		}

		public bool IsMatch(TData input, TOffset start)
		{
			return Match(input, start).Success;
		}

		public Match<TData, TOffset> Match(TData input)
		{
			return Match(input, GetStartAnnotation(input));
		}

		public Match<TData, TOffset> Match(TData input, TOffset start)
		{
			return Match(input, GetStartAnnotation(input, start));
		}

		public IEnumerable<Match<TData, TOffset>> Matches(TData input)
		{
			return Matches(input, GetStartAnnotation(input));
		}

		public IEnumerable<Match<TData, TOffset>> Matches(TData input, TOffset start)
		{
			return Matches(input, GetStartAnnotation(input, start));
		}

		public IEnumerable<Match<TData, TOffset>> AllMatches(TData input)
		{
			return AllMatches(input, GetStartAnnotation(input));
		}

		public IEnumerable<Match<TData, TOffset>> AllMatches(TData input, TOffset start)
		{
			return AllMatches(input, GetStartAnnotation(input, start));
		}

		internal Match<TData, TOffset> Match(TData input, Annotation<TOffset> startAnn)
		{
			return GetMatches(input, startAnn, false).First();
		}

		internal IEnumerable<Match<TData, TOffset>> Matches(TData input, Annotation<TOffset> startAnn)
		{
			Match<TData, TOffset> match = Match(input, startAnn);
			while (match.Success)
			{
				yield return match;
				match = match.NextMatch();
			}
		}

		internal IEnumerable<Match<TData, TOffset>> AllMatches(TData input, Annotation<TOffset> startAnn)
		{
			return GetMatches(input, startAnn, true);
		}

		private IEnumerable<Match<TData, TOffset>> GetMatches(TData input, Annotation<TOffset> startAnn, bool allMatches)
		{
			IEnumerable<FsaMatch<TOffset>> fsaMatches;
			if (_fsa.IsMatch(input, startAnn, allMatches, _settings.UseDefaultsForMatching, out fsaMatches))
			{
				foreach (FsaMatch<TOffset> fsaMatch in fsaMatches)
					yield return CreatePatternMatch(input, fsaMatch);
			}

			yield return new Match<TData, TOffset>(this, _spanFactory.Empty, input);
		}

		private Annotation<TOffset> GetStartAnnotation(TData input)
		{
			return GetStartAnnotation(input, input.Span.GetStart(_settings.Direction));
		}

		private Annotation<TOffset> GetStartAnnotation(TData input, TOffset start)
		{
			Annotation<TOffset> startAnn;
			input.Annotations.Find(start, _settings.Direction, out startAnn);
			if (!_settings.Filter(startAnn))
				startAnn = startAnn.GetNext(_settings.Direction, _settings.Filter);
			return startAnn;
		}
	}
}
