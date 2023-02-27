using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Matching
{
    /// <summary>
    /// This class represents a match between a phonetic shape and a phonetic pattern.
    /// </summary>
    public class Match<TData, TOffset> : GroupCapture<TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        private readonly Matcher<TData, TOffset> _matcher;
        private readonly GroupCaptureCollection<TOffset> _groupCaptures;
        private readonly VariableBindings _varBindings;
        private readonly IList<string> _patternPath;
        private readonly TData _input;
        private readonly Annotation<TOffset> _nextAnn;

        internal Match(Matcher<TData, TOffset> matcher, Range<TOffset> range, TData input)
            : this(
                matcher,
                range,
                input,
                Enumerable.Empty<GroupCapture<TOffset>>(),
                new string[0],
                new VariableBindings(),
                null
            ) { }

        internal Match(
            Matcher<TData, TOffset> matcher,
            Range<TOffset> range,
            TData input,
            IEnumerable<GroupCapture<TOffset>> groupCaptures,
            IList<string> patternPath,
            VariableBindings varBindings,
            Annotation<TOffset> nextAnn
        )
            : base(Matcher<TData, TOffset>.EntireMatch, range)
        {
            _matcher = matcher;
            _groupCaptures = new GroupCaptureCollection<TOffset>(groupCaptures);
            _patternPath = patternPath;
            _varBindings = varBindings;
            _input = input;
            _nextAnn = nextAnn;
        }

        public Matcher<TData, TOffset> Matcher
        {
            get { return _matcher; }
        }

        public TData Input
        {
            get { return _input; }
        }

        public IReadOnlyList<string> PatternPath
        {
            get { return _patternPath.ToReadOnlyList(); }
        }

        public VariableBindings VariableBindings
        {
            get { return _varBindings; }
        }

        public GroupCaptureCollection<TOffset> GroupCaptures
        {
            get { return _groupCaptures; }
        }

        public Match<TData, TOffset> NextMatch(VariableBindings varBindings = null)
        {
            return _matcher.Match(_input, _nextAnn, varBindings);
        }

        internal Annotation<TOffset> NextAnnotation
        {
            get { return _nextAnn; }
        }
    }
}
