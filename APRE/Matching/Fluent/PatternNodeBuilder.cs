using System;
using System.Collections.Generic;
using System.Linq;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Matching.Fluent
{
	public abstract class PatternNodeBuilder<TOffset>
	{
		private readonly List<PatternNode<TOffset>> _nodes;
		private readonly List<PatternNode<TOffset>> _alternation;

		private bool _inAlternation;

		protected PatternNodeBuilder()
		{
			_nodes = new List<PatternNode<TOffset>>();
			_alternation = new List<PatternNode<TOffset>>();
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
			AddNode(new Constraint<TOffset>(fs));
		}

		protected void AddGroup(string name, Func<IGroupSyntax<TOffset>, IGroupSyntax<TOffset>> build)
		{
			CheckEndAlternation();
			var groupBuilder = new GroupBuilder<TOffset>(name);
			IGroupSyntax<TOffset> result = build(groupBuilder);
			AddNode(result.Value);
		}

		protected void AddQuantifier(int min, int max)
		{
			List<PatternNode<TOffset>> list = _alternation.Any() ? _alternation : _nodes;
			var quantifier = new Quantifier<TOffset>(min, max, list.Last());
			list[list.Count - 1] = quantifier;
		}

		protected void AddExpression(string name, Func<IExpressionSyntax<TOffset>, IExpressionSyntax<TOffset>> build)
		{
			CheckEndAlternation();
			var exprBuilder = new ExpressionBuilder<TOffset>(name);
			IExpressionSyntax<TOffset> result = build(exprBuilder);
			AddNode(result.Value);
		}

		protected void AddAnchor(AnchorType anchor)
		{
			CheckEndAlternation();
			AddNode(new Anchor<TOffset>(anchor));
		}

		private void AddNode(PatternNode<TOffset> node)
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
				_nodes.Add(new Alternation<TOffset>(_alternation));
				_alternation.Clear();
			}
		}

		protected void PopulateNode(PatternNode<TOffset> node)
		{
			CheckEndAlternation();
			foreach (PatternNode<TOffset> child in _nodes)
				node.Children.Add(child);
		}
	}
}
