using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.Matching;
using SIL.ObjectModel;

namespace SIL.Machine.Rules
{
    public class BatchPatternRuleSpec<TData, TOffset> : IPatternRuleSpec<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        private readonly Pattern<TData, TOffset> _pattern;
        private readonly ObservableList<IPatternRuleSpec<TData, TOffset>> _ruleSpecs;
        private readonly Dictionary<string, IPatternRuleSpec<TData, TOffset>> _ruleIds;
        private int _curRuleId;

        public BatchPatternRuleSpec()
        {
            _ruleSpecs = new ObservableList<IPatternRuleSpec<TData, TOffset>>();
            _ruleSpecs.CollectionChanged += RuleSpecsChanged;
            _ruleIds = new Dictionary<string, IPatternRuleSpec<TData, TOffset>>();
            _pattern = new Pattern<TData, TOffset>();
        }

        public BatchPatternRuleSpec(IEnumerable<IPatternRuleSpec<TData, TOffset>> ruleSpecs)
            : this()
        {
            foreach (IPatternRuleSpec<TData, TOffset> ruleSpec in ruleSpecs)
                _ruleSpecs.Add(ruleSpec);
        }

        private void RuleSpecsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldStartingIndex > -1)
            {
                PatternNode<TData, TOffset> startNode = _pattern.Children.ElementAt(e.OldStartingIndex);
                foreach (
                    Pattern<TData, TOffset> node in startNode
                        .GetNodes()
                        .Take(e.OldItems.Count)
                        .Cast<Pattern<TData, TOffset>>()
                        .ToArray()
                )
                {
                    _ruleIds.Remove(node.Name);
                    node.Remove();
                }
            }
            if (e.NewStartingIndex > -1)
            {
                PatternNode<TData, TOffset> startNode =
                    e.NewStartingIndex == 0
                        ? _pattern.Children.Begin
                        : _pattern.Children.ElementAt(e.NewStartingIndex - 1);
                foreach (IPatternRuleSpec<TData, TOffset> rs in e.NewItems)
                {
                    IPatternRuleSpec<TData, TOffset> ruleSpec = rs;
                    string id = "rule" + _curRuleId++;
                    _ruleIds[id] = ruleSpec;
                    var subpattern = new Pattern<TData, TOffset>(id, ruleSpec.Pattern.Children.CloneItems())
                    {
                        Acceptable = match => ruleSpec.IsApplicable(match.Input) && ruleSpec.Pattern.Acceptable(match)
                    };
                    startNode.AddAfter(subpattern);
                    startNode = subpattern;
                }
            }
        }

        public IList<IPatternRuleSpec<TData, TOffset>> RuleSpecs
        {
            get { return _ruleSpecs; }
        }

        public Pattern<TData, TOffset> Pattern
        {
            get { return _pattern; }
        }

        public bool IsApplicable(TData input)
        {
            return true;
        }

        public TData ApplyRhs(PatternRule<TData, TOffset> rule, Match<TData, TOffset> match)
        {
            IPatternRuleSpec<TData, TOffset> ruleSpec = _ruleIds[match.PatternPath.First()];
            var newMatch = new Match<TData, TOffset>(
                match.Matcher,
                match.Success,
                match.Range,
                match.Input,
                match.GroupCaptures,
                match.PatternPath.Skip(1).ToArray(),
                match.VariableBindings,
                match.NextAnnotation
            );
            return ruleSpec.ApplyRhs(rule, newMatch);
        }
    }
}
