using System;
using System.Collections.Generic;
using System.Linq;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Patterns
{
	public class PatternExpressionBuilder<TOffset>
	{
		private readonly List<PatternNode<TOffset>> _nodes;

		public PatternExpressionBuilder()
		{
			_nodes = new List<PatternNode<TOffset>>();
		}

		public PatternExpressionBuilder<TOffset> Or(Action<PatternExpressionBuilder<TOffset>> build)
		{
			var groupBuilder = new PatternExpressionBuilder<TOffset>();
			build(groupBuilder);
			_nodes.Add(new Alternation<TOffset>(groupBuilder.Build()));
			return this;
		}

		public PatternExpressionBuilder<TOffset> Group(string name, Action<PatternExpressionBuilder<TOffset>> build)
		{
			var groupBuilder = new PatternExpressionBuilder<TOffset>();
			build(groupBuilder);
			_nodes.Add(new Group<TOffset>(name, groupBuilder.Build()));
			return this;
		}

		public PatternExpressionBuilder<TOffset> Group(Action<PatternExpressionBuilder<TOffset>> build)
		{
			var groupBuilder = new PatternExpressionBuilder<TOffset>();
			build(groupBuilder);
			_nodes.Add(new Group<TOffset>(groupBuilder.Build()));
			return this;
		}

		public PatternExpressionBuilder<TOffset> ZeroOrMore()
		{
			var quantifier = new Quantifier<TOffset>(0, -1, _nodes.Last());
			_nodes[_nodes.Count - 1] = quantifier;
			return this;
		}

		public PatternExpressionBuilder<TOffset> OneOrMore()
		{
			var quantifier = new Quantifier<TOffset>(1, -1, _nodes.Last());
			_nodes[_nodes.Count - 1] = quantifier;
			return this;
		}

		public PatternExpressionBuilder<TOffset> Optional()
		{
			var quantifier = new Quantifier<TOffset>(0, 1, _nodes.Last());
			_nodes[_nodes.Count - 1] = quantifier;
			return this;
		}

		public PatternExpressionBuilder<TOffset> Range(int min, int max)
		{
			var quantifier = new Quantifier<TOffset>(min, max, _nodes.Last());
			_nodes[_nodes.Count - 1] = quantifier;
			return this;
		}

		public PatternExpressionBuilder<TOffset> Annotation(FeatureSystem featSys, Action<DisjunctiveFeatureStructureBuilder> build)
		{
			var fsBuilder = new DisjunctiveFeatureStructureBuilder(featSys);
			build(fsBuilder);
			_nodes.Add(new Constraints<TOffset>(fsBuilder));
			return this;
		}

		public PatternExpressionBuilder<TOffset> Annotation(FeatureStructure fs)
		{
			_nodes.Add(new Constraints<TOffset>(fs));
			return this;
		}

		public IEnumerable<PatternNode<TOffset>> Build()
		{
			return _nodes;
		}
	}
}
