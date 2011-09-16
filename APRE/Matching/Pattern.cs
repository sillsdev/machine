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
		private bool _checkAnchors;
		private readonly Direction _dir;

		private readonly FeatureSymbol _leftSide;
		private readonly FeatureSymbol _rightSide;
		private readonly SymbolicFeature _anchorFeature;

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

			InitAnchorFeature(out _anchorFeature, out _leftSide, out _rightSide);
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

			InitAnchorFeature(out _anchorFeature, out _leftSide, out _rightSide);
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

			InitAnchorFeature(out _anchorFeature, out _leftSide, out _rightSide);
        }

		private static void InitAnchorFeature(out SymbolicFeature anchorFeature, out FeatureSymbol leftSide, out FeatureSymbol rightSide)
		{
			leftSide = new FeatureSymbol(Guid.NewGuid().ToString(), "LeftSide");
			rightSide = new FeatureSymbol(Guid.NewGuid().ToString(), "RightSide");
			anchorFeature = new SymbolicFeature(Guid.NewGuid().ToString(), "Anchor");
			anchorFeature.AddPossibleSymbol(leftSide);
			anchorFeature.AddPossibleSymbol(rightSide);
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
			GenerateExpressionNfa(_fsa.StartState, this, null, new[] {Acceptable}, ref nextPriority);
			_fsa.MarkArcPriorities();

			var writer = new StreamWriter(string.Format("c:\\{0}-nfa.dot", _dir == Direction.LeftToRight ? "ltor" : "rtol"));
			_fsa.ToGraphViz(writer);
			writer.Close();

			_fsa.Determinize();

			writer = new StreamWriter(string.Format("c:\\{0}-dfa.dot", _dir == Direction.LeftToRight ? "ltor" : "rtol"));
			_fsa.ToGraphViz(writer);
			writer.Close();

			_checkAnchors = this.GetNodes().OfType<Anchor<TOffset>>().Any();
		}

		private void GenerateExpressionNfa(State<TOffset> startState, Expression<TOffset> expr, string parentName,
			Func<IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, bool>[] acceptables, ref int nextPriority)
		{
			string name = parentName == null ? expr.Name : parentName + "*" + expr.Name;
			if (expr.Children.All(node => node is Expression<TOffset>))
			{
				foreach (Expression<TOffset> childExpr in expr.Children.GetNodes(_fsa.Direction))
					GenerateExpressionNfa(startState, childExpr, name, acceptables.Concat(expr.Acceptable).ToArray(), ref nextPriority);
			}
			else
			{
				if (!(expr.Children.GetFirst(_fsa.Direction) is Anchor<TOffset>))
				{
					State<TOffset> nextState = startState.AddArc(_fsa.CreateState(), ArcPriorityType.VeryLow);
					State<TOffset> endState = nextState.AddArc(new FeatureStruct(), _fsa.CreateState());
					startState.AddArc(endState);
					endState.AddArc(startState, ArcPriorityType.VeryLow);
					startState = endState;
				}

				startState = _fsa.CreateTag(startState, _fsa.CreateState(), EntireMatch, true);
				startState = expr.GenerateNfa(_fsa, startState);
				startState = _fsa.CreateTag(startState, _fsa.CreateState(), EntireMatch, false);

				if (!(expr.Children.GetLast(_fsa.Direction) is Anchor<TOffset>))
				{
					State<TOffset> nextState = startState.AddArc(_fsa.CreateState(), ArcPriorityType.Low);
					State<TOffset> endState = nextState.AddArc(new FeatureStruct(), _fsa.CreateState());
					startState.AddArc(endState);
					endState.AddArc(startState, ArcPriorityType.Low);
					startState = endState;
				}

				State<TOffset> acceptingState = _fsa.CreateAcceptingState(name,
					(input, match) => acceptables.All(acceptable => acceptable(input, CreatePatternMatch(match))), nextPriority++);
				startState.AddArc(acceptingState);
			}
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
			if (!IsCompiled)
				Compile();

			//HashSet<Annotation<TOffset>> leftAnchored = null;
			//HashSet<Annotation<TOffset>> rightAnchored = null;
			//if (_checkAnchors)
			//{
			//    leftAnchored = new HashSet<Annotation<TOffset>>(GetAnchoredAnnotations(annList.GetFirst(Direction.LeftToRight), Direction.LeftToRight));
			//    rightAnchored = new HashSet<Annotation<TOffset>>(GetAnchoredAnnotations(annList.GetFirst(Direction.RightToLeft), Direction.RightToLeft));
			//    MarkAnchoredAnnotations(leftAnchored, rightAnchored);
			//}

			List<PatternMatch<TOffset>> matchesList = null;
			IEnumerable<FsaMatch<TOffset>> fsaMatches;
			if (_fsa.IsMatch(annList, out fsaMatches))
			{
				matchesList = new List<PatternMatch<TOffset>>();
				foreach (FsaMatch<TOffset> match in fsaMatches)
				{
					PatternMatch<TOffset> patMatch = CreatePatternMatch(match);
					if (patMatch != null)
						matchesList.Add(patMatch);
				}
			}

			//Annotation<TOffset> first = annList.GetFirst(_dir, _filter);
			//while (first != null)
			//{
			//    IEnumerable<FsaMatch<TOffset>> fsaMatches;
			//    if (_fsa.IsMatch(annList.GetView(first, _dir), out fsaMatches))
			//    {
			//        if (matchesList == null)
			//            matchesList = new List<PatternMatch<TOffset>>();
			//        foreach (FsaMatch<TOffset> match in fsaMatches)
			//            matchesList.Add(CreatePatternMatch(match));
			//        if (!allMatches)
			//        {
			//            matches = matchesList;
			//            return true;
			//        }
			//    }

			//    first = first.GetNext(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
			//}

			//if (_checkAnchors)
			//    UnmarkAnchoredAnnotations(leftAnchored, rightAnchored);

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

		private void MarkAnchoredAnnotations(HashSet<Annotation<TOffset>> leftAnchored, HashSet<Annotation<TOffset>> rightAnchored)
		{
			foreach (Annotation<TOffset> ann in leftAnchored)
			{
				AnchorTypes anchor = AnchorTypes.LeftSide;
				if (rightAnchored.Contains(ann))
					anchor = anchor | AnchorTypes.RightSide;
				AddAnchor(ann.FeatureStruct, anchor);
			}

			foreach (Annotation<TOffset> ann in rightAnchored)
			{
				if (!leftAnchored.Contains(ann))
					AddAnchor(ann.FeatureStruct, AnchorTypes.RightSide);
			}
		}

		private void UnmarkAnchoredAnnotations(HashSet<Annotation<TOffset>> leftAnchored, HashSet<Annotation<TOffset>> rightAnchored)
		{
			foreach (Annotation<TOffset> ann in leftAnchored.Union(rightAnchored))
				ann.FeatureStruct.RemoveValue(_anchorFeature);
		}

		private IEnumerable<Annotation<TOffset>> GetAnchoredAnnotations(Annotation<TOffset> curAnn, Direction dir)
		{
			TOffset offset = curAnn.Span.GetStart(dir);
			for (; curAnn != null && curAnn.Span.GetStart(_dir).Equals(offset); curAnn = curAnn.GetNext(_dir, _filter))
			{
				yield return curAnn;
				if (curAnn.IsOptional)
				{
					Annotation<TOffset> nextAnn = curAnn.GetNext(_dir, (cur, next) => !cur.Span.Overlaps(next.Span) && _filter(next));
					if (nextAnn != null)
					{
						foreach (Annotation<TOffset> a in GetAnchoredAnnotations(nextAnn, dir))
							yield return a;
					}
				}
			}
		}

		private void AddAnchor(FeatureStruct fs, AnchorTypes anchor)
		{
			if (anchor == AnchorTypes.None)
			{
				fs.AddValue(_anchorFeature, new SymbolicFeatureValue(_anchorFeature));
			}
			else
			{
				IEnumerable<FeatureSymbol> symbols = Enumerable.Empty<FeatureSymbol>();
				if ((anchor & AnchorTypes.LeftSide) == AnchorTypes.LeftSide)
					symbols = symbols.Concat(_leftSide);
				if ((anchor & AnchorTypes.RightSide) == AnchorTypes.RightSide)
					symbols = symbols.Concat(_rightSide);
				fs.AddValue(_anchorFeature, new SymbolicFeatureValue(symbols));
			}
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
