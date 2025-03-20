using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.FiniteState;
using SIL.ObjectModel;

namespace SIL.Machine.Matching
{
    public class Matcher<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        public const string EntireMatch = "*entire*";

        private readonly MatcherSettings<TOffset> _settings;
        private Fst<TData, TOffset> _fsa;
        private readonly IEqualityComparer<Match<TData, TOffset>> _matchComparer;

        public Matcher(Pattern<TData, TOffset> pattern)
            : this(pattern, new MatcherSettings<TOffset>()) { }

        public Matcher(Pattern<TData, TOffset> pattern, MatcherSettings<TOffset> settings)
        {
            _settings = settings;
            _settings.Freeze();

            _matchComparer = AnonymousEqualityComparer.Create<Match<TData, TOffset>>(MatchEquals, MatchGetHashCode);

            Compile(pattern);
        }

        private bool MatchEquals(Match<TData, TOffset> x, Match<TData, TOffset> y)
        {
            return EqualityComparer<TOffset>.Default.Equals(x.Range.Start, y.Range.Start)
                && EqualityComparer<TOffset>.Default.Equals(x.Range.End, y.Range.End)
                && x.PatternPath.SequenceEqual(y.PatternPath);
        }

        private int MatchGetHashCode(Match<TData, TOffset> m)
        {
            int code = 23;
            code =
                code * 31 + (m.Range.Start == null ? 0 : EqualityComparer<TOffset>.Default.GetHashCode(m.Range.Start));
            code = code * 31 + (m.Range.End == null ? 0 : EqualityComparer<TOffset>.Default.GetHashCode(m.Range.End));
            code = code * 31 + m.PatternPath.GetSequenceHashCode();
            return code;
        }

        public MatcherSettings<TOffset> Settings
        {
            get { return _settings; }
        }

        public Direction Direction
        {
            get { return _settings.Direction; }
        }

        private void Compile(Pattern<TData, TOffset> pattern)
        {
            _fsa = new Fst<TData, TOffset>(EqualityComparer<TOffset>.Default)
            {
                Direction = _settings.Direction,
                Filter = _settings.Filter,
                UseUnification = _settings.MatchingMethod == MatchingMethod.Unification
            };
            _fsa.StartState = _fsa.CreateState();
            int nextPriority = 0;
            bool hasVariables = GeneratePatternNfa(
                _fsa.StartState,
                pattern,
                null,
                new Func<Match<TData, TOffset>, bool>[0],
                ref nextPriority
            );

#if FST_GRAPHS
            using (
                var writer = new System.IO.StreamWriter(
                    string.Format("{0}-nfa.dot", _settings.Direction == Direction.LeftToRight ? "ltor" : "rtol")
                )
            )
                _fsa.ToGraphViz(writer);
#endif

            if (!_settings.Nondeterministic && !hasVariables && !_settings.AllSubmatches)
            {
                _fsa = _fsa.Determinize();
#if FST_GRAPHS
                using (
                    var writer = new System.IO.StreamWriter(
                        string.Format("{0}-dfa.dot", _settings.Direction == Direction.LeftToRight ? "ltor" : "rtol")
                    )
                )
                    _fsa.ToGraphViz(writer);
#endif
                _fsa.Minimize();
#if FST_GRAPHS
                using (
                    var writer = new System.IO.StreamWriter(
                        string.Format("{0}-mindfa.dot", _settings.Direction == Direction.LeftToRight ? "ltor" : "rtol")
                    )
                )
                    _fsa.ToGraphViz(writer);
#endif
            }
            else
            {
                _fsa = _fsa.EpsilonRemoval();
#if FST_GRAPHS
                using (
                    var writer = new System.IO.StreamWriter(
                        string.Format("{0}-ernfa.dot", _settings.Direction == Direction.LeftToRight ? "ltor" : "rtol")
                    )
                )
                    _fsa.ToGraphViz(writer);
#endif
            }
            _fsa.IgnoreVariables = !hasVariables;
            _fsa.Freeze();
        }

        private bool GeneratePatternNfa(
            State<TData, TOffset> startState,
            Pattern<TData, TOffset> pattern,
            string parentName,
            Func<Match<TData, TOffset>, bool>[] acceptables,
            ref int nextPriority
        )
        {
            bool hasVariables = false;
            string name = parentName == null ? pattern.Name : parentName + "*" + pattern.Name;
            if (pattern.Acceptable != null)
                acceptables = acceptables.Concat(pattern.Acceptable).ToArray();
            if (pattern.Children.All(node => node is Pattern<TData, TOffset>))
            {
                foreach (Pattern<TData, TOffset> childExpr in pattern.Children.Cast<Pattern<TData, TOffset>>())
                {
                    if (GeneratePatternNfa(startState, childExpr, name, acceptables, ref nextPriority))
                        hasVariables = true;
                }
            }
            else
            {
                startState = _fsa.CreateTag(startState, _fsa.CreateState(), EntireMatch, true);
                startState = pattern.GenerateNfa(_fsa, startState, out hasVariables);
                startState = _fsa.CreateTag(startState, _fsa.CreateState(), EntireMatch, false);

                Func<TData, FstResult<TData, TOffset>, bool> acceptable = null;
                if (acceptables.Length > 0)
                {
                    acceptable = (input, match) =>
                    {
                        Match<TData, TOffset> patMatch = CreatePatternMatch(input, match);
                        return acceptables.All(a => a(patMatch));
                    };
                }
                State<TData, TOffset> acceptingState = _fsa.CreateAcceptingState(name, acceptable, nextPriority++);
                startState.Arcs.Add(acceptingState);
            }

            return hasVariables;
        }

        private Match<TData, TOffset> CreatePatternMatch(TData input, FstResult<TData, TOffset> match)
        {
            TOffset matchStart,
                matchEnd;
            _fsa.GetOffsets(EntireMatch, match.Registers, out matchStart, out matchEnd);
            Range<TOffset> matchRange = Range<TOffset>.Create(matchStart, matchEnd);
            var groupCaptures = new List<GroupCapture<TOffset>>();
            foreach (string groupName in _fsa.GroupNames)
            {
                if (groupName == EntireMatch)
                    continue;

                GroupCapture<TOffset> groupCapture = null;
                TOffset start,
                    end;
                if (_fsa.GetOffsets(groupName, match.Registers, out start, out end))
                {
                    if (Range<TOffset>.IsValidRange(start, end) && !Range<TOffset>.IsEmptyRange(start, end))
                    {
                        Range<TOffset> range = Range<TOffset>.Create(start, end);
                        if (matchRange.Contains(range))
                            groupCapture = new GroupCapture<TOffset>(groupName, range);
                    }
                }

                if (groupCapture == null)
                    groupCapture = new GroupCapture<TOffset>(groupName, Range<TOffset>.Null);
                groupCaptures.Add(groupCapture);
            }

            return new Match<TData, TOffset>(
                this,
                true,
                matchRange,
                input,
                groupCaptures,
                string.IsNullOrEmpty(match.ID) ? new string[0] : match.ID.Split('*'),
                match.VariableBindings,
                match.NextAnnotation
            );
        }

        public bool IsMatch(TData input, VariableBindings varBindings = null)
        {
            return Match(input, varBindings).Success;
        }

        public bool IsMatch(TData input, TOffset start, VariableBindings varBindings = null)
        {
            return Match(input, start, varBindings).Success;
        }

        public Match<TData, TOffset> Match(TData input, VariableBindings varBindings = null)
        {
            return Match(input, GetStartAnnotation(input), varBindings);
        }

        public Match<TData, TOffset> Match(TData input, TOffset start, VariableBindings varBindings = null)
        {
            return Match(input, GetStartAnnotation(input, start), varBindings);
        }

        public IEnumerable<Match<TData, TOffset>> Matches(TData input, VariableBindings varBindings = null)
        {
            return Matches(input, GetStartAnnotation(input), varBindings);
        }

        public IEnumerable<Match<TData, TOffset>> Matches(
            TData input,
            TOffset start,
            VariableBindings varBindings = null
        )
        {
            return Matches(input, GetStartAnnotation(input, start), varBindings);
        }

        public IEnumerable<Match<TData, TOffset>> AllMatches(TData input, VariableBindings varBindings = null)
        {
            return AllMatches(input, GetStartAnnotation(input), varBindings);
        }

        public IEnumerable<Match<TData, TOffset>> AllMatches(
            TData input,
            TOffset start,
            VariableBindings varBindings = null
        )
        {
            return AllMatches(input, GetStartAnnotation(input, start), varBindings);
        }

        internal Match<TData, TOffset> Match(TData input, Annotation<TOffset> startAnn, VariableBindings varBindings)
        {
            FstResult<TData, TOffset> result;
            if (
                _fsa.Transduce(
                    input,
                    startAnn,
                    varBindings,
                    _settings.AnchoredToStart,
                    _settings.AnchoredToEnd,
                    _settings.UseDefaults,
                    out result
                )
            )
            {
                return CreatePatternMatch(input, result);
            }

            return new Match<TData, TOffset>(this, false, Range<TOffset>.Null, input);
        }

        internal IEnumerable<Match<TData, TOffset>> Matches(
            TData input,
            Annotation<TOffset> startAnn,
            VariableBindings varBindings
        )
        {
            Match<TData, TOffset> match = Match(input, startAnn, varBindings);
            while (match.Success)
            {
                yield return match;
                match = match.NextMatch(varBindings);
            }
        }

        private IEnumerable<Match<TData, TOffset>> AllMatches(
            TData input,
            Annotation<TOffset> startAnn,
            VariableBindings varBindings
        )
        {
            IEnumerable<FstResult<TData, TOffset>> results;
            if (
                _fsa.Transduce(
                    input,
                    startAnn,
                    varBindings,
                    _settings.AnchoredToStart,
                    _settings.AnchoredToEnd,
                    _settings.UseDefaults,
                    out results
                )
            )
            {
                IEnumerable<Match<TData, TOffset>> matches = results.Select(fm => CreatePatternMatch(input, fm));
                if (!_fsa.IsDeterministic && !_settings.AllSubmatches)
                    return matches.GroupBy(m => m, _matchComparer).Select(group => group.First());
                return matches;
            }

            return Enumerable.Empty<Match<TData, TOffset>>();
        }

        private Annotation<TOffset> GetStartAnnotation(TData input)
        {
            return GetStartAnnotation(input, input.Range.GetStart(_settings.Direction));
        }

        private Annotation<TOffset> GetStartAnnotation(TData input, TOffset start)
        {
            Annotation<TOffset> startAnn;
            if (!input.Annotations.FindDepthFirst(start, _settings.Direction, out startAnn))
            {
                startAnn =
                    startAnn == input.Annotations.GetBegin(_settings.Direction)
                        ? input.Annotations.GetFirst(_settings.Direction)
                        : startAnn.GetNext(_settings.Direction);
            }
            if (!_settings.Filter(startAnn))
                startAnn = startAnn.GetNextDepthFirst(_settings.Direction, _settings.Filter);
            return startAnn;
        }
    }
}
