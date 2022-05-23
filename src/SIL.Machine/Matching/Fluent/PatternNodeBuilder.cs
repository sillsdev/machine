using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Matching.Fluent
{
    public abstract class PatternNodeBuilder<TData, TOffset> where TData : IAnnotatedData<TOffset>
    {
        private readonly List<PatternNode<TData, TOffset>> _nodes;
        private readonly List<PatternNode<TData, TOffset>> _alternation;

        private bool _inAlternation;

        protected PatternNodeBuilder()
        {
            _nodes = new List<PatternNode<TData, TOffset>>();
            _alternation = new List<PatternNode<TData, TOffset>>();
        }

        protected void AddAlternative()
        {
            if (_alternation.Count == 0)
            {
                _alternation.Add(_nodes.Last());
                _nodes.RemoveAt(_nodes.Count - 1);
            }
            _inAlternation = true;
        }

        protected void AddAnnotation(FeatureStruct fs)
        {
            CheckEndAlternation();
            AddNode(new Constraint<TData, TOffset>(fs));
        }

        protected void AddGroup(string name, Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build)
        {
            CheckEndAlternation();
            var groupBuilder = new GroupBuilder<TData, TOffset>(name);
            IGroupSyntax<TData, TOffset> result = build(groupBuilder);
            AddNode(result.Value);
        }

        protected void AddQuantifier(int min, int max, bool greedy)
        {
            List<PatternNode<TData, TOffset>> list = _alternation.Any() ? _alternation : _nodes;
            var quantifier = new Quantifier<TData, TOffset>(min, max, list.Last()) { IsGreedy = greedy };
            list[list.Count - 1] = quantifier;
        }

        protected void AddSubpattern(
            string name,
            Func<IPatternSyntax<TData, TOffset>, IInitialNodesPatternSyntax<TData, TOffset>> build
        )
        {
            CheckEndAlternation();
            var exprBuilder = new PatternBuilder<TData, TOffset>(name);
            IInitialNodesPatternSyntax<TData, TOffset> result = build(exprBuilder);
            AddNode(result.Value);
        }

        private void AddNode(PatternNode<TData, TOffset> node)
        {
            if (_inAlternation)
            {
                _alternation.Add(node);
                _inAlternation = false;
            }
            else
            {
                _nodes.Add(node);
            }
        }

        private void CheckEndAlternation()
        {
            if (!_inAlternation && _alternation.Count > 0)
            {
                _nodes.Add(new Alternation<TData, TOffset>(_alternation));
                _alternation.Clear();
            }
        }

        protected void PopulateNode(PatternNode<TData, TOffset> node)
        {
            CheckEndAlternation();
            foreach (PatternNode<TData, TOffset> child in _nodes)
                node.Children.Add(child);
        }
    }
}
