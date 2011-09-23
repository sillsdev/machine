using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SIL.APRE.FeatureModel;
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

		private const string EntireMatch = "*entire*";

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
			: base(null, acceptable)
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
			: base(null, acceptable, nodes)
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

		public bool IsCompiled
		{
			get { return _fsa != null; }
		}

		public void Compile()
		{
			_fsa = new FiniteStateAutomaton<TOffset>(_dir, _filter);
			int nextPriority = 0;
			GenerateExpressionNfa(_fsa.StartState, this, null, new Func<IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, bool>[0], ref nextPriority);
			_fsa.MarkArcPriorities();

			var writer = new StreamWriter(string.Format("c:\\{0}-nfa.dot", _dir == Direction.LeftToRight ? "ltor" : "rtol"));
			_fsa.ToGraphViz(writer);
			writer.Close();

			_fsa.Determinize();

			writer = new StreamWriter(string.Format("c:\\{0}-dfa.dot", _dir == Direction.LeftToRight ? "ltor" : "rtol"));
			_fsa.ToGraphViz(writer);
			writer.Close();
		}

		private void GenerateExpressionNfa(State<TOffset> startState, Expression<TOffset> expr, string parentName,
			Func<IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, bool>[] acceptables, ref int nextPriority)
		{
			string name = parentName == null ? expr.Name : parentName + "*" + expr.Name;
			acceptables = acceptables.Concat(expr.Acceptable).ToArray();
			if (expr.Children.All(node => node is Expression<TOffset>))
			{
				foreach (Expression<TOffset> childExpr in expr.Children.GetNodes(_fsa.Direction))
					GenerateExpressionNfa(startState, childExpr, name, acceptables, ref nextPriority);
			}
			else
			{
				startState = _fsa.CreateTag(startState, _fsa.CreateState(), EntireMatch, true);
				State<TOffset> endState;
				if (expr.Children.GetFirst(_fsa.Direction) is Anchor<TOffset>)
				{
					endState = startState.AddArc(FeatureStruct.With(FsaFeatureSystem.Instance)
						.Feature(FsaFeatureSystem.Type).EqualTo("anchor")
						.Symbol(_dir == Direction.LeftToRight ? FsaFeatureSystem.LeftSide : FsaFeatureSystem.RightSide).Value, _fsa.CreateState());
				}
				else
				{
					State<TOffset> nextState = startState.AddArc(_fsa.CreateState(), ArcPriorityType.High);
					endState = nextState.AddArc(FeatureStruct.With(FsaFeatureSystem.Instance)
						.Feature(FsaFeatureSystem.Type).EqualTo("anchor")
						.Symbol(_dir == Direction.LeftToRight ? FsaFeatureSystem.LeftSide : FsaFeatureSystem.RightSide).Value, _fsa.CreateState());
					startState.AddArc(endState);
				}
				startState = expr.GenerateNfa(_fsa, endState);

				if (expr.Children.GetLast(_fsa.Direction) is Anchor<TOffset>)
				{
					endState = startState.AddArc(FeatureStruct.With(FsaFeatureSystem.Instance)
						.Feature(FsaFeatureSystem.Type).EqualTo("anchor")
						.Symbol(_dir == Direction.LeftToRight ? FsaFeatureSystem.RightSide : FsaFeatureSystem.LeftSide).Value, _fsa.CreateState());
				}
				else
				{
					State<TOffset> nextState = startState.AddArc(_fsa.CreateState(), ArcPriorityType.VeryLow);
					endState = nextState.AddArc(FeatureStruct.With(FsaFeatureSystem.Instance)
						.Feature(FsaFeatureSystem.Type).EqualTo("anchor")
						.Symbol(_dir == Direction.LeftToRight ? FsaFeatureSystem.RightSide : FsaFeatureSystem.LeftSide).Value, _fsa.CreateState());
					startState.AddArc(endState);
				}
				startState = _fsa.CreateTag(endState, _fsa.CreateState(), EntireMatch, false);
				State<TOffset> acceptingState = _fsa.CreateAcceptingState(name,
					(input, match) =>
						{
							PatternMatch<TOffset> patMatch = CreatePatternMatch(match);
							return acceptables.All(acceptable => acceptable(input, patMatch));
						}, nextPriority++);
				startState.AddArc(acceptingState);
			}
		}

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList, Annotation<TOffset> start)
		{
			IEnumerable<PatternMatch<TOffset>> matches;
			return IsMatch(annList, start, false, out matches);
		}

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList, Annotation<TOffset> start, out PatternMatch<TOffset> match)
		{
			IEnumerable<PatternMatch<TOffset>> matches;
			if (IsMatch(annList, start, false, out matches))
			{
				match = matches.First();
				return true;
			}
			match = null;
			return false;
		}

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList, Annotation<TOffset> start, out IEnumerable<PatternMatch<TOffset>> matches)
		{
			return IsMatch(annList, start, true, out matches);
		}

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList)
		{
			IEnumerable<PatternMatch<TOffset>> matches;
			return IsMatch(annList, annList.GetFirst(_dir, _filter), false, out matches);
		}

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList, out PatternMatch<TOffset> match)
		{
			IEnumerable<PatternMatch<TOffset>> matches;
			if (IsMatch(annList, annList.GetFirst(_dir, _filter), false, out matches))
			{
				match = matches.First();
				return true;
			}
			match = null;
			return false;
		}

		public bool IsMatch(IBidirList<Annotation<TOffset>> annList, out IEnumerable<PatternMatch<TOffset>> matches)
		{
			return IsMatch(annList, annList.GetFirst(_dir, _filter), true, out matches);
		}

		private bool IsMatch(IBidirList<Annotation<TOffset>> annList, Annotation<TOffset> start, bool allMatches, out IEnumerable<PatternMatch<TOffset>> matches)
		{
			if (!IsCompiled)
				Compile();

			List<PatternMatch<TOffset>> matchesList = null;
			IEnumerable<FsaMatch<TOffset>> fsaMatches;
			if (_fsa.IsMatch(annList, start, allMatches, out fsaMatches))
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

		public Pattern<TOffset> Reverse()
		{
			return new Pattern<TOffset>(_spanFactory, _dir == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight,
				_filter, Acceptable, Children.Clone());
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

		//private void MarkAnchoredAnnotations(HashSet<Annotation<TOffset>> leftAnchored, HashSet<Annotation<TOffset>> rightAnchored)
		//{
		//    foreach (Annotation<TOffset> ann in leftAnchored)
		//    {
		//        AnchorTypes anchor = AnchorTypes.LeftSide;
		//        if (rightAnchored.Contains(ann))
		//            anchor = anchor | AnchorTypes.RightSide;
		//        AddAnchor(ann.FeatureStruct, anchor);
		//    }

		//    foreach (Annotation<TOffset> ann in rightAnchored)
		//    {
		//        if (!leftAnchored.Contains(ann))
		//            AddAnchor(ann.FeatureStruct, AnchorTypes.RightSide);
		//    }
		//}

		//private void UnmarkAnchoredAnnotations(HashSet<Annotation<TOffset>> leftAnchored, HashSet<Annotation<TOffset>> rightAnchored)
		//{
		//    foreach (Annotation<TOffset> ann in leftAnchored.Union(rightAnchored))
		//        ann.FeatureStruct.RemoveValue(_anchorFeature);
		//}

		//private IEnumerable<Annotation<TOffset>> GetAnchoredAnnotations(Annotation<TOffset> curAnn, Direction dir)
		//{
		//    TOffset offset = curAnn.Span.GetStart(dir);
		//    for (; curAnn != null && curAnn.Span.GetStart(_dir).Equals(offset); curAnn = curAnn.GetNext(_dir, _filter))
		//    {
		//        yield return curAnn;
		//        if (curAnn.Optional)
		//        {
		//            Annotation<TOffset> nextAnn = curAnn.GetNext(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
		//            if (nextAnn != null)
		//            {
		//                foreach (Annotation<TOffset> a in GetAnchoredAnnotations(nextAnn, dir))
		//                    yield return a;
		//            }
		//        }
		//    }
		//}

		//private void AddAnchor(FeatureStruct fs, AnchorTypes anchor)
		//{
		//    if (anchor == AnchorTypes.None)
		//    {
		//        fs.AddValue(_anchorFeature, new SymbolicFeatureValue(_anchorFeature));
		//    }
		//    else
		//    {
		//        IEnumerable<FeatureSymbol> symbols = Enumerable.Empty<FeatureSymbol>();
		//        if ((anchor & AnchorTypes.LeftSide) == AnchorTypes.LeftSide)
		//            symbols = symbols.Concat(_leftSide);
		//        if ((anchor & AnchorTypes.RightSide) == AnchorTypes.RightSide)
		//            symbols = symbols.Concat(_rightSide);
		//        fs.AddValue(_anchorFeature, new SymbolicFeatureValue(symbols));
		//    }
		//}

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
